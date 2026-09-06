using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.Config;
using Dalamud.Hooking;
using Minerva;
using Minerva.GameSync;

namespace Minerva.Automation;

/// <summary>
/// Real character-steering controller for auto-dodge. Two backends, preferred in order:
/// <list type="number">
/// <item><b>Navmesh</b> — if the Ariadne plugin (vnavmesh-compatible) is loaded with a mesh for the
/// zone, we hand the dodge target to its <c>Path.MoveTo</c> IPC, which paths around geometry and owns
/// the movement input. Our own hook yields to it (via the shared <c>PathIsRunning</c> flag) so the two
/// never fight over the same input.</item>
/// <item><b>Raw walk override</b> — otherwise we hook the game's walk-input reader
/// (<c>PlayerMoveController::readInput</c>, "RMIWalk") and, when a dodge target is set and the player
/// isn't steering themselves, override the sampled movement toward the target.</item>
/// </list>
/// <para>
/// The raw path is a deliberately trimmed clean-room port of BossmodReborn's <c>MovementOverride</c>:
/// walk only — no fly, misdirection, or spinning. The three signatures and the input convention
/// (sumLeft/sumForward relative to the forward-movement direction) are reused from BMR as hard-won
/// game constants; the logic is our own. Guarded so a stale signature or a detour fault can never take
/// the game's movement down — it just falls back to vanilla input.
/// </para>
/// </summary>
public sealed unsafe class MovementController : IMovementController, IDisposable
{
    // Stop overriding once we're within this radius of the target, so we glide in instead of jittering on top of it.
    private const float ArrivalRadius = 0.5f;

    /// <summary>
    /// How much waypoint-pass slack to ask a path-follower for on a dodge route, in yards.
    ///
    /// <para>Tolerance is how close the character has to get to a corner before heading for the next one,
    /// so a loose value cuts corners — and these corners exist to go around an AOE, which makes cutting one
    /// the same as clipping the thing it was avoiding. Asked for per-path so the user's global travel
    /// tolerance is left alone; backends without a per-path option get their own default.</para>
    /// </summary>
    private const float DodgeWaypointTolerance = 0.25f;

    // self, sumLeft, sumForward, sumTurnLeft, haveBackwardOrStrafe, a6, bAdditiveUnk
    private delegate void RMIWalkDelegate(void* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk);
    private delegate bool RMIWalkIsInputEnabledDelegate(void* self);

    private readonly Configuration config;
    private readonly NavmeshIPC nav = new();
    private readonly Hook<RMIWalkDelegate>? walkHook;
    private readonly RMIWalkIsInputEnabledDelegate? inputEnabled1;
    private readonly RMIWalkIsInputEnabledDelegate? inputEnabled2;

    private WPos? target;   // current dodge destination, or null when we shouldn't steer
    private WPos? navTarget; // destination last handed to the navmesh, to avoid re-issuing every frame
    private IReadOnlyList<WPos>? navRoute; // and the path that went with it, for the same reason
    private int navStalls;                 // stalls seen on the path currently being walked
    private bool legacyMode; // "legacy" control scheme: movement is relative to the camera, not the character

    /// <summary>Whether the walk-input hook resolved and installed. If false, raw steering can't work
    /// (signature likely outdated); auto-move then depends on the navmesh backend. Surfaced for diagnosis.</summary>
    public bool HookInstalled => this.walkHook != null;

    /// <summary>Whether a navmesh backend (Ariadne, else vnavmesh) is present and ready to drive movement.</summary>
    public bool UsingNavmesh => this.config.UseNavmesh && this.nav.Ready();

    /// <summary>Name of the active navmesh backend ("Ariadne" / "vnavmesh"), or null when steering directly.</summary>
    public string? NavmeshBackend => this.UsingNavmesh ? this.nav.ActiveName : null;

    /// <summary>True while we're actively moving the character — either overriding walk input, or the navmesh is following our path.</summary>
    public bool Steering => this.rawSteering || (this.navTarget != null && this.nav.PathRunning());

    /// <summary>Which mover the last MoveTo went through; None after Stop.</summary>
    public Mover Mode { get; private set; }

    /// <summary>The mover is actually driving (see <see cref="Steering"/>).</summary>
    public bool Busy => this.Steering;

    private bool rawSteering; // set on frames the raw override wrote input

