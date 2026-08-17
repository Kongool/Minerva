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
/// Generic per-actor hint while a specific cast is up — e.g. an interruptible or tankbuster cue.
/// </summary>
public class CastHint(ModuleBase module, uint aid, string hint) : ModuleComponent(module)
{
    public readonly uint WatchedAction = aid;
    public readonly string Hint = hint;
    protected int active;

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction)
            this.active++;
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction && this.active > 0)
            this.active--;
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (this.active > 0)
            hints.Add(this.Hint);
    }
}

/// <summary>A single-target cast cue (e.g. a tankbuster) — a hint with a default label. Mirrors BMR's
/// <c>SingleTargetCast</c> so ported modules using it compile unchanged.</summary>
public class SingleTargetCast(ModuleBase module, uint aid, string hint = "Tankbuster") : CastHint(module, aid, hint);
