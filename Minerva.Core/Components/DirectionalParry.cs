namespace Minerva.Components;

/// <summary>
/// Directional parry: an enemy blocks (and counters) attacks from specific sides, so melee must move to
/// an unshielded side. The active sides come from the common parry status; modules can also predict the
/// next set with <see cref="PredictParrySide"/>. Ported from BossmodReborn (BSD-3; see
/// THIRD-PARTY-NOTICES.txt); the shielded arcs are drawn as line segments rather than BMR's arc path.
/// </summary>
public class DirectionalParry(ModuleBase module, uint[] actorOID, int forbiddenPriority = AIHints.Enemy.PriorityForbidden) : AddsMulti(module, actorOID)
{
    [Flags]
    public enum Side
    {
        None = 0x0,
        Front = 0x1,
        Back = 0x2,
        Left = 0x4,
        Right = 0x8,
        All = 0xF,
    }

    public const uint ParrySID = 680u; // the game's shared "directional parry" status
    public readonly int ForbiddenPriority = forbiddenPriority;

    /// <summary>Per-actor state: active sides in the low nibble, predicted ("imminent") sides in the next.</summary>
    public readonly Dictionary<ulong, int> ActorStates = [];

    public bool Active
    {
        get
        {
            foreach (var state in this.ActorStates.Values)
                if (ActiveSides(state) != Side.None)
                    return true;
            return false;
        }
    }

    /// <summary>Which side of <paramref name="target"/> an attack from <paramref name="from"/> lands on.</summary>
    private static Side AttackSide(Actor target, WPos from)
    {
        var attackDir = (from - target.Position).Normalized();
        var facing = target.Rotation.ToDirection();
        var forwardDot = attackDir.Dot(facing);
        if (forwardDot > 0.7071068f)
            return Side.Front;
        if (forwardDot < -0.7071068f)
            return Side.Back;
        return attackDir.Dot(facing.OrthoL()) > 0f ? Side.Left : Side.Right;
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (this.ActorStates.Count == 0)
            return;
        var target = this.ActiveActors.Find(a => a.InstanceID == actor.TargetID);
        if (target == null || !this.ActorStates.TryGetValue(actor.TargetID, out var state))
            return;
        if ((ForbiddenSides(state) & AttackSide(target, actor.Position)) != default)
            hints.Add("Attack target from unshielded side!");
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        foreach (var (id, state) in this.ActorStates)
        {
            if (this.World.Actors.Find(id) is not { } target)
                continue;

            var forbidden = ForbiddenSides(state);
            if ((forbidden & AttackSide(target, actor.Position)) == default)
                continue;

            if (ActiveSides(state) != default)
                hints.SetPriority(target, this.ForbiddenPriority);

            // only steer around the enemy we're actually attacking
            if (actor.TargetID != id)
                continue;
            if ((forbidden & Side.Front) != default)
                ForbidDirection(default);
            if ((forbidden & Side.Left) != default)
                ForbidDirection(90f.Degrees());
            if ((forbidden & Side.Back) != default)
                ForbidDirection(180f.Degrees());
            if ((forbidden & Side.Right) != default)
                ForbidDirection(270f.Degrees());

            void ForbidDirection(Angle offset)
                => hints.AddForbiddenZone(new SDCone(target.Position, 100f, target.Rotation + offset, 45f.Degrees()), DateTime.MaxValue);
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        base.DrawArenaForeground(pcSlot, pc);
        foreach (var a in this.ActiveActors)
        {
            if (!this.ActorStates.TryGetValue(a.InstanceID, out var state))
                continue;
            var active = ActiveSides(state);
            var imminent = ImminentSides(state);
            this.DrawParry(a, default, active, imminent, Side.Front);
            this.DrawParry(a, 180f.Degrees(), active, imminent, Side.Back);
            this.DrawParry(a, 90f.Degrees(), active, imminent, Side.Left);
            this.DrawParry(a, 270f.Degrees(), active, imminent, Side.Right);
        }
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        // the status' Extra carries the shielded-side bitfield, and supersedes any prediction
        if (status.ID == ParrySID)
            this.ActorStates[actor.InstanceID] = status.Extra & 0xF;
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == ParrySID)
            this.UpdateState(actor.InstanceID, this.ActorState(actor.InstanceID) & ~0xF);
    }

    /// <summary>Tell the component which sides are about to become shielded (drawn dimmer than active).</summary>
    public void PredictParrySide(ulong instanceID, Side sides)
        => this.UpdateState(instanceID, ((int)sides << 4) | (this.ActorState(instanceID) & 0xF));

    public int ActorState(ulong instanceID) => this.ActorStates.GetValueOrDefault(instanceID, 0);

    public void UpdateState(ulong instanceID, int state)
    {
        if (state == 0)
            this.ActorStates.Remove(instanceID);
        else
            this.ActorStates[instanceID] = state;
    }

    private void DrawParry(Actor actor, Angle offset, Side active, Side imminent, Side check)
    {
        if ((active & check) != default)
            this.DrawParryArc(actor, offset, Colors.Enemy);
        else if ((imminent & check) != default)
            this.DrawParryArc(actor, offset, Colors.Border);
    }

    /// <summary>A 90-degree shield arc at radius 1.5y, approximated with straight segments.</summary>
    private void DrawParryArc(Actor actor, Angle offset, uint color)
    {
        const int segments = 8;
        const float radius = 1.5f;
        var dir = actor.Rotation + offset;
        var from = (dir - 45f.Degrees()).Rad;
        var step = (90f.Degrees()).Rad / segments;
        var prev = actor.Position + new Angle(from).ToDirection() * radius;
        for (var i = 1; i <= segments; ++i)
        {
            var next = actor.Position + new Angle(from + step * i).ToDirection() * radius;
            this.Arena.AddLine(prev, next, color);
            prev = next;
        }
    }

    private static Side ActiveSides(int state) => (Side)(state & (int)Side.All);
    private static Side ImminentSides(int state) => ActiveSides(state >> 4);
    private static Side ForbiddenSides(int state) => ActiveSides(state) | ImminentSides(state);
}
