namespace Minerva.Components;

/// <summary>
/// Thin ice: a status makes the player slide a fixed distance forward whenever they move, so every step
/// must be aimed. Modelled as a forward directional knockback of <paramref name="distance"/>. Ported from
/// BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt); BMR additionally flips the slide direction to the
/// camera azimuth in legacy-movement mode, which Minerva's game-free core can't see, so the slide is
/// always along the player's facing. <paramref name="stopAfterWall"/> is accepted for source
/// compatibility — Minerva's knockback model stops <i>at</i> the wall, never past it.
/// </summary>
public abstract class ThinIce(ModuleBase module, float distance, bool createForbiddenZones = false, uint statusID = 911u, bool stopAtWall = false, bool stopAfterWall = false)
    : GenericKnockback(module, stopAtWall: stopAtWall || stopAfterWall)
{
    public readonly uint StatusID = statusID;
    public readonly float Distance = distance;
    public BitMask Mask;

    private static readonly WDir offset = new(default, 1f);

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor)
        => this.Mask[slot] ? new Knockback[1] { new(actor.Position, this.Distance, default, default, actor.Rotation, Kind.DirForward) } : [];

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == this.StatusID)
            this.Mask.Set(this.World.Party.FindSlot(actor.InstanceID));
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == this.StatusID)
            this.Mask.Clear(this.World.Party.FindSlot(actor.InstanceID));
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var movements = this.CalculateMovements(slot, actor);
        if (movements.Count != 0 && this.DestinationUnsafe(slot, actor, movements[0].to))
            hints.Add("You are risking to slide into danger!");
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        base.DrawArenaForeground(pcSlot, pc);
        if (this.Mask[pcSlot])
            this.Arena.ZoneCircleOutline(pc.Position, this.Distance, Colors.Vulnerable);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (!createForbiddenZones || !this.Mask[slot])
            return;
        // only the landing rings are safe: one slide, two slides, or not moving at all
        var pos = actor.Position;
        var ddistance = 2f * this.Distance;
        hints.AddForbiddenZone(new SDIntersection(
        [
            new SDInvertedDonut(pos, this.Distance, this.Distance + 1.2f),
            new SDInvertedDonut(pos, ddistance, ddistance + 1.2f),
            new SDInvertedRect(pos, offset, 0.5f, 0.5f, 0.5f),
        ]), DateTime.MaxValue);
    }
}
