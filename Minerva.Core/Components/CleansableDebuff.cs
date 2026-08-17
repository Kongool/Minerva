namespace Minerva.Components;

/// <summary>
/// Tracks a cleansable debuff (Doom, etc.) and reminds the affected player to get it removed. Ported from
/// BossmodReborn's CleansableDebuff (BSD-3; see THIRD-PARTY-NOTICES.txt); Minerva has no per-actor Role,
/// so it can't tell healers to cleanse a specific ally — it warns the afflicted player and lists who is hit.
/// </summary>
public abstract class CleansableDebuff(ModuleBase module, uint statusID, string noun = "Doom", string adjective = "doomed") : ModuleComponent(module)
{
    private readonly List<Actor> affected = [];
    private readonly List<Actor> pending = [];
    public readonly uint StatusID = statusID;
    public readonly string Noun = noun;
    public readonly string Adjective = adjective;

    public IReadOnlyList<Actor> Affected => this.affected;

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == this.StatusID && !this.affected.Contains(actor))
            this.affected.Add(actor);
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        // the status slot is cleared right after this fires, so verify in Update that every instance is gone
        if (status.ID == this.StatusID)
            this.pending.Add(actor);
    }

    public override void Update()
    {
        for (var i = this.pending.Count - 1; i >= 0; --i)
        {
            var a = this.pending[i];
            if (a.FindStatus(this.StatusID) == null)
                this.affected.Remove(a);
            this.pending.RemoveAt(i);
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (this.affected.Contains(actor))
            hints.Add($"You are {this.Adjective}! Get cleansed.");
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (this.affected.Count != 0)
            hints.Add($"Cleanse ({this.Noun}): " + string.Join(", ", this.affected.ConvertAll(a => a.Name)));
    }
}
