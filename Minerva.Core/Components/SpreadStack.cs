namespace Minerva.Components;

/// <summary>
/// Spread markers: a cast of <c>WatchedAction</c> targets a player who must keep others out of a
/// radius. Draws a circle that follows each target actor and warns when players are too close.
/// Circles follow live positions, so this reads correctly as people move.
/// </summary>
public class SpreadFromCastTargets(ModuleBase module, uint aid, float radius) : ModuleComponent(module)
{
    public readonly uint WatchedAction = aid;
    public readonly float Radius = radius;
    private readonly AOEShapeCircle shape = new(radius);
    protected readonly List<ulong> Targets = [];

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction && cast.TargetID != 0)
            this.Targets.Add(cast.TargetID);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction)
            this.Targets.Remove(cast.TargetID);
    }

    public bool IsTarget(Actor actor) => this.Targets.Contains(actor.InstanceID);

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var id in this.Targets)
        {
            var a = this.World.Actors.Find(id);
            if (a != null)
                this.Arena.ZoneShape(this.shape, a.Position, default, Colors.AOE);
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (this.Targets.Count == 0)
            return;
        // dangerous if you're a target with someone in range, or you're standing in someone else's spread
        foreach (var id in this.Targets)
        {
            var a = this.World.Actors.Find(id);
            if (a == null || a.InstanceID == actor.InstanceID)
                continue;
            if (actor.Position.InCircle(a.Position, this.Radius))
            {
                hints.Add("Spread!");
                return;
            }
        }
        if (this.IsTarget(actor))
            hints.Add("Spread!", false);
    }
}

/// <summary>
/// Stack marker: a cast targets one player everyone else should pile onto. Draws the stack circle at
/// the target and reminds non-targets to stack.
/// </summary>
public class StackWithCastTargets(ModuleBase module, uint aid, float radius) : ModuleComponent(module)
{
    public readonly uint WatchedAction = aid;
    public readonly float Radius = radius;
    private readonly AOEShapeCircle shape = new(radius);
    protected readonly List<ulong> Targets = [];

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction && cast.TargetID != 0)
            this.Targets.Add(cast.TargetID);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction)
            this.Targets.Remove(cast.TargetID);
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var id in this.Targets)
        {
            var a = this.World.Actors.Find(id);
            if (a != null)
                this.Arena.ZoneShape(this.shape, a.Position, default, Colors.Safe);
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        foreach (var id in this.Targets)
        {
            var a = this.World.Actors.Find(id);
            if (a == null)
                continue;
            if (a.InstanceID != actor.InstanceID && !actor.Position.InCircle(a.Position, this.Radius))
            {
                hints.Add("Stack!");
                return;
            }
        }
    }
}
