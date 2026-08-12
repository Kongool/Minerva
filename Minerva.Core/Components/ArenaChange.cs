namespace Minerva.Components;

/// <summary>
/// Swaps the arena bounds (and optionally the centre) partway through a fight — the common "floor
/// breaks / walls close in / boss teleports the arena" pattern. Fires once, when either an actor of
/// <c>TriggerOID</c> appears or a cast of <c>TriggerAID</c> begins. Because the radar and the auto-dodge
/// pathfinder both read <see cref="ModuleBase.Bounds"/>/<see cref="ModuleBase.Center"/> live, nothing
/// else needs to change — the new shape just takes effect. Author a one-liner subclass per fight, e.g.
/// <c>sealed class Shrink(ModuleBase m) : Components.ArenaChange(m, new ArenaBoundsCircle(25f), triggerOID: (uint)OID.Deathwall);</c>
/// </summary>
public class ArenaChange(ModuleBase module, ArenaBounds newBounds, uint triggerOID = 0, uint triggerAID = 0, WPos? newCenter = null) : ModuleComponent(module)
{
    public readonly ArenaBounds NewBounds = newBounds;
    public readonly uint TriggerOID = triggerOID;
    public readonly uint TriggerAID = triggerAID;
    public readonly WPos? NewCenter = newCenter;

    public bool Applied { get; private set; }

    private void Apply()
    {
        if (this.Applied)
            return;
        this.Applied = true;
        this.Module.Bounds = this.NewBounds;
        if (this.NewCenter is { } c)
            this.Module.Center = c;
    }

    public override void OnActorCreated(Actor actor)
    {
        if (this.TriggerOID != 0 && actor.OID == this.TriggerOID && !actor.IsDeadOrDestroyed)
            this.Apply();
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (this.TriggerAID != 0 && cast.Action.ID == this.TriggerAID)
            this.Apply();
    }
}
