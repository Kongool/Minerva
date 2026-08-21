namespace Minerva.Components;

/// <summary>
/// A buff on an enemy that the party must dispel. Highlights the holder and flags it for the AI's target
/// list. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class Dispel(ModuleBase module, uint statusID, uint action = default) : CastHint(module, action, "Prepare to dispel!")
{
    private readonly List<Actor> targets = [];

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        foreach (var t in this.targets)
            if (hints.FindEnemy(t) is { } enemy)
                enemy.ShouldBeDispelled = true;
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (this.targets.Count != 0)
            hints.Add($"Dispel {this.targets[0].Name}!");
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == statusID)
            this.targets.Add(actor);
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == statusID)
            this.targets.Remove(actor);
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc) => this.Arena.Actors(this.targets, Colors.Other1);
}
