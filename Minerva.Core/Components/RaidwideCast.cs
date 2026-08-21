namespace Minerva.Components;

/// <summary>
/// Shows a raid-wide advisory while any of the given casts is in progress — for unavoidable
/// party damage that has no dodge, just a "mitigate/heal" cue. No arena drawing.
/// </summary>
public class RaidwideCasts(ModuleBase module, uint[] aids, string hint = "Raidwide") : ModuleComponent(module)
{
    public readonly uint[] AIDs = aids;
    public readonly string Hint = hint;
    protected int active;

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (Array.IndexOf(this.AIDs, cast.Action.ID) >= 0)
            this.active++;
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (Array.IndexOf(this.AIDs, cast.Action.ID) >= 0 && this.active > 0)
            this.active--;
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (this.active > 0)
            hints.Add(this.Hint);
    }
}

/// <summary>Single-AID convenience form of <see cref="RaidwideCasts"/>.</summary>
public class RaidwideCast(ModuleBase module, uint aid, string hint = "Raidwide") : RaidwideCasts(module, [aid], hint);

/// <summary>
/// Unavoidable instant raidwide expected at <see cref="Activation"/>: a global "mitigate/heal" cue is
/// shown once armed until the AOE resolves. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// Minerva omits BMR's predicted-damage AI weighting (there is nothing to dodge).
/// </summary>
public class RaidwideInstant(ModuleBase module, uint aid, double delay = default, string hint = "Raidwide") : CastCounter(module, aid)
{
    public readonly double Delay = delay;
    public readonly string Hint = hint;
    public DateTime Activation; // default if inactive, otherwise expected resolve time

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (this.Activation != default && this.Hint.Length > 0)
            hints.Add(this.Hint);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == this.WatchedAction)
        {
            ++this.NumCasts;
            this.Activation = default;
        }
    }
}

/// <summary>
/// Raidwide telegraphed by a visual cast that resolves as an instant AOE <paramref name="delay"/> later.
/// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class RaidwideCastDelay(ModuleBase module, uint actionVisual, uint actionAOE, double delay, string hint = "Raidwide") : RaidwideInstant(module, actionAOE, delay, hint)
{
    public readonly uint ActionVisual = actionVisual;

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.ActionVisual)
            this.Activation = this.Module.CastFinishAt(cast, (float)this.Delay);
    }
}

/// <summary>A <see cref="RaidwideCastDelay"/> keyed off several visual/AOE action pairs.</summary>
public class RaidwideCastsDelay(ModuleBase module, uint[] aidsVisual, uint[] aidsAOE, double delay, string hint = "Raidwide") : RaidwideCastDelay(module, default, default, delay, hint)
{
    private readonly uint[] AIDsVisual = aidsVisual;
    private readonly uint[] AIDsAOE = aidsAOE;

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (Array.IndexOf(this.AIDsVisual, cast.Action.ID) >= 0)
            this.Activation = this.Module.CastFinishAt(cast, (float)this.Delay);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (Array.IndexOf(this.AIDsAOE, spell.Action.ID) >= 0)
        {
            ++this.NumCasts;
            this.Activation = default;
        }
    }
}

/// <summary>
/// Generic per-actor hint while a specific cast is up — e.g. an interruptible or tankbuster cue.
/// </summary>
public class CastHint(ModuleBase module, uint aid, string hint) : CastCounter(module, aid)
{
    public readonly string Hint = hint;

    /// <summary>Actors currently casting the watched action. Subclasses read this to reach the cast target.</summary>
    public readonly List<Actor> Casters = [];

    protected int active => this.Casters.Count;

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction)
            this.Casters.Add(caster);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction)
            this.Casters.Remove(caster);
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (this.Casters.Count > 0)
            hints.Add(this.Hint);
    }
}

/// <summary>Multi-AID form of <see cref="CastHint"/>.</summary>
public class CastHints(ModuleBase module, uint[] aids, string hint) : CastCounterMulti(module, aids)
{
    /// <summary>BMR-compatible alias for the inherited <see cref="CastCounterMulti.WatchedActions"/>.</summary>
    public uint[] AIDs => this.WatchedActions;

    public readonly string Hint = hint;
    protected int active;

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (Array.IndexOf(this.AIDs, cast.Action.ID) >= 0)
            this.active++;
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (Array.IndexOf(this.AIDs, cast.Action.ID) >= 0 && this.active > 0)
            this.active--;
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (this.active > 0)
            hints.Add(this.Hint);
    }
}

/// <summary>
/// Advises interrupting/stunning a specific cast while it is in progress. Ported from BossmodReborn's
/// CastInterruptHint (BSD-3; see THIRD-PARTY-NOTICES.txt); Minerva shows only the global text cue (its
/// auto-dodge does not issue interrupt/stun actions).
/// </summary>
public class CastInterruptHint(ModuleBase module, uint aid, bool canBeInterrupted = true, bool canBeStunned = false, string hintExtra = "", bool showNameInHint = false) : CastHint(module, aid, BuildHint(canBeInterrupted, canBeStunned, hintExtra))
{
    public readonly bool CanBeInterrupted = canBeInterrupted;
    public readonly bool CanBeStunned = canBeStunned;
    public readonly bool ShowNameInHint = showNameInHint;

