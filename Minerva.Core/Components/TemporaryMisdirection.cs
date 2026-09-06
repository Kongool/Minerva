namespace Minerva.Components;

/// <summary>
/// "Temporary misdirection": a debuff that reverses/randomises movement input, so the auto-dodge must
/// stop steering while it is up. Subclasses name the cast that applies it; the statuses themselves are
/// the shared game-wide misdirection ids. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public abstract class TemporaryMisdirection(ModuleBase module, uint aid, string hint = "Applies temporary misdirection") : CastHint(module, aid, hint)
{
    /// <summary>The game's temporary-misdirection status ids (all expansions).</summary>
    private static readonly uint[] misdirectionSIDs = [1422u, 2936u, 3694u, 3909u];

    private BitMask mask;

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (Array.IndexOf(misdirectionSIDs, status.ID) >= 0)
            this.mask.Set(this.World.Party.FindSlot(actor.InstanceID));
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (Array.IndexOf(misdirectionSIDs, status.ID) >= 0)
            this.mask.Clear(this.World.Party.FindSlot(actor.InstanceID));
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (this.mask[slot])
            hints.AddSpecialMode(AIHints.SpecialMode.Misdirection, default);
    }
}
