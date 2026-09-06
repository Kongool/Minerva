using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Plugin.Ipc;
using Minerva;

namespace Minerva.Automation;

/// <summary>
/// Thin wrapper over a navmesh plugin's consumer IPC, used by <see cref="MovementController"/> to hand
/// auto-dodge steering to a pathfinder (which routes around geometry and owns the input hooks) when one
/// is available. Two backends are tried in order and the first that is ready drives:
/// <list type="number">
/// <item><b>Ariadne</b> — <c>Ariadne.*</c> gates; ready when <c>IsConnected</c> and <c>ZoneStatus</c> is
/// 2 (LocalCurrent) or 3 (MnemosyneCached).</item>
/// <item><b>vnavmesh</b> — the original <c>vnavmesh.*</c> gates (identical shapes); ready when
/// <c>Nav.IsReady</c>.</item>
/// </list>
/// Everything is guarded: when a plugin isn't loaded its gate calls throw, so the presence probe is
/// cached and re-run at most once a second, and every drive call is a safe no-op when absent.
/// </summary>
internal sealed class NavmeshIPC
{
    // re-probe presence/readiness at most this often — calling a gate while the plugin is absent throws,
    // and we must not pay an exception on every input sample.
    private static readonly TimeSpan ProbeInterval = TimeSpan.FromSeconds(1);

    // one navmesh provider: a readiness test, the two path gates we drive it with, and two optional
    // extras. The extras are Ariadne-only -- vnavmesh has no equivalent -- so they are nullable and every
    // use falls back to the plain behaviour rather than feature-gating the whole backend.
    private sealed class Backend(string name, Func<bool> ready,
        ICallGateSubscriber<List<Vector3>, bool, object> moveTo, ICallGateSubscriber<object> stop,
        ICallGateSubscriber<List<Vector3>, bool, float, object>? moveToWithTolerance = null,
        ICallGateSubscriber<int>? stallCount = null,
        ICallGateSubscriber<Vector3, object>? steerTo = null)
    {
        public string Name => name;
        public Func<bool> Ready => ready;
        public ICallGateSubscriber<List<Vector3>, bool, object> MoveTo => moveTo;
        public ICallGateSubscriber<object> Stop => stop;

        /// <summary>Per-path waypoint tolerance, leaving the user's global setting alone. Null when unsupported.</summary>
        public ICallGateSubscriber<List<Vector3>, bool, float, object>? MoveToWithTolerance => moveToWithTolerance;

        /// <summary>Stalls on the CURRENT path, reset on every Move/Stop. Null when unsupported.</summary>
        public ICallGateSubscriber<int>? StallCount => stallCount;

        /// <summary>Steer straight at a point through the backend's own input hook, re-issued per
        /// tick. Null when unsupported.</summary>
        public ICallGateSubscriber<Vector3, object>? SteerTo => steerTo;
    }

    private readonly Backend[] backends;
    private DateTime lastProbe;
    private Backend? active;   // last-resolved ready backend (cached between probes)
    private Backend? driving;  // backend we last issued a MoveTo to, so Stop targets the right one
    private bool everLogged;   // so the first resolution logs even when it resolves to "none"
    private string? loggedName; // last backend name we logged, to log only on change (no per-probe spam)

    public NavmeshIPC()
    {
        var pi = Service.PluginInterface;

        var ariConnected = pi.GetIpcSubscriber<bool>("Ariadne.IsConnected");
        var ariZone = pi.GetIpcSubscriber<int>("Ariadne.ZoneStatus");
        var vnavReady = pi.GetIpcSubscriber<bool>("vnavmesh.Nav.IsReady");

        this.backends =
        [
            new Backend("Ariadne",
                // Presence, not mesh state. Path.MoveTo and Path.SteerTo follow points we supply and do
                // no pathfinding, so neither needs a mesh -- only Nav.Pathfind* does, and we never call it.
                // Gating driving on the mesh meant standing down through a build, which is exactly when a
                // fight starts. The shared-data tag exists for as long as the plugin is loaded, so its
                // presence answers "can it drive" without an IPC call or an exception; its value answers
                // "is there a mesh", which we do not need.
                () => Service.PluginInterface.TryGetData<bool[]>("ariadne.NavReady", out _),
                pi.GetIpcSubscriber<List<Vector3>, bool, object>("Ariadne.Path.MoveTo"),
                pi.GetIpcSubscriber<object>("Ariadne.Path.Stop"),
                pi.GetIpcSubscriber<List<Vector3>, bool, float, object>("Ariadne.Path.MoveToWithTolerance"),
                pi.GetIpcSubscriber<int>("Ariadne.Path.StallCount"),
                pi.GetIpcSubscriber<Vector3, object>("Ariadne.Path.SteerTo")),
            new Backend("vnavmesh",
                () => vnavReady.InvokeFunc(),
                pi.GetIpcSubscriber<List<Vector3>, bool, object>("vnavmesh.Path.MoveTo"),
                pi.GetIpcSubscriber<object>("vnavmesh.Path.Stop")),
        ];
    }

    /// <summary>True when some navmesh backend is present and has a usable mesh for the current zone.</summary>
    public bool Ready() => this.Resolve() != null;

    /// <summary>Name of the active backend ("Ariadne" / "vnavmesh"), or null when none is ready.</summary>
    public string? ActiveName => this.active?.Name;

