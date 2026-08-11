namespace Minerva.Components;

/// <summary>
/// Line stack: a cast of <c>WatchedAction</c> aims a long rectangle from its source through a marked
/// player, and the party must line up inside that rectangle to share it. Draws the line from the
/// source toward the target and warns anyone standing outside it to stack in. The rectangle follows
/// live positions, so it reads correctly as the marked player moves.
/// </summary>
public class LineStack(ModuleBase module, uint aid, float halfWidth, float length = 50f) : ModuleComponent(module)
{
    public readonly uint WatchedAction = aid;
    public readonly float HalfWidth = halfWidth;
    public readonly float Length = length;
    private readonly AOEShapeRect shape = new(length, halfWidth);
    protected readonly List<(ulong source, ulong target)> Stacks = [];

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction && cast.TargetID != 0)
            this.Stacks.Add((caster.InstanceID, cast.TargetID));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction)
            this.Stacks.RemoveAll(s => s.source == caster.InstanceID && s.target == cast.TargetID);
    }

    public bool Active => this.Stacks.Count > 0;

    // resolve a stack's live geometry: the rect starts at the source and points at the target
    private bool TryGeometry((ulong source, ulong target) s, out WPos origin, out Angle rotation)
    {
        origin = default;
        rotation = default;
        var src = this.World.Actors.Find(s.source);
        var tgt = this.World.Actors.Find(s.target);
        if (src == null || tgt == null)
            return false;
        var dir = (tgt.Position - src.Position).Normalized();
        origin = src.Position;
        rotation = dir == default ? src.Rotation : Angle.FromDirection(dir);
        return true;
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        foreach (var s in this.Stacks)
            if (this.TryGeometry(s, out var origin, out var rotation))
                this.Arena.ZoneShape(this.shape, origin, rotation, Colors.Safe);
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        foreach (var s in this.Stacks)
        {
            if (!this.TryGeometry(s, out var origin, out var rotation))
                continue;
            if (!this.shape.Check(actor.Position, origin, rotation))
            {
                hints.Add("Stack in line!");
                return;
            }
        }
    }
}
