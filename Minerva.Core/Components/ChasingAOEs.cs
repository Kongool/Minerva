using System.Runtime.InteropServices;

namespace Minerva.Components;

/// <summary>
/// A "chasing AOE": an AOE that follows its target for a set number of casts, stepping at most
/// <paramref name="moveDistance"/> toward them each time. The drawn instance sits at the predicted next
/// position, so running keeps you ahead of it. Ported from BossmodReborn (BSD-3; see
/// THIRD-PARTY-NOTICES.txt).
/// </summary>
public class GenericChasingAOEs(ModuleBase module, float moveDistance, uint aid = default, string warningText = "GTFO from chasing aoe!") : GenericAOEs(module, aid, warningText)
{
    private readonly float moveDistance = moveDistance;

    /// <summary>One chaser: where it last landed, who it follows, and how many steps remain.</summary>
    public sealed class Chaser(AOEShape shape, Actor target, WPos prevPos, float moveDist, int numRemaining, DateTime nextActivation, double secondsBetweenActivations)
    {
        public AOEShape Shape = shape;
        public Actor Target = target;
        public WPos PrevPos = prevPos;
        public float MoveDist = moveDist;
        public int NumRemaining = numRemaining;
        public DateTime NextActivation = nextActivation;
        public double SecondsBetweenActivations = secondsBetweenActivations;

        /// <summary>Where the next explosion lands: a capped step from the last position toward the target.</summary>
        public WPos PredictedPosition()
        {
            var loc = this.Target.Position;
            var offset = loc - this.PrevPos;
            var distance = offset.Length();
            return distance > this.MoveDist ? this.PrevPos + this.MoveDist * offset / distance : loc;
        }
    }

    public readonly List<Chaser> Chasers = [];
    private readonly List<AOEInstance> active = [];

    public bool IsChaserTarget(Actor? actor) => this.Chasers.Exists(c => c.Target == actor);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        this.active.Clear();
        foreach (var c in this.Chasers)
        {
            var pos = c.PredictedPosition();
            var off = pos - c.PrevPos;
            this.active.Add(new AOEInstance(c.Shape, pos, off.LengthSq() > 0f ? Angle.FromDirection(off) : default, c.NextActivation));
        }
        return CollectionsMarshal.AsSpan(this.active);
    }

    /// <summary>
    /// Step the chaser nearest <paramref name="pos"/> forward one cast, dropping it once its last step
    /// has resolved. Returns false when no chaser matched.
    /// </summary>
    public bool Advance(WPos pos, float moveDistance, DateTime currentTime, bool removeWhenFinished = true)
    {
        ++this.NumCasts;
        Chaser? c = null;
        var minDistSq = float.MaxValue;
        foreach (var chaser in this.Chasers)
        {
            var distSq = (chaser.PredictedPosition() - pos).LengthSq();
            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                c = chaser;
            }
        }

        if (c == null)
            return false;

        if (--c.NumRemaining <= 0 && removeWhenFinished)
        {
            this.Chasers.Remove(c);
        }
        else
        {
            c.PrevPos = pos;
            c.MoveDist = moveDistance;
            c.NextActivation = currentTime.AddSeconds(c.SecondsBetweenActivations);
        }
        return true;
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (this.Chasers.Count == 0)
            return;
        this.AddForbiddenZones(actor, hints, true);
        this.AddForbiddenZones(actor, hints, false);
    }

    /// <summary>
    /// The chased player's own zone is pushed back along their facing and widened by the step distance,
    /// so the pathfinder always has a strictly-better direction to run — otherwise every escape route
    /// scores the same and it stalls inside the AOE. (BMR's trick, kept verbatim.)
    /// </summary>
    private void AddForbiddenZones(Actor actor, AIHints hints, bool isTarget)
    {
        if (this.Chasers[0].Shape is not AOEShapeCircle circle)
            return; // the pushed-back-circle hack only makes sense for circular chasers
        var radius = circle.Radius;
        foreach (var c in this.Chasers)
        {
            if (c.Target == actor == isTarget)
            {
                var radiusAdj = isTarget ? this.moveDistance + radius : radius + 1f;
                var predicted = c.PredictedPosition();
                var position = isTarget ? predicted - radius * actor.Rotation.ToDirection() : predicted;
                hints.AddForbiddenZone(new SDCircle(position, radiusAdj), c.NextActivation);
            }
        }
    }
}

