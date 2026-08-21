using System;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace Minerva.Automation;

/// <summary>
/// Minerva's provider-side IPC: what other plugins may read out of the active boss module. Shaped after
/// the Ariadne consumer surface documented in <c>docs/consumer-ipc.md</c> — a shared-data flag for the
/// hot per-frame read, plus call gates for everything else.
/// <para>
/// The motivating consumer is a rotation plugin (Daedalus): several fights punish *any* action, not just
/// movement — The Clyteum's Eye of the Scorpion Motion Tracker, and every Pyretic variant. A rotation that
/// only knows a hardcoded status-id list misses each new fight; reading <c>MustNotAct</c> instead means it
/// stops for whatever the active Minerva module says is a stand-still punisher, including the cases driven
/// by beam overlap rather than by a status.
/// </para>
/// </summary>
internal sealed class MinervaIpc : IDisposable
{
    /// <summary>Shared-data tag for the hot path: <c>flag[0]</c> is true while any action would punish.
    /// Consumers read it every frame with no try/catch cost, exactly like <c>ariadne.PathIsRunning</c>.</summary>
    public const string MustNotActTag = "minerva.MustNotAct";

    /// <summary>Shared-data tag: <c>flag[0]</c> is true while movement would punish.</summary>
    public const string MustNotMoveTag = "minerva.MustNotMove";

    /// <summary>
    /// Shared-data tag: <c>flag[0]</c> is true while a gaze constrains facing.
    /// <para>A rotation plugin should hold here, not because acting is punished, but because the game turns
    /// you toward your target when you act — so continuing to cast undoes the turn away from the gaze. This
    /// is the hot-path twin of <see cref="Minerva.Automation.AIManager.FacingConstrained"/>.</para>
    /// </summary>
    public const string MustNotTurnTag = "minerva.MustNotTurn";

    private readonly AIManager ai;
    private readonly bool[] mustNotAct;
    private readonly bool[] mustNotMove;
    private readonly bool[] mustNotTurn;

    private readonly ICallGateProvider<bool> isConnected;
    private readonly ICallGateProvider<bool> mustNotActGate;
    private readonly ICallGateProvider<bool> mustNotMoveGate;
    private readonly ICallGateProvider<string> activeModule;
    private readonly ICallGateProvider<bool> mustNotTurnGate;
    private readonly ICallGateProvider<float> safeFacingGate;
    private readonly ICallGateProvider<float> secondsUntilMustNotAct;
    private readonly ICallGateProvider<float> secondsUntilMustNotMove;
    private readonly ICallGateProvider<float> secondsUntilGaze;
    private readonly ICallGateProvider<float> maxCastTime;
    private readonly ICallGateProvider<int, double, bool> requestPositional;
    private readonly ICallGateProvider<string[]> listPresets;
    private readonly ICallGateProvider<string> activePreset;
    private readonly ICallGateProvider<string, string, bool> applyPreset;
    private readonly ICallGateProvider<string, bool> releasePreset;

