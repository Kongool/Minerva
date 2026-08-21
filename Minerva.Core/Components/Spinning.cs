namespace Minerva.Components;

/// <summary>
/// "Spinning": a status that walks the player forward on its own, so the safe play is to face somewhere
/// harmless and let it run. Adds a forward look-ahead box and forbids the rear cone (turning around).
/// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public abstract class Spinning(ModuleBase module, uint aid, bool createForbiddenZones = true, uint statusID = 2973u, string hint = "Applies spinning") : CastHint(module, aid, hint)
{
    protected BitMask Mask;
    private readonly uint statusID = statusID;
    private const float SpinningLookahead = 5.5f;

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == this.statusID)
            this.Mask.Set(this.World.Party.FindSlot(actor.InstanceID));
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == this.statusID)
            this.Mask.Clear(this.World.Party.FindSlot(actor.InstanceID));
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (!createForbiddenZones || !this.Mask[slot])
            return;
        var pos = actor.Position;
        var rot = actor.Rotation;
        // simulate the forced forward walk; the rect is offset behind the player because player-centred
        // shapes make the pathfinder thrash (BMR's note, kept)
        hints.AddForbiddenZone(new SDRect(pos, rot, SpinningLookahead, SpinningLookahead + 2f, SpinningLookahead + 2f), this.World.FutureTime(2d));
        hints.AddForbiddenZone(new SDCone(pos, 100f, rot + 180f.Degrees(), 45f.Degrees()), DateTime.MaxValue);
    }
}
