using System;
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

    public void MoveTo(WPos target)
    {
        this.target = target;

        // prefer the navmesh: hand it the destination and let it drive (our hook yields via PathRunning).
        // re-issue only when the target moves meaningfully, so we don't restart the path every frame.
        if (this.UsingNavmesh)
        {
            if ((this.navTarget is not { } t || !t.AlmostEqual(target, ArrivalRadius))
                && GameData.TryLocalPlayerPose(out var pos, out _))
            {
                this.nav.MoveTo(new Vector3(target.X, pos.Y, target.Z)); // keep the player's Y (ground height)
                this.navTarget = target;
            }
        }
        else if (this.navTarget != null)
        {
            // navmesh went away (unloaded / left zone) mid-dodge — stop its path, fall back to the raw hook
            this.nav.Stop();
            this.navTarget = null;
        }
    }

    public void Stop()
    {
        this.target = null;
        if (this.navTarget != null)
        {
            this.nav.Stop();
            this.navTarget = null;
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