    public MinervaIpc(IDalamudPluginInterface pi, AIManager ai, Modules.ModuleManager modules, DodgePresets presets)
    {
        this.ai = ai;

        this.mustNotAct = pi.GetOrCreateData<bool[]>(MustNotActTag, () => [false]);
        this.mustNotMove = pi.GetOrCreateData<bool[]>(MustNotMoveTag, () => [false]);
        this.mustNotTurn = pi.GetOrCreateData<bool[]>(MustNotTurnTag, () => [false]);

        this.isConnected = pi.GetIpcProvider<bool>("Minerva.IsConnected");
        this.isConnected.RegisterFunc(() => true);

        this.mustNotActGate = pi.GetIpcProvider<bool>("Minerva.MustNotAct");
        this.mustNotActGate.RegisterFunc(() => this.ai.MustNotAct);

        this.mustNotMoveGate = pi.GetIpcProvider<bool>("Minerva.MustNotMove");
        this.mustNotMoveGate.RegisterFunc(() => this.ai.MustNotMove);

        this.activeModule = pi.GetIpcProvider<string>("Minerva.ActiveModule");
        this.activeModule.RegisterFunc(() => modules.ActiveModule?.GetType().Name ?? string.Empty);

        this.mustNotTurnGate = pi.GetIpcProvider<bool>("Minerva.MustNotTurn");
        this.mustNotTurnGate.RegisterFunc(() => this.ai.FacingConstrained);

        // radians in the game's own convention; NaN when nothing constrains facing, so a consumer can tell
        // "no gaze" from "a gaze that wants you pointed at 0" without a second call
        this.safeFacingGate = pi.GetIpcProvider<float>("Minerva.SafeFacing");
        this.safeFacingGate.RegisterFunc(() => this.ai.SafeFacing?.Rad ?? float.NaN);

        // Lead time, not just state. The flags above are present tense, and a rotation that only reads
        // present tense reacts a GCD too late -- it has already committed a hardcast that resolves inside
        // the mechanic. These say how long it has, so it can decline to start rather than be interrupted.
        // Call gates rather than shared data: a rotation asks at GCD boundaries, not every frame.
        this.secondsUntilMustNotAct = pi.GetIpcProvider<float>("Minerva.SecondsUntilMustNotAct");
        this.secondsUntilMustNotAct.RegisterFunc(() => this.ai.SecondsUntilMustNotAct);

        this.secondsUntilMustNotMove = pi.GetIpcProvider<float>("Minerva.SecondsUntilMustNotMove");
        this.secondsUntilMustNotMove.RegisterFunc(() => this.ai.SecondsUntilMustNotMove);

        // the one a gaze fight actually needs: how long until facing is snapshotted
        this.secondsUntilGaze = pi.GetIpcProvider<float>("Minerva.SecondsUntilGaze");
        this.secondsUntilGaze.RegisterFunc(() => this.ai.SecondsUntilGaze);

        // "Is this spot safe for eight seconds so I can raise?" -- same name, units and meaning as
        // BossmodReborn's Hints.MaxCastTime, so a consumer already asking BMR ports the call as a rename.
        this.maxCastTime = pi.GetIpcProvider<float>("Minerva.MaxCastTime");
        this.maxCastTime.RegisterFunc(() => this.ai.MaxCastTime);

        // A rotation knows which side its next weaponskill wants; Minerva knows where it is safe to stand.
        // Neither can answer alone, so the rotation states the requirement and the dodge honours it where
        // safety allows. The mask is Positional flags, so "rear or flank" is one call rather than two.
        this.requestPositional = pi.GetIpcProvider<int, double, bool>("Minerva.RequestPositional");
        this.requestPositional.RegisterFunc((mask, seconds) =>
        {
            this.ai.RequestPositional((Positional)mask, seconds);
            return true;
        });

        // Presets are how a rotation states everything that is job-shaped rather than fight-shaped -- how
        // much clearance to keep, and how far inside a positional arc to stand (Monk wants the border,
        // Samurai does not care). One claimed slot rather than a setter per field: a plugin writing
        // individual settings is indistinguishable from the user's own config drifting.
        this.listPresets = pi.GetIpcProvider<string[]>("Minerva.ListPresets");
        this.listPresets.RegisterFunc(() => presets.All().ConvertAll(p => p.Name).ToArray());

        // empty while nothing is applied is impossible -- there is always an active preset -- so a caller
        // can compare this against what it asked for to notice the user taking the slot back
        this.activePreset = pi.GetIpcProvider<string>("Minerva.ActivePreset");
        this.activePreset.RegisterFunc(() => presets.Active);

        // false means the name is unknown: create it in Minerva first, or fall back to Default
        this.applyPreset = pi.GetIpcProvider<string, string, bool>("Minerva.ApplyPreset");
        this.applyPreset.RegisterFunc((name, owner) => presets.Apply(name, string.IsNullOrWhiteSpace(owner) ? null : owner));

        // only the holder may release, so a superseded caller cannot undo whatever replaced it
        this.releasePreset = pi.GetIpcProvider<string, bool>("Minerva.ReleasePreset");
        this.releasePreset.RegisterFunc(presets.Release);
    }

    /// <summary>Republish the per-frame flags. Called once per framework tick, after the AI update.</summary>
    public void Update()
    {
        this.mustNotAct[0] = this.ai.MustNotAct;
        this.mustNotMove[0] = this.ai.MustNotMove;
        this.mustNotTurn[0] = this.ai.FacingConstrained;
    }

    public void Dispose()
    {
        // leave the shared-data arrays cleared so a consumer that outlives us doesn't hold a stale "stop"
        this.mustNotAct[0] = false;
        this.mustNotMove[0] = false;
        this.mustNotTurn[0] = false;
        this.isConnected.UnregisterFunc();
        this.mustNotActGate.UnregisterFunc();
        this.mustNotMoveGate.UnregisterFunc();
        this.activeModule.UnregisterFunc();
        this.mustNotTurnGate.UnregisterFunc();
        this.safeFacingGate.UnregisterFunc();
        this.secondsUntilMustNotAct.UnregisterFunc();
        this.secondsUntilMustNotMove.UnregisterFunc();
        this.secondsUntilGaze.UnregisterFunc();
        this.maxCastTime.UnregisterFunc();
        this.requestPositional.UnregisterFunc();
        this.listPresets.UnregisterFunc();
        this.activePreset.UnregisterFunc();
        this.applyPreset.UnregisterFunc();
        this.releasePreset.UnregisterFunc();
    }
}
