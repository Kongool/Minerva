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

    // one navmesh provider: a readiness test plus the two path gates we drive it with.
    private sealed class Backend(string name, Func<bool> ready,
        ICallGateSubscriber<List<Vector3>, bool, object> moveTo, ICallGateSubscriber<object> stop)
    {
        public string Name => name;
        public Func<bool> Ready => ready;
        public ICallGateSubscriber<List<Vector3>, bool, object> MoveTo => moveTo;
        public ICallGateSubscriber<object> Stop => stop;
    }

    private readonly Backend[] backends;
    private DateTime lastProbe;
    private Backend? active;   // last-resolved ready backend (cached between probes)
    private Backend? driving;  // backend we last issued a MoveTo to, so Stop targets the right one

    public NavmeshIPC()
    {
        var pi = Service.PluginInterface;

        var ariConnected = pi.GetIpcSubscriber<bool>("Ariadne.IsConnected");
        var ariZone = pi.GetIpcSubscriber<int>("Ariadne.ZoneStatus");
        var vnavReady = pi.GetIpcSubscriber<bool>("vnavmesh.Nav.IsReady");

        this.backends =
        [
            new Backend("Ariadne",
                () => ariConnected.InvokeFunc() && ariZone.InvokeFunc() is 2 or 3,
                pi.GetIpcSubscriber<List<Vector3>, bool, object>("Ariadne.Path.MoveTo"),
                pi.GetIpcSubscriber<object>("Ariadne.Path.Stop")),
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
    public void MoveTo(Vector3 dest)
    {
        var b = this.Resolve();
        if (b == null)
            return;
        try { b.MoveTo.InvokeAction([dest], false); this.driving = b; }
        catch { /* backend unloaded mid-flight — ignore */ }
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
