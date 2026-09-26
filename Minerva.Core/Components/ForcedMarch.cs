namespace Minerva.Components;

/// <summary>
/// Forced march: "march forward/left/back/right" debuffs rotate the player, then a forced-march status
/// walks them in that direction for its duration. Several pending marches chain together, so this
/// simulates the whole sequence from the player's current position and draws where they will end up.
/// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class GenericForcedMarch(ModuleBase module, float activationLimit = float.MaxValue, bool stopAfterWall = false, bool stopAtWall = false) : ModuleComponent(module)
{
    public sealed class PlayerState
    {
        public readonly List<(Angle dir, float duration, DateTime activation)> PendingMoves = [];
        public DateTime ForcedEnd; // default if a forced march is not currently running

        public bool Active(DateTime now) => this.ForcedEnd > now || this.PendingMoves.Count > 0;
    }

    public readonly bool StopAfterWall = stopAfterWall;
    public readonly bool StopAtWall = stopAtWall;
    public bool OverrideDirection;
    public int NumActiveForcedMarches;
    public readonly Dictionary<ulong, PlayerState> State = []; // key = instance id
    public float MovementSpeed = 6f;
    public readonly float ActivationLimit = activationLimit; // ignore moves activating later than this

    private const float ApproxHitboxRadius = 0.499f;
    private const float MaxIntersectionError = 0.5f - ApproxHitboxRadius;

    /// <summary>Override to treat more than "outside the arena" as a bad place to be marched into.</summary>
    public virtual bool DestinationUnsafe(int slot, Actor actor, WPos pos) => !this.Module.Bounds.Contains(this.Module.Center, pos);

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var movements = this.ForcedMovements(actor);
        if (movements.Count == 0)
            return;
        var last = movements[^1];
        if (last.from != last.to && this.DestinationUnsafe(slot, actor, last.to))
            hints.Add("Aim for safe spot!");
    }

    /// <summary>
    /// Aim the march. The walk goes the way the character faces when it starts, turned by the march's own
    /// direction, so choosing the facing chooses where the walk ends -- which is how a player solves it. This
    /// tries every facing, simulates the walk from each, keeps the ones that stay on the floor, do not end where
    /// the module calls unsafe and do not end in anything still drawn when the walk begins, picks one of them
    /// (<see cref="PickFacing"/>) and forbids every other facing: the channel an inverted gaze uses, so the code
    /// that turns the character out of a gaze turns it into this. It needs Face on.
    ///
    /// <para>Nothing aimed marches at all before this, and the cost showed on 2026-09-25: of eight walks across
    /// two Iambe pulls, all eight went outward and two ended outside the FATE -- the only rule there was
    /// BossmodReborn's "walk directly away from the boss", right about the circle that lands on the boss after
    /// the walk and blind to the edge behind the character.</para>
    ///
    /// <para>Not while walking: the game owns the feet and the facing for that. Not for a march whose
    /// directions are absolute (<see cref="OverrideDirection"/>), where facing changes nothing. And nothing is
    /// published when no facing is safe, rather than a least-bad one that reads as a solution.</para>
    /// </summary>
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (this.OverrideDirection || !this.State.TryGetValue(actor.InstanceID, out var state)
            || state.PendingMoves.Count == 0 || state.ForcedEnd > this.World.CurrentTime)
            return;

        var starts = state.PendingMoves[0].activation;
        if (this.AimFacing(slot, actor, state, starts) is { } facing)
            hints.ForbiddenDirections.Add(AimArc(facing, starts));
    }

    /// <summary>The facing that sends the walk somewhere survivable, or null if none does.</summary>
    public Angle? AimFacing(int slot, Actor actor, PlayerState state, DateTime starts)
        => PickFacing(actor.Rotation, facing => this.WalkSurvivable(slot, actor, state, facing, starts));

    /// <summary>How many ten-degree steps either side of an aimed facing should also work.</summary>
    private const int SpareSteps = 2;

    /// <summary>
    /// Of the facings ten degrees apart that <paramref name="survivable"/> accepts, the one with the most room
    /// either side -- up to twenty degrees -- and among those the smallest turn from <paramref name="current"/>.
    ///
    /// <para>The smallest turn alone is the wrong rule, because it always lands on the edge of what works: the
    /// character faces somewhere that fails, so the nearest facing that does not is the one beside the first
    /// that fails. A walk aimed there is one twitch from failing, and the twitch comes -- the turn interpolates,
    /// the dodge keeps the character moving until the walk starts, and a rotation turns it toward its target.
    /// The same lesson the gaze turn learnt with its margin.</para>
    /// </summary>
    public static Angle? PickFacing(Angle current, Func<Angle, bool> survivable)
    {
        var works = new bool[36];
        for (var i = 0; i < 36; ++i)
            works[i] = survivable((i * 10f).Degrees());

        Angle? best = null;
        var bestRoom = -1;
        var bestSwing = float.MaxValue;
        for (var i = 0; i < 36; ++i)
        {
            if (!works[i])
                continue;
            var room = 0;
            while (room < SpareSteps && works[(i + room + 1) % 36] && works[(i + 36 - room - 1) % 36])
                ++room;
            var facing = (i * 10f).Degrees();
            var swing = MathF.Abs((facing - current).Normalized().Rad);
            if (room > bestRoom || (room == bestRoom && swing < bestSwing))
            {
                best = facing;
                bestRoom = room;
                bestSwing = swing;
            }
        }

        return best;
    }

    /// <summary>
    /// The forbidden arc that turns the character onto <paramref name="facing"/> itself.
    ///
    /// <para>Not "everything but a quarter around it". The gaze turn stops a margin
    /// (<see cref="AIHints.GazeFacingMargin"/>) inside the edge of what is allowed, not in the middle, so with a
    /// quarter left open it stopped twenty degrees short of the aim -- far enough, from a character backed into
    /// a wall, to walk it off the floor. Leaving exactly the margin open either side puts that stopping point on
    /// the aim.</para>
    /// </summary>
    public static (Angle center, Angle halfWidth, DateTime activation) AimArc(Angle facing, DateTime starts)
        => (facing + 180f.Degrees(), 180f.Degrees() - AIHints.GazeFacingMargin, starts);

    /// <summary>Does the walk from this facing stay on the floor and end somewhere survivable? A character already
    /// off the floor when it starts -- Iambe, 2026-09-25, one began 31 yalms out in a 30-yalm FATE -- is judged
    /// by whether the walk brings it back and keeps it there, or every facing fails and none is aimed.</summary>
    private bool WalkSurvivable(int slot, Actor actor, PlayerState state, Angle facing, DateTime starts)
    {
        var from = actor.Position;
        var dir = facing;
        var onFloor = this.Module.Bounds.Contains(this.Module.Center, from);
        var limit = this.ActivationLimit < float.MaxValue ? this.World.FutureTime(this.ActivationLimit) : DateTime.MaxValue;
        foreach (var move in state.PendingMoves)
        {
            if (move.activation > limit)
                break;
            dir += move.dir;
            var to = this.Travel(from, dir, this.MovementSpeed * move.duration);
            var leg = to - from;
            var length = leg.Length();
            for (var d = 1f; d < length; d += 1f)
            {
                if (this.Module.Bounds.Contains(this.Module.Center, from + (leg * (d / length))))
                    onFloor = true;
                else if (onFloor)
                    return false;
            }
            from = to;
        }

        return !this.DestinationUnsafe(slot, actor, from) && !this.EndsInSomethingDrawn(slot, actor, from, starts);
    }

    /// <summary>Would the walk end inside an AOE the module is drawing that is still to come when the walk begins?
    /// One resolving before then is gone by the time anyone arrives. With no start time known, all of them count.</summary>
    private bool EndsInSomethingDrawn(int slot, Actor actor, WPos end, DateTime starts)
    {
        var gone = starts == default ? default : starts.AddSeconds(-0.5d);
        foreach (var component in this.Module.Components)
        {
            if (component is not GenericAOEs aoes)
                continue;
            foreach (ref readonly var aoe in aoes.ActiveAOEs(slot, actor))
                if ((aoe.Activation == default || aoe.Activation >= gone) && aoe.Check(end))
                    return true;
        }

        return false;
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var m in this.ForcedMovements(pc))
        {
            this.Arena.ActorProjected(m.from, m.to, m.dir, Colors.Danger);
            this.Arena.AddLine(m.from, m.to, Colors.Danger);
        }
    }

    private PlayerState StateOf(ulong instanceID)
    {
        if (!this.State.TryGetValue(instanceID, out var s))
            this.State[instanceID] = s = new PlayerState();
        return s;
    }

    public void AddForcedMovement(Actor player, Angle direction, float duration, DateTime activation)
    {
        var moves = this.StateOf(player.InstanceID).PendingMoves;
        moves.Add((direction, duration, activation));
        moves.Sort(static (a, b) => a.activation.CompareTo(b.activation));
    }

    public bool HasForcedMovements(Actor player)
        => this.State.TryGetValue(player.InstanceID, out var s) && s.Active(this.World.CurrentTime);

    public void ActivateForcedMovement(Actor player, DateTime expiration)
    {
        this.StateOf(player.InstanceID).ForcedEnd = expiration;
        ++this.NumActiveForcedMarches;
    }

    public void DeactivateForcedMovement(Actor player)
    {
        this.StateOf(player.InstanceID).ForcedEnd = default;
        --this.NumActiveForcedMarches;
    }

    /// <summary>
    /// Walk the pending sequence forward from the player's position: each leg turns by its own relative
    /// direction, then travels speed x duration, optionally clipped at the arena wall.
    /// </summary>
    public List<(WPos from, WPos to, Angle dir)> ForcedMovements(Actor player)
    {
        if (!this.State.TryGetValue(player.InstanceID, out var state))
            return [];

        var from = player.Position;
        var dir = this.OverrideDirection ? default : player.Rotation;
        var movements = new List<(WPos, WPos, Angle)>();
        var now = this.World.CurrentTime;

        if (state.ForcedEnd > now)
        {
            // once marching, the player is already turned to the marched direction
            dir = player.Rotation;
            var to = this.Travel(from, dir, this.MovementSpeed * (float)(state.ForcedEnd - now).TotalSeconds);
            movements.Add((from, to, dir));
            from = to;
        }

        var limit = this.ActivationLimit < float.MaxValue ? this.World.FutureTime(this.ActivationLimit) : DateTime.MaxValue;
        foreach (var move in state.PendingMoves)
        {
            if (move.activation > limit)
                break;
            dir += move.dir;
            var to = this.Travel(from, dir, this.MovementSpeed * move.duration);
            movements.Add((from, to, dir));
            from = to;
        }
        return movements;
    }

    private WPos Travel(WPos from, Angle dir, float distance)
    {
        var wdir = dir.ToDirection();
        if (this.StopAfterWall)
            distance = Math.Min(distance, this.Module.Bounds.IntersectRay(this.Module.Center, from, wdir) + MaxIntersectionError);
        else if (this.StopAtWall)
            distance = Math.Min(distance, this.Module.Bounds.IntersectRay(this.Module.Center, from, wdir) - MaxIntersectionError);
        return from + distance * wdir;
    }
}