/// <summary>
/// The standard chasing-AOE pattern: an overhead icon marks the targets, a long first cast
/// (<paramref name="actionFirst"/>) plants the chaser on the nearest marked player, and instant
/// follow-up casts (<paramref name="actionRest"/>) step it along. Ported from BossmodReborn (BSD-3;
/// see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class StandardChasingAOEs(ModuleBase module, AOEShape shape, uint actionFirst, uint actionRest, float moveDistance, double secondsBetweenActivations, int maxCasts, bool resetTargets = false, uint icon = default, double activationDelay = 5.1d)
    : GenericChasingAOEs(module, moveDistance)
{
    public StandardChasingAOEs(ModuleBase module, float radius, uint actionFirst, uint actionRest, float moveDistance, double secondsBetweenActivations, int maxCasts, bool resetTargets = false, uint icon = default, double activationDelay = 5.1d)
        : this(module, new AOEShapeCircle(radius), actionFirst, actionRest, moveDistance, secondsBetweenActivations, maxCasts, resetTargets, icon, activationDelay) { }

    public readonly AOEShape Shape = shape;
    public readonly uint ActionFirst = actionFirst;
    public readonly uint ActionRest = actionRest;
    public readonly float MoveDistance = moveDistance;
    public readonly double SecondsBetweenActivations = secondsBetweenActivations;
    public int MaxCasts = maxCasts;
    public readonly uint Icon = icon;
    public readonly double ActivationDelay = activationDelay;
    public readonly bool ResetTargets = resetTargets;
    public readonly List<Actor> Targets = [];
    public BitMask TargetsMask; // marked players, tracked from the icon so modules can add their own zones
    public DateTime Activation;

    public override void Update()
    {
        for (var i = this.Chasers.Count - 1; i >= 0; --i)
        {
            var c = this.Chasers[i];
            if ((c.Target.IsDestroyed || c.Target.IsDead) && c.NumRemaining < this.MaxCasts)
                this.Chasers.RemoveAt(i);
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var c in this.Chasers)
            this.Arena.AddLine(c.PrevPos, c.Target.Position, Colors.Danger);
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID != this.ActionFirst)
            return;

        // the long first cast is baited on whichever marked player is nearest to where it lands
        var pos = cast.LocXZ;
        Actor? target = null;
        var minDistance = float.MaxValue;
        foreach (var t in this.Targets)
        {
            var distanceSq = (t.Position - pos).LengthSq();
            if (distanceSq < minDistance)
            {
                minDistance = distanceSq;
                target = t;
            }
        }
        if (target != null)
        {
            this.Targets.Remove(target);
            this.TargetsMask.Clear(this.World.Party.FindSlot(target.InstanceID));
            this.Chasers.Add(new Chaser(this.Shape, target, pos, 0, this.MaxCasts, this.Module.CastFinishAt(cast), this.SecondsBetweenActivations)); // the initial cast does not move
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent cast)
    {
        var id = cast.Action.ID;
        if (id != this.ActionFirst && id != this.ActionRest)
            return;

        var pos = cast.MainTargetID == caster.InstanceID ? caster.Position : this.World.Actors.Find(cast.MainTargetID)?.Position ?? cast.TargetXZ;
        this.Advance(pos, this.MoveDistance, this.World.CurrentTime);
        if (this.Chasers.Count == 0 && this.ResetTargets)
        {
            this.Targets.Clear();
            this.NumCasts = 0;
        }
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == this.Icon)
        {
            this.Activation = this.World.FutureTime(this.ActivationDelay);
            this.Targets.Add(actor);
            this.TargetsMask.Set(this.World.Party.FindSlot(targetID));
        }
    }
}
