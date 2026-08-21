using System.Runtime.InteropServices;

namespace Minerva.Components;

/// <summary>
/// Targets that must not be attacked — invincible or otherwise off-limits. Warns when the player is
/// targeting one and pushes it to the bottom of the AI's target priority. Ported from BossmodReborn
/// (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public abstract class GenericInvincible(ModuleBase module, string hint = "Attacking invincible target!", int priority = AIHints.Enemy.PriorityInvincible) : ModuleComponent(module)
{
    public bool EnableHints = true;

    protected abstract ReadOnlySpan<Actor> ForbiddenTargets(int slot, Actor actor);

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (!this.EnableHints)
            return;
        var targets = this.ForbiddenTargets(slot, actor);
        for (var i = 0; i < targets.Length; ++i)
        {
            if (targets[i].InstanceID == actor.TargetID)
            {
                hints.Add(hint);
                return;
            }
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var targets = this.ForbiddenTargets(slot, actor);
        for (var i = 0; i < targets.Length; ++i)
            hints.SetPriority(targets[i], priority);
    }
}

/// <summary>Invincibility conferred by a status (Hallowed Ground and friends).</summary>
public class InvincibleStatus(ModuleBase module, uint statusId, string hint = "Attacking invincible target!", int priority = AIHints.Enemy.PriorityInvincible)
    : GenericInvincible(module, hint, priority)
{
    protected readonly List<Actor> Actors = [];

    protected override ReadOnlySpan<Actor> ForbiddenTargets(int slot, Actor actor) => CollectionsMarshal.AsSpan(this.Actors);

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == statusId && !this.Actors.Contains(actor))
            this.Actors.Add(actor);
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == statusId)
            this.Actors.Remove(actor);
    }
}