    public MovementController(Configuration config)
    {
        this.config = config;
        try
        {
            var enabled1 = Service.SigScanner.ScanText("E8 ?? ?? ?? ?? 84 C0 75 10 38 43 3C");
            var enabled2 = Service.SigScanner.ScanText("E8 ?? ?? ?? ?? 84 C0 75 03 88 47 3F");
            this.inputEnabled1 = Marshal.GetDelegateForFunctionPointer<RMIWalkIsInputEnabledDelegate>(enabled1);
            this.inputEnabled2 = Marshal.GetDelegateForFunctionPointer<RMIWalkIsInputEnabledDelegate>(enabled2);

            this.walkHook = Service.GameInterop.HookFromSignature<RMIWalkDelegate>("E8 ?? ?? ?? ?? 80 7B 3E 00 48 8D 3D", this.RMIWalkDetour);
            this.walkHook.Enable();
            Service.Log.Information("Minerva: movement controller installed (auto-move available).");
        }
        catch (Exception ex)
        {
            // no hook -> the controller is inert (MoveTo is a no-op). Guidance/radar still work.
            Service.Log.Warning(ex, "Minerva: failed to install movement hook (signature may be outdated). Auto-move disabled; guidance only.");
        }

        Service.GameConfig.UiControlChanged += this.OnConfigChanged;
        this.UpdateLegacyMode();
    }

    /// <summary>
    /// Turn to face a direction, via the game's own auto-face routine. Unlike <see cref="MoveTo"/> this
    /// needs no movement hook — it is the same call the game makes when an ability faces its target.
    /// </summary>
    public void Face(Angle direction) => GameData.TryFace(direction.Rad);

    public void MoveTo(WPos target) => this.MoveTo(target, null);

    /// <summary>
    /// Steer toward <paramref name="target"/>, following <paramref name="route"/> when a navmesh backend is
    /// driving and one was computed.
    ///
    /// <para>The route matters because the backends walk the points they are handed and do no pathfinding
    /// of their own. Given only a destination, the follower takes the straight chord to it — through any
    /// AOE the dodge was bending around, with our own steering hook standing down for the duration.
    /// Handing over the corners makes it walk the safe path instead.</para>
    /// </summary>
    public void MoveTo(WPos target, IReadOnlyList<WPos>? route)
    {
        this.target = target;
        this.Mode = this.UsingNavmesh ? Mover.NavPath : this.HookInstalled ? Mover.Hook : Mover.None;

        // prefer the navmesh: hand it the path and let it drive (our hook yields via PathRunning).
        // re-issue only when the route actually changes, so we don't restart the follower every frame --
        // each Move() call clears its waypoint list and resets its stall and progress detectors.
        if (this.UsingNavmesh)
        {
            // A route with no bend in it is a straight move to a point, which is what SteerTo is for:
            // re-issued every tick against a solve that re-picks the point every tick, with no waypoint
            // list, no pass tolerance and no stall recovery in between to get out of step with us. A route
            // that actually bends still goes as a path -- the corners are the safety, and handing them over
            // one tick at a time would lose them the moment a frame is slow.
            var bends = route is { Count: > 1 };
            if (!bends && this.nav.CanSteer && GameData.TryLocalPlayerPose(out var here, out _))
            {
                this.nav.Steer(new Vector3(target.X, here.Y, target.Z));
                this.Mode = Mover.NavSteer;
                this.navTarget = target;
                this.navRoute = route;
                return;
            }

            if (this.RouteChanged(target, route) && GameData.TryLocalPlayerPose(out var pos, out _))
            {
                var path = new List<Vector3>(route?.Count ?? 1);
                if (route is { Count: > 0 })
                {
                    for (var i = 0; i < route.Count; ++i)
                        path.Add(new Vector3(route[i].X, pos.Y, route[i].Z)); // keep the player's Y (ground height)
                }
                else
                {
                    path.Add(new Vector3(target.X, pos.Y, target.Z));
                }

                this.nav.MoveTo(path, DodgeWaypointTolerance);
                this.navTarget = target;
                this.navRoute = route;
                this.navStalls = 0; // the follower resets its own count on every Move
            }
        }
        else if (this.navTarget != null)
        {
            // navmesh went away (unloaded / left zone) mid-dodge — stop its path, fall back to the raw hook
            this.nav.Stop();
            this.navTarget = null;
            this.navRoute = null;
        }
    }