    // pick the first ready backend, cached and re-probed at most once per interval
    private Backend? Resolve()
    {
        var now = DateTime.UtcNow;
        if (now - this.lastProbe >= ProbeInterval)
        {
            this.lastProbe = now;
            this.active = null;
            foreach (var b in this.backends)
            {
                try { if (b.Ready()) { this.active = b; break; } }
                catch { /* that plugin isn't loaded — try the next */ }
            }

            // log only when the resolved backend changes, so the fallback is visible in /xllog without spamming
            var name = this.active?.Name;
            if (!this.everLogged || name != this.loggedName)
            {
                this.everLogged = true;
                this.loggedName = name;
                Service.Log.Information(name != null
                    ? $"Minerva: navmesh backend ready -> {name} (auto-move will path through it)."
                    : "Minerva: no navmesh backend ready (auto-move steers directly).");
            }
        }
        return this.active;
    }

    /// <summary>
    /// Whether a navmesh path is currently being followed — by Ariadne or by vnavmesh. Read from the
    /// always-published shared-data flag (no IPC, no exceptions), so <see cref="MovementController"/> can
    /// yield to it every frame. Both tags checked so we coexist with either plugin.
    /// </summary>
    public bool PathRunning()
    {
        var pi = Service.PluginInterface;
        if (pi.TryGetData<bool[]>("ariadne.PathIsRunning", out var a) && a is { Length: > 0 } && a[0])
            return true;
        if (pi.TryGetData<bool[]>("vnav.PathIsRunning", out var v) && v is { Length: > 0 } && v[0])
            return true;
        return false;
    }

    /// <summary>Follow a straight one-waypoint path to <paramref name="dest"/> (walk). No-op if no backend is ready.</summary>
    public void MoveTo(Vector3 dest) => this.MoveTo([dest]);

    /// <summary>
    /// Walk <paramref name="path"/> in order. No-op if no backend is ready or the path is empty.
    ///
    /// <para>Both backends follow the points they are given literally — <c>Path.MoveTo</c> does no
    /// pathfinding of its own (that is <c>Nav.Pathfind*</c>). So a one-point path means "walk the straight
    /// line to here", which is why handing over only the destination walked the character through whatever
    /// the dodge was routing around. Sending the corners makes the follower take our path.</para>
    /// </summary>
    public void MoveTo(List<Vector3> path) => this.MoveTo(path, null);

    /// <summary>
    /// Walk <paramref name="path"/> in order, asking for <paramref name="tolerance"/> yards of
    /// waypoint-pass slack when the backend supports a per-path value.
    ///
    /// <para>Tolerance matters more for a dodge than for travel: it is how close the follower has to get
    /// to a corner before heading for the next one, so a loose value cuts the corner -- and on a route
    /// whose corners exist to go around an AOE, cutting one means clipping the thing it was avoiding.
    /// Asking per-path leaves the user's global travel setting untouched.</para>
    /// </summary>
    public void MoveTo(List<Vector3> path, float? tolerance)
    {
        var b = this.Resolve();
        if (b == null || path.Count == 0)
            return;
        try
        {
            if (tolerance is { } tol && b.MoveToWithTolerance != null)
                b.MoveToWithTolerance.InvokeAction(path, false, tol);
            else
                b.MoveTo.InvokeAction(path, false);
            this.driving = b;
        }
        catch { /* backend unloaded mid-flight — ignore */ }
    }

    /// <summary>Can the active backend steer directly, without a path? (Ariadne only.)</summary>
    public bool CanSteer => (this.driving ?? this.Resolve())?.SteerTo != null;

    /// <summary>
    /// Steer straight at <paramref name="dest"/> through the backend's own input hook. Re-issue every
    /// tick; the backend stops itself on arrival.
    ///
    /// <para>This is what our own <c>RMIWalk</c> hook does, done by the plugin that maintains that hook
    /// against upstream. It is the right shape for a dodge: a straight move to a point our solver already
    /// re-picks every frame, with no waypoint list, no tolerance and no stall recovery in between.</para>
    /// </summary>
    public void Steer(Vector3 dest)
    {
        var b = this.Resolve();
        if (b?.SteerTo == null)
            return;
        try { b.SteerTo.InvokeAction(dest); this.driving = b; }
        catch { /* backend unloaded mid-flight — ignore */ }
    }

    /// <summary>
    /// Stalls the follower has seen on the path it is walking now, or 0 when the backend cannot say.
    /// <para>Recovery for an externally-supplied path is deliberately ours: the follower keeps walking our
    /// corners rather than re-pathing round them through the mesh, and tells us it is stuck instead. Note
    /// that a knockback or stun ticks this too -- the detector cannot tell "held" from "wedged" -- so a
    /// single stall is not evidence the route is wrong.</para>
    /// </summary>
    public int StallCount()
    {
        var b = this.driving ?? this.Resolve();
        if (b?.StallCount == null)
            return 0;
        try { return b.StallCount.InvokeFunc(); }
        catch { return 0; }
    }

    /// <summary>Drop the path on whichever backend we last drove. No-op if none.</summary>
    public void Stop()
    {
        var b = this.driving ?? this.active;
        this.driving = null;
        if (b == null)
            return;
        try { b.Stop.InvokeAction(); }
        catch { /* absent — nothing to stop */ }
    }
}