/// <summary>
/// The usual status-driven forced march: four directional statuses queue moves, and a fifth (plus its
/// NPC variant) starts the walk. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class StatusDrivenForcedMarch(ModuleBase module, float duration, uint statusForward, uint statusBackward, uint statusLeft, uint statusRight, uint statusForced = 1257u, uint statusForcedNPCs = 3629u, float activationLimit = float.MaxValue, bool stopAfterWall = false, bool stopAtWall = false)
    : GenericForcedMarch(module, activationLimit, stopAfterWall, stopAtWall)
{
    public float Duration = duration;

    // order matters: index 0..3 are the quarter-turns, 4+ mean "start marching"
    public readonly uint[] Statuses = [statusForward, statusLeft, statusBackward, statusRight, statusForced, statusForcedNPCs];

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        var kind = Array.IndexOf(this.Statuses, status.ID);
        if (kind >= 4)
            this.ActivateForcedMovement(actor, status.ExpireAt);
        else if (kind >= 0)
            this.AddForcedMovement(actor, kind * 90f.Degrees(), this.Duration, status.ExpireAt);
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        var kind = Array.IndexOf(this.Statuses, status.ID);
        if (kind >= 4)
        {
            this.DeactivateForcedMovement(actor);
        }
        else if (kind >= 0)
        {
            var dir = kind * 90f.Degrees();
            if (this.State.TryGetValue(actor.InstanceID, out var s))
                s.PendingMoves.RemoveAll(m => m.dir == dir);
        }
    }
}

