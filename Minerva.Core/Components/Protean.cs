namespace Minerva.Components;

/// <summary>
/// Protean: a fan of AOEs, one aimed at each player, where every player must take their own and no one
/// else's. Subclasses say which source/target pairs are live. Ported from BossmodReborn (BSD-3; see
/// THIRD-PARTY-NOTICES.txt).
/// </summary>
public abstract class GenericProtean(ModuleBase module, uint aid, AOEShape shape) : CastCounter(module, aid)
{
    public readonly AOEShape Shape = shape;

    public abstract IEnumerable<(Actor source, Actor target)> ActiveAOEs();

    public bool IsPlayerClipped(Actor source, Actor target, Actor player)
        => this.Shape.Check(player.Position, source.Position, Angle.FromDirection(target.Position - source.Position));

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        foreach (var (source, target) in this.ActiveAOEs())
        {
            if (target != actor && this.IsPlayerClipped(source, target, actor))
            {
                hints.Add("GTFO from protean!");
                return;
            }
        }
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        // draw the viewer's own protean, plus any that would clip them
        foreach (var (source, target) in this.ActiveAOEs())
            if (target == pc || this.IsPlayerClipped(source, target, pc))
                this.Arena.ZoneShape(this.Shape, source.Position, Angle.FromDirection(target.Position - source.Position), Colors.AOE);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        // everyone else's protean is a danger zone; your own is yours to take
        foreach (var (source, target) in this.ActiveAOEs())
            if (target != actor)
                hints.AddForbiddenZone(this.Shape, source.Position, Angle.FromDirection(target.Position - source.Position));
    }
}

/// <summary>The usual form: proteans originate from the primary actor and hit every living player.</summary>
public class SimpleProtean(ModuleBase module, uint aid, AOEShape shape) : GenericProtean(module, aid, shape)
{
    public override IEnumerable<(Actor source, Actor target)> ActiveAOEs()
    {
        foreach (var p in this.World.Party.WithoutSlot())
            yield return (this.Module.PrimaryActor, p);
    }
}