    private static string BuildHint(bool canBeInterrupted, bool canBeStunned, string hintExtra)
    {
        var verbs = (canBeInterrupted, canBeStunned) switch
        {
            (true, true) => "Interrupt or stun!",
            (true, false) => "Interrupt!",
            (false, true) => "Stun!",
            _ => "Interruptible"
        };
        return hintExtra.Length > 0 ? $"{verbs} ({hintExtra})" : verbs;
    }
}

/// <summary>A single-target cast cue (e.g. a tankbuster) — a hint with a default label. Mirrors BMR's
/// <c>SingleTargetCast</c> so ported modules using it compile unchanged.</summary>
public class SingleTargetCast(ModuleBase module, uint aid, string hint = "Tankbuster") : CastHint(module, aid, hint);

/// <summary>Single-target cue telegraphed by a visual cast that resolves as an instant hit later.
/// Mirrors BMR's <c>SingleTargetCastDelay</c> (BSD-3); Minerva shows the hint while the visual cast is up.</summary>
public class SingleTargetCastDelay(ModuleBase module, uint actionVisual, uint actionAOE, double delay, string hint = "Tankbuster") : CastHint(module, actionVisual, hint)
{
    public readonly uint ActionAOE = actionAOE;
    public readonly double Delay = delay;
}

/// <summary>Delayable single-target cast cue. Minerva has no predicted-damage AI, so this behaves like
/// <see cref="SingleTargetCast"/>. Mirrors BMR's <c>SingleTargetDelayableCast</c> (BSD-3).</summary>
public class SingleTargetDelayableCast(ModuleBase module, uint aid, string hint = "Tankbuster") : SingleTargetCastDelay(module, aid, aid, default, hint);

/// <summary>Multi-AID form of <see cref="SingleTargetDelayableCast"/>. Mirrors BMR's <c>SingleTargetDelayableCasts</c> (BSD-3).</summary>
public class SingleTargetDelayableCasts(ModuleBase module, uint[] aids, string hint = "Tankbuster") : CastHints(module, aids, hint);

/// <summary>
/// Unavoidable single-target damage initiated by some custom condition and landing after a delay.
/// Subclasses fill <see cref="Targets"/>. The predicted damage is recorded for modules and analysis;
/// Minerva does not time mitigation off it. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class SingleTargetInstant(ModuleBase module, uint aid, double delay = default, string hint = "Tankbuster", AIHints.PredictedDamageType damageType = AIHints.PredictedDamageType.Tankbuster) : CastCounter(module, aid)
{
    public readonly double Delay = delay; // visual cast end -> cast event
    public readonly string Hint = hint;
    public readonly List<(int slot, DateTime activation, ulong instanceID, Actor caster, Actor target)> Targets = [];

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (this.Targets.Count != 0 && this.Hint.Length != 0)
            hints.Add(this.Hint);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        foreach (var t in this.Targets)
            hints.AddPredictedDamage(new BitMask().WithBit(t.slot), t.activation, damageType);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent cast)
    {
        if (cast.Action.ID != this.WatchedAction)
            return;
        ++this.NumCasts;
        var id = cast.MainTargetID;
        for (var i = 0; i < this.Targets.Count; ++i)
        {
            if (this.Targets[i].instanceID == id)
            {
                this.Targets.RemoveAt(i);
                return;
            }
        }
    }
}

/// <summary>Multi-AID form of <see cref="SingleTargetCast"/>. Ported from BossmodReborn (BSD-3).</summary>
public class SingleTargetCasts(ModuleBase module, uint[] aids, string hint = "Tankbuster") : SingleTargetCast(module, default, hint)
{
    private readonly uint[] aids = aids;

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (Array.IndexOf(this.aids, cast.Action.ID) >= 0)
            this.Casters.Add(caster);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (Array.IndexOf(this.aids, cast.Action.ID) >= 0)
            this.Casters.Remove(caster);
    }
}

/// <summary>
/// Unavoidable instant single-target damage announced by a (usually visual-only) cast event.
/// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class SingleTargetEventDelay(ModuleBase module, uint actionVisual, uint actionAOE, double delay, string hint = "Tankbuster") : SingleTargetInstant(module, actionAOE, delay, hint)
{
    public uint ActionVisual = actionVisual;

    public override void OnEventCast(Actor caster, ActorCastEvent cast)
    {
        base.OnEventCast(caster, cast);
        if (cast.Action.ID != this.ActionVisual)
            return;
        // a self-targeted visual actually lands on the caster's main target
        var target = cast.MainTargetID != caster.InstanceID ? cast.MainTargetID : caster.TargetID;
        if (this.World.Actors.Find(target) is { } t)
            this.Targets.Add((this.World.Party.FindSlot(target), this.World.FutureTime(this.Delay), target, caster, t));
    }
}

/// <summary>
/// Unavoidable instant raidwide announced by an NPC yell. NOTE: Minerva has no NpcYell packet hook yet
/// (category 455 is a PacketID, not an ActorControl category), so <see cref="OnActorNpcYell"/> never
/// fires and this component is inert until that hook lands. Ported from BossmodReborn (BSD-3).
/// </summary>
public class RaidwideAfterNPCYell(ModuleBase module, uint aid, uint npcYellID, double delay, string hint = "Raidwide") : RaidwideInstant(module, aid, delay, hint)
{
    public uint NPCYellID = npcYellID;

    public override void OnActorNpcYell(Actor actor, ushort id)
    {
        if (id == this.NPCYellID)
            this.Activation = this.World.FutureTime(this.Delay);
    }
}
