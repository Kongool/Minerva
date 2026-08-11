namespace Minerva.Components;

/// <summary>
/// Radial knockback from the caster: while a cast of <c>WatchedAction</c> is active, players are
/// pushed <c>Distance</c> away from the source. Warns that a knockback is coming and, more usefully,
/// flags when the player's predicted landing spot would be off the arena.
/// </summary>
public class SimpleKnockbacks(ModuleBase module, uint aid, float distance) : ModuleComponent(module)
{
    public readonly uint WatchedAction = aid;
    public readonly float Distance = distance;
    protected readonly List<ulong> Sources = [];

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction)
            this.Sources.Add(caster.InstanceID);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction)
            this.Sources.Remove(caster.InstanceID);
    }

    public bool Active => this.Sources.Count > 0;

    /// <summary>Where <paramref name="actor"/> would end up, pushed radially from <paramref name="source"/>.</summary>
    public WPos PredictLanding(Actor actor, WPos source)
    {
        var dir = (actor.Position - source).Normalized();
        return dir == default ? actor.Position : actor.Position + dir * this.Distance;
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var id in this.Sources)
        {
            var a = this.World.Actors.Find(id);
            if (a == null)
                continue;
            var landing = this.PredictLanding(pc, a.Position);
            this.Arena.AddLine(pc.Position, landing, Colors.Danger, 2f);
            this.Arena.AddCircle(landing, 0.6f, Colors.Danger, 2f);
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        foreach (var id in this.Sources)
        {
            var a = this.World.Actors.Find(id);
            if (a == null)
                continue;
            var landing = this.PredictLanding(actor, a.Position);
            if (!this.Module.Bounds.Contains(this.Module.Center, landing))
            {
                hints.Add("Knockback off the arena!");
                return;
            }
            hints.Add("Knockback", false);
            return;
        }
    }
}
