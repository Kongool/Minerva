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
    private readonly ICallGateProvider<bool> isSteering;
    private readonly ICallGateProvider<float> safeFacingGate;
    private readonly ICallGateProvider<float> secondsUntilMustNotAct;
    private readonly ICallGateProvider<float> secondsUntilMustNotMove;
    private readonly ICallGateProvider<float> secondsUntilGaze;
    private readonly ICallGateProvider<float> maxCastTime;
    private readonly ICallGateProvider<System.Numerics.Vector3, bool> isPositionSafe;
    private readonly ICallGateProvider<System.Numerics.Vector3, System.Numerics.Vector3, bool> isDashSafe;
    private readonly ICallGateProvider<float> secondsUntilRaidwide;
    private readonly ICallGateProvider<float> secondsUntilTankbuster;
    private readonly ICallGateProvider<float> secondsUntilSharedDamage;
    private readonly ICallGateProvider<uint> nextTankbusterTargets;
    private readonly ICallGateProvider<ulong> tankSwapCurrentTank;
    private readonly ICallGateProvider<float> secondsUntilTankSwap;
    private readonly ICallGateProvider<ulong[]> priorityTargets;
    private readonly ICallGateProvider<ulong[]> forbiddenTargets;
    private readonly ICallGateProvider<ulong[]> deprioritizedTargets;
    private readonly ICallGateProvider<ulong> forcedTarget;
    private readonly ICallGateProvider<ulong[]> targetsToInterrupt;
    private readonly ICallGateProvider<ulong[]> targetsToStun;
    private readonly ICallGateProvider<ulong, int> pendingHPDifference;
    private readonly ICallGateProvider<ulong, int> pendingHPRaw;
    private readonly ICallGateProvider<ulong, uint[]> pendingStatuses;
    private readonly ICallGateProvider<int[]> partyPendingHP;
    private readonly ICallGateProvider<int[]> cleanseTargets;
    private readonly ICallGateProvider<ulong, bool> cleansePending;
    private readonly ICallGateProvider<int, double, bool> requestPositional;
    private readonly ICallGateProvider<double, bool> requestHold;
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

        // Whether Minerva's own steering is moving the character this frame. A rotation yields its own
        // positional hops to it the way it yields to BossmodReborn's AI: two movers on one character
        // stutter, and this is the flag that decides who owns the feet.
        this.isSteering = pi.GetIpcProvider<bool>("Minerva.IsSteering");
        this.isSteering.RegisterFunc(() => this.ai.Movement is Minerva.Automation.MovementController { Steering: true });

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

        // The positional half of BossmodReborn's Hints surface. MaxCastTime answers "how long may I stand
        // here", which is what a hard-cast raise needs; these answer "is that spot / that dash any good",
        // which is what deciding WHERE to raise from needs. A revival routine wants both: somewhere safe to
        // stand, and long enough to finish the cast once standing there.
        //
        // Not time-aware, matching BMR: they test the zones that exist right now. A consumer must not read
        // "safe" here as "safe for the duration of my cast" — that is MaxCastTime's question, and conflating
        // them is how a raise gets started one second before a mechanic lands.
        this.isPositionSafe = pi.GetIpcProvider<System.Numerics.Vector3, bool>("Minerva.Hints.IsPositionSafe");
        this.isPositionSafe.RegisterFunc(to => this.ai.IsPositionSafe(to));

        this.isDashSafe = pi.GetIpcProvider<System.Numerics.Vector3, System.Numerics.Vector3, bool>("Minerva.Hints.IsDashSafe");
        this.isDashSafe.RegisterFunc((from, to) => this.ai.IsDashSafe(from, to));

        // Incoming damage, by kind and by target. BossmodReborn splits these across NextDamageIn and
        // NextTankbusterDamageIn; the target mask has no BMR equivalent and is the reason a consumer can
        // decide a tank swap rather than just a mitigation.
        this.secondsUntilRaidwide = pi.GetIpcProvider<float>("Minerva.Hints.SecondsUntilRaidwide");
        this.secondsUntilRaidwide.RegisterFunc(() => this.ai.SecondsUntilRaidwide);

        this.secondsUntilTankbuster = pi.GetIpcProvider<float>("Minerva.Hints.SecondsUntilTankbuster");
        this.secondsUntilTankbuster.RegisterFunc(() => this.ai.SecondsUntilTankbuster);

        this.secondsUntilSharedDamage = pi.GetIpcProvider<float>("Minerva.Hints.SecondsUntilSharedDamage");
        this.secondsUntilSharedDamage.RegisterFunc(() => this.ai.SecondsUntilSharedDamage);

        this.nextTankbusterTargets = pi.GetIpcProvider<uint>("Minerva.Hints.NextTankbusterTargets");
        this.nextTankbusterTargets.RegisterFunc(() => this.ai.NextTankbusterTargets);

        // The tank swap, as an identity rather than a printed word. BossmodReborn has the hint and does not
        // expose it, so a consumer driving both tanks cannot act on it — which is the case that matters
        // when one plugin plays the whole party.
        this.tankSwapCurrentTank = pi.GetIpcProvider<ulong>("Minerva.Hints.TankSwapCurrentTank");
        this.tankSwapCurrentTank.RegisterFunc(() => this.ai.TankSwapCurrentTank);

        this.secondsUntilTankSwap = pi.GetIpcProvider<float>("Minerva.Hints.SecondsUntilTankSwap");
        this.secondsUntilTankSwap.RegisterFunc(() => this.ai.SecondsUntilTankSwap);

        // Targeting. The fight was authored; the rotation only sees a list of hostiles. These say which of
        // them matters -- kill this add before the boss, bring these two down together, do not waste a
        // cooldown on the invincible one -- which is knowledge no rotation can derive on its own.
        this.priorityTargets = pi.GetIpcProvider<ulong[]>("Minerva.Hints.PriorityTargets");
        this.priorityTargets.RegisterFunc(() => this.ai.PriorityTargets);

        this.forbiddenTargets = pi.GetIpcProvider<ulong[]>("Minerva.Hints.ForbiddenTargets");
        this.forbiddenTargets.RegisterFunc(() => this.ai.ForbiddenTargets);

        this.deprioritizedTargets = pi.GetIpcProvider<ulong[]>("Minerva.Hints.DeprioritizedTargets");
        this.deprioritizedTargets.RegisterFunc(() => this.ai.DeprioritizedTargets);

        this.forcedTarget = pi.GetIpcProvider<ulong>("Minerva.Hints.ForcedTarget");
        this.forcedTarget.RegisterFunc(() => this.ai.ForcedTargetId);

        this.targetsToInterrupt = pi.GetIpcProvider<ulong[]>("Minerva.Hints.TargetsToInterrupt");
        this.targetsToInterrupt.RegisterFunc(() => this.ai.TargetsToInterrupt);

        this.targetsToStun = pi.GetIpcProvider<ulong[]>("Minerva.Hints.TargetsToStun");
        this.targetsToStun.RegisterFunc(() => this.ai.TargetsToStun);

        // Damage that has been announced but not drawn. The sharpest healing signal Minerva has: the other
        // timers say something is coming, these say it has LANDED, on whom, for how much -- while the HP
        // bar still reads full. Reacting after the bar moves is reacting after the information existed.
        this.pendingHPDifference = pi.GetIpcProvider<ulong, int>("Minerva.Hints.PendingHPDifference");
        this.pendingHPDifference.RegisterFunc(id => this.ai.PendingHPDifference(id));

        this.pendingHPRaw = pi.GetIpcProvider<ulong, int>("Minerva.Hints.PendingHPRaw");
        this.pendingHPRaw.RegisterFunc(id => this.ai.PendingHPRaw(id));

        this.pendingStatuses = pi.GetIpcProvider<ulong, uint[]>("Minerva.Hints.PendingStatuses");
        this.pendingStatuses.RegisterFunc(id => this.ai.PendingStatuses(id));

        this.partyPendingHP = pi.GetIpcProvider<int[]>("Minerva.Party.PendingHP");
        this.partyPendingHP.RegisterFunc(() => this.ai.PartyPendingHP());

        // Which party slots the FIGHT says to cleanse -- not "who has a debuff", which the rotation can see
        // for itself and mostly should ignore. Paired with a check for a cleanse already in flight, because
        // the status stays on the bar until the first Esuna resolves and a boxed party will throw four.
        this.cleanseTargets = pi.GetIpcProvider<int[]>("Minerva.Party.CleanseTargets");
        this.cleanseTargets.RegisterFunc(() => this.ai.CleanseTargets);

        this.cleansePending = pi.GetIpcProvider<ulong, bool>("Minerva.Hints.CleansePending");
        this.cleansePending.RegisterFunc(id => this.ai.CleansePending(id));

        // A rotation knows which side its next weaponskill wants; Minerva knows where it is safe to stand.
        // Neither can answer alone, so the rotation states the requirement and the dodge honours it where
        // safety allows. The mask is Positional flags, so "rear or flank" is one call rather than two.
        this.requestPositional = pi.GetIpcProvider<int, double, bool>("Minerva.RequestPositional");
        this.requestPositional.RegisterFunc((mask, seconds) =>
        {
            this.ai.RequestPositional((Positional)mask, seconds);
            return true;
        });

        // The other half of that bargain. RequestPositional says where to stand; this says "stop moving me
        // at all for a moment, I am out of position deliberately". Minerva cannot infer that -- a healer
        // parked 30y away at a corpse is indistinguishable from one who drifted -- so a hardcast raise
        // under auto-dodge needs the rotation to say it out loud. Danger still overrides it.
        this.requestHold = pi.GetIpcProvider<double, bool>("Minerva.RequestHold");
        this.requestHold.RegisterFunc(seconds =>
        {
            this.ai.RequestHold(seconds);
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
        this.isSteering.UnregisterFunc();
        this.safeFacingGate.UnregisterFunc();
        this.secondsUntilMustNotAct.UnregisterFunc();
        this.secondsUntilMustNotMove.UnregisterFunc();
        this.secondsUntilGaze.UnregisterFunc();
        this.maxCastTime.UnregisterFunc();
        this.isPositionSafe.UnregisterFunc();
        this.isDashSafe.UnregisterFunc();
        this.secondsUntilRaidwide.UnregisterFunc();
        this.secondsUntilTankbuster.UnregisterFunc();
        this.secondsUntilSharedDamage.UnregisterFunc();
        this.nextTankbusterTargets.UnregisterFunc();
        this.tankSwapCurrentTank.UnregisterFunc();
        this.secondsUntilTankSwap.UnregisterFunc();
        this.priorityTargets.UnregisterFunc();
        this.forbiddenTargets.UnregisterFunc();
        this.deprioritizedTargets.UnregisterFunc();
        this.forcedTarget.UnregisterFunc();
        this.targetsToInterrupt.UnregisterFunc();
        this.targetsToStun.UnregisterFunc();
        this.pendingHPDifference.UnregisterFunc();
        this.pendingHPRaw.UnregisterFunc();
        this.pendingStatuses.UnregisterFunc();
        this.partyPendingHP.UnregisterFunc();
        this.cleanseTargets.UnregisterFunc();
        this.cleansePending.UnregisterFunc();
        this.requestPositional.UnregisterFunc();
        this.requestHold.UnregisterFunc();
        this.listPresets.UnregisterFunc();
        this.activePreset.UnregisterFunc();
        this.applyPreset.UnregisterFunc();
        this.releasePreset.UnregisterFunc();
    }
}