    /// <summary>
    /// Is the plan different enough from the one already being walked to be worth re-issuing?
    /// <para>Comparing only the destination is not enough: a new AOE can bend the route around something
    /// while the far end stays put, and the follower would keep walking the old line straight through it.</para>
    /// </summary>
    private bool RouteChanged(WPos target, IReadOnlyList<WPos>? route)
    {
        if (this.navTarget is not { } t || !t.AlmostEqual(target, ArrivalRadius))
            return true;
        var prev = this.navRoute;
        if (route is null || prev is null)
            return (route is null) != (prev is null);
        if (route.Count != prev.Count)
            return true;
        for (var i = 0; i < route.Count; ++i)
            if (!route[i].AlmostEqual(prev[i], ArrivalRadius))
                return true;

        // The follower does not recover an externally-supplied path -- it keeps walking our corners rather
        // than re-pathing around them through a mesh that knows nothing about the AOE, and reports being
        // stuck instead. Recovery is therefore ours: re-issue, which hands it a route freshly solved from
        // where the character actually is. A knockback or stun also ticks this, so one stall is not
        // evidence the route is wrong -- but re-issuing an unchanged route costs nothing either.
        var stalls = this.nav.StallCount();
        if (stalls > this.navStalls)
        {
            this.navStalls = stalls;
            return true;
        }

        return false;
    }

    public void Stop()
    {
        this.Mode = Mover.None;
        this.target = null;
        if (this.navTarget != null)
        {
            this.nav.Stop();
            this.navTarget = null;
            this.navRoute = null;
        }
    }

    private void RMIWalkDetour(void* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk)
    {
        // always let the game sample real input first, so vanilla movement is untouched when we don't override
        this.walkHook!.Original(self, sumLeft, sumForward, sumTurnLeft, haveBackwardOrStrafe, a6, bAdditiveUnk);

        try
        {
            this.rawSteering = false;

            // yield to a navmesh path-follower (Ariadne or plain vnavmesh) if one is driving — never fight it
            if (this.nav.PathRunning())
                return;

            // only act on the real input-sampling pass (bAdditiveUnk == 0)
            if (bAdditiveUnk != 0 || this.target is not { } dest)
                return;

            // never fight the player: if they're already feeding movement input, defer to them entirely
            if (*sumLeft != 0f || *sumForward != 0f)
                return;

            // respect the game's own "is input allowed right now" gates (cutscenes, occupied states, ...)
            if (this.inputEnabled1 != null && !this.inputEnabled1(self))
                return;
            if (this.inputEnabled2 != null && !this.inputEnabled2(self))
                return;

            if (this.TryComputeMove(dest, out var move))
            {
                *sumLeft = move.X;
                *sumForward = move.Z;
                this.rawSteering = true;
            }
        }
        catch (Exception ex)
        {
            // a fault here would corrupt movement every frame — swallow it and leave vanilla input in place
            Service.Log.Error(ex, "Minerva: movement override faulted; using vanilla input this frame.");
        }
    }

    /// <summary>
    /// Convert a world destination into the game's (left, forward) input pair. The pair is expressed
    /// relative to the forward-movement direction — the character's facing in standard mode, or the
    /// camera azimuth (+180°) in legacy mode — so we rotate the world direction into that local frame.
    /// </summary>
    private bool TryComputeMove(WPos dest, out WDir move)
    {
        move = default;
        if (!GameData.TryLocalPlayerPose(out var pos3, out var rotation))
            return false;

        var toDest = dest - new WPos(pos3.X, pos3.Z);
        if (toDest.LengthSq() <= ArrivalRadius * ArrivalRadius)
            return false; // close enough — stop steering so we don't oscillate on the target

        var worldAngle = toDest.Normalized().ToAngle();
        var forward = this.legacyMode && GameData.TryCameraAzimuth(out var azimuth)
            ? new Angle(azimuth + MathF.PI)   // legacy: forward is the camera direction (azimuth + 180°)
            : new Angle(rotation);             // standard: forward is the character's facing
        move = (worldAngle - forward).Normalized().ToDirection();
        return true;
    }

    private void OnConfigChanged(object? sender, ConfigChangeEvent evt) => this.UpdateLegacyMode();

    private void UpdateLegacyMode()
        => this.legacyMode = Service.GameConfig.UiControl.TryGetUInt("MoveMode", out var mode) && mode == 1;

    public void Dispose()
    {
        Service.GameConfig.UiControlChanged -= this.OnConfigChanged;
        if (this.navTarget != null)
            this.nav.Stop();
        this.walkHook?.Dispose();
        this.target = null;
    }
}
