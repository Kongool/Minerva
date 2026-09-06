namespace Minerva.Components;

/// <summary>
/// Icon-marked players who must huddle together. The game only sends a cast event when someone *fails*,
/// so resolution is guessed from a fixed activation delay. Ported from BossmodReborn (BSD-3; see
/// THIRD-PARTY-NOTICES.txt).
/// </summary>
public class StackTogether(ModuleBase module, uint iconId, float activationDelay, float radius = 3f) : ModuleComponent(module)
{
    public readonly List<Actor> Targets = [];
    public DateTime Activation;
    public readonly uint Icon = iconId;
    public readonly float Radius = radius;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID != this.Icon || this.World.Party.FindSlot(actor.InstanceID) < 0)
            return;
        this.Targets.Add(actor);
        if (this.Activation == default)
            this.Activation = this.World.FutureTime(activationDelay);
    }

    public override void Update()
    {
        if (this.Activation != default && this.Activation < this.World.CurrentTime)
        {
            this.Activation = default;
            this.Targets.Clear();
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var actorFound = false;
        var stacked = false;
        foreach (var target in this.Targets)
        {
            if (target == actor)
                actorFound = true;
            else if (target.Position.InCircle(actor.Position, this.Radius))
                stacked = true;
        }
        if (actorFound)
            hints.Add("Stack with other targets!", !stacked);
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (!this.Targets.Contains(pc))
            return;
        foreach (var target in this.Targets)
            if (target != pc)
                this.Arena.ZoneCircleOutline(target.Position, this.Radius, Colors.Safe);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (!this.Targets.Contains(actor))
            return;
        // be inside every other target's circle at once
        var forbidden = new List<ShapeDistance>(this.Targets.Count);
        foreach (var target in this.Targets)
            if (target != actor)
                forbidden.Add(new SDInvertedCircle(target.Position, this.Radius));
        if (forbidden.Count != 0)
            hints.AddForbiddenZone(new SDIntersection([.. forbidden]), this.Activation);
    }
}
