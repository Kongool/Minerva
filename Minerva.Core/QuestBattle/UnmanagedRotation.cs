using System.Numerics;

namespace Minerva.QuestBattle;

/// <summary>
/// The kit of a character a quest battle makes you play -- Y'shtola in The Will of the Moon, Alphinaud, Hien, Nyelbert
/// -- as a small hand-written rotation. Ported from BossmodReborn's <c>QuestBattle.UnmanagedRotation</c> (BSD-3; see
/// THIRD-PARTY-NOTICES.txt).
///
/// <para>Minerva presses no buttons. The rotation decides, as it does in BossmodReborn, and its choices go on
/// <see cref="AIHints.ActionsToExecute"/>; Minerva publishes the role-play ones (<see cref="RoleplayRequests"/>,
/// <c>Minerva.Hints.RoleplayActions</c>) and Daedalus presses them, its own job rotation standing down meanwhile.
/// The fight knowledge a kit needs -- heal Hien harder while Daidukul casts Tranquil Annihilation -- is in the module,
/// which is why the decision lives here. Until 2026-09-29 these were stripped (tools/strip_questbattle.py); the user:
/// "we will need full kits for all the solo fights not as the warrior of light".</para>
/// </summary>
public abstract class UnmanagedRotation(WorldState ws, float effectiveRange)
{
    protected AIHints Hints = null!;
    protected Actor Player = null!;
    protected WorldState World => ws;
    protected uint MP;

    /// <summary>The step the game's combo continues from (BossmodReborn's <c>ComboAction</c>); 0 when none.</summary>
    protected Roleplay.AID ComboAction => (Roleplay.AID)this.World.Client.ComboAction;

    protected abstract void Exec(Actor? primaryTarget);

    public void Execute(Actor player, AIHints hints)
    {
        this.Hints = hints;
        this.Player = player;
        this.MP = player.HPMP.CurMP;
        hints.RoleplayKitRange = effectiveRange;

        // the nearest of the highest-priority enemies the module left attackable
        Actor? primary = null;
        var minDistanceSq = float.MaxValue;
        var maxPriority = int.MinValue;
        foreach (var e in hints.PotentialTargets)
        {
            if (e.Priority < 0)
                continue;
            var distanceSq = (e.Actor.Position - player.Position).LengthSq();
            if (e.Priority > maxPriority || e.Priority == maxPriority && distanceSq < minDistanceSq)
            {
                maxPriority = e.Priority;
                minDistanceSq = distanceSq;
                primary = e.Actor;
            }
        }

        if (primary != null)
        {
            hints.ForcedTarget = primary;
            hints.GoalZones.Add(AIHints.GoalSingleTarget(primary, effectiveRange));
        }
        this.Exec(primary);
    }

    protected void UseAction(Roleplay.AID action, Actor? target, float additionalPriority = default, Vector3 targetPos = default, Angle? facingAngle = null)
        => this.UseAction(ActionID.MakeSpell(action), target, additionalPriority, targetPos, facingAngle);

    /// <summary>Ask for <paramref name="action"/>. The cast time is left to the presser, which reads it from the game's
    /// Action sheet; BossmodReborn took it from its own action database, which Minerva does not have.</summary>
    protected void UseAction(ActionID action, Actor? target, float additionalPriority = default, Vector3 targetPos = default, Angle? facingAngle = null)
        => this.Hints.ActionsToExecute.Push(action, target, ActionQueue.Priority.High + additionalPriority, targetPos: targetPos, facingAngle: facingAngle);

    protected float StatusDuration(DateTime expireAt) => Math.Max((float)(expireAt - this.World.CurrentTime).TotalSeconds, 0f);

    /// <summary>Seconds left and stacks of <paramref name="sid"/> applied to <paramref name="actor"/> by
    /// <paramref name="sourceID"/>; zeros when it is not there.</summary>
    protected (float Left, int Stacks) StatusDetails(Actor? actor, uint sid, ulong sourceID)
    {
        if (actor != null)
            foreach (var s in actor.Statuses)
                if (s.ID == sid && s.SourceID == sourceID)
                    return (this.StatusDuration(s.ExpireAt), s.Extra & 0xFF);
        return (0f, 0);
    }

    protected (float Left, int Stacks) StatusDetails<SID>(Actor? actor, SID sid, ulong sourceID) where SID : Enum
        => this.StatusDetails(actor, (uint)(object)sid, sourceID);
}

/// <summary>
/// Runs an <see cref="UnmanagedRotation"/> for the viewer each frame, as BossmodReborn's <c>RotationModule</c> does.
/// The rotation takes the module and the world, or the world alone.
/// </summary>
public abstract class RotationModule<R>(ModuleBase module) : ModuleComponent(module) where R : UnmanagedRotation
{
    private readonly R rotation = Create(module);

    private static R Create(ModuleBase module)
    {
        var withModule = typeof(R).GetConstructors().FirstOrDefault(c => c.GetParameters() is { Length: 2 } p
            && p[0].ParameterType.IsAssignableFrom(module.GetType()) && p[1].ParameterType == typeof(WorldState));
        return withModule != null
            ? (R)withModule.Invoke([module, module.World])
            : (R)Activator.CreateInstance(typeof(R), module.World)!;
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
        => this.rotation.Execute(actor, hints);
}
