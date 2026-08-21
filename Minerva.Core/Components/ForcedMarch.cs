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
