namespace Minerva.Components;

/// <summary>
/// Look-away mechanic: while a cast of <c>WatchedAction</c> is active, the player must not face its
/// source. Warns when the source is in the player's front hemisphere and marks the source on the
/// radar. Not a positional dodge, so it contributes no forbidden zone.
/// </summary>
public class Gaze(ModuleBase module, uint aid) : ModuleComponent(module)
{
    public readonly uint WatchedAction = aid;
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

    /// <summary>True if <paramref name="actor"/> is facing toward the source (front hemisphere).</summary>
    public static bool FacingToward(Actor actor, WPos source)
    {
        var toSource = (source - actor.Position).Normalized();
        return WDir.Dot(actor.Rotation.ToDirection(), toSource) > 0f;
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var id in this.Sources)
        {
            var a = this.World.Actors.Find(id);
            if (a != null)
                this.Arena.AddCircle(a.Position, 1f, Colors.Danger, 2f);
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        foreach (var id in this.Sources)
        {
            var a = this.World.Actors.Find(id);
            if (a != null && FacingToward(actor, a.Position))
            {
                hints.Add("Look away!");
                return;
            }
        }
    }
}