/// <summary>
/// Forced march started by a cast rather than by directional statuses: the cast queues the same move on
/// everyone. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class ActionDrivenForcedMarch(ModuleBase module, uint aid, float duration, Angle rotation, float actionEffectDelay, uint statusForced = 5174u, uint statusForcedNPCs = 3629u, float activationLimit = float.MaxValue)
    : GenericForcedMarch(module, activationLimit)
{
    public readonly float Duration = duration;
    public readonly float ActionEffectDelay = actionEffectDelay;
    public readonly Angle Rotation = rotation;
    public readonly uint StatusForced = statusForced;
    public readonly uint StatusForcedNPCs = statusForcedNPCs;
    public readonly uint Aid = aid;

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID != this.StatusForced && status.ID != this.StatusForcedNPCs)
            return;
        // the queued move is now in force — drop the prediction and run the real thing
        if (this.State.TryGetValue(actor.InstanceID, out var s))
        {
            var i = s.PendingMoves.FindIndex(m => m.dir == this.Rotation);
            if (i >= 0)
                s.PendingMoves.RemoveAt(i);
        }
        this.ActivateForcedMovement(actor, status.ExpireAt);
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == this.StatusForced || status.ID == this.StatusForcedNPCs)
            this.DeactivateForcedMovement(actor);
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID != this.Aid)
            return;
        foreach (var p in this.World.Party.WithoutSlot())
            this.AddForcedMovement(p, this.Rotation, this.Duration, this.Module.CastFinishAt(cast, this.ActionEffectDelay));
    }
}
