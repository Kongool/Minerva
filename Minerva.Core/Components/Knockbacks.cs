using System.Runtime.InteropServices;

namespace Minerva.Components;

/// <summary>
/// Generic knockback / attract component: subclasses expose a set of active <see cref="Knockback"/>
/// sources and this base predicts where the player is shoved, draws the arrow, and warns when the
/// landing spot leaves the arena. Ported from BossmodReborn's GenericKnockback (BSD-3; see
/// THIRD-PARTY-NOTICES.txt), simplified for Minerva's local-player focus — party-wide status-immunity
/// tracking and wall-segment ray tests are omitted (Minerva has no party state / ray-bounds helper).
/// </summary>
public abstract class GenericKnockback(ModuleBase module, uint aid = default, int maxCasts = int.MaxValue, bool stopAtWall = false) : CastCounter(module, aid)
{
    public enum Kind
    {
        None,
        AwayFromOrigin, // standard knockback along the ray from origin to target
        TowardsOrigin,  // standard pull toward the source
        DirBackward,    // pull backward along the source's facing
        DirForward,     // directional knockback along the source's facing
        DirLeft,        // directional knockback 90° CCW of the source's facing
        DirRight        // directional knockback 90° CW of the source's facing
    }

    public readonly struct SafeWall(WPos vertex1, WPos vertex2)
    {
        public readonly WPos Vertex1 = vertex1;
        public readonly WPos Vertex2 = vertex2;
    }

    public readonly struct Knockback(
        WPos origin,
        float distance,
        DateTime activation = default,
        AOEShape? shape = null,          // if null, an unavoidable raidwide knockback/attract
        Angle direction = default,       // for the directional kinds
        Kind kind = Kind.AwayFromOrigin,
        float minDistance = default,     // for attracts: don't pull closer than this
        IReadOnlyList<SafeWall>? safeWalls = null,
        ulong actorID = default,
        bool ignoreImmunes = false)
    {
        public readonly WPos Origin = origin;
        public readonly float Distance = distance;
        public readonly DateTime Activation = activation;
        public readonly AOEShape? Shape = shape;
        public readonly Angle Direction = direction;
        public readonly Kind Kind = kind;
        public readonly float MinDistance = minDistance;
        public readonly SafeWall[] SafeWalls = safeWalls != null ? [.. safeWalls] : [];
        public readonly ulong ActorID = actorID;
        public readonly bool IgnoreImmunes = ignoreImmunes;
    }

    public bool StopAtWall = stopAtWall;   // wall is solid: the push stops at the boundary rather than crossing it
    public readonly int MaxCasts = maxCasts;

    /// <summary>Whether the player in <paramref name="slot"/> is knockback-immune at <paramref name="time"/>.
    /// Minerva does not track status immunity, so this is always false (knockbacks are always shown).</summary>
    public bool IsImmune(int slot, DateTime time) => false;

    public static WPos AwayFromSource(WPos pos, WPos origin, float distance) => pos != origin ? pos + distance * (pos - origin).Normalized() : pos;
    public static WPos AwayFromSource(WPos pos, Actor? source, float distance) => source != null ? AwayFromSource(pos, source.Position, distance) : pos;

    // subclasses return the active sources; multiple are applied sequentially in activation order
    public abstract ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor);

    /// <summary>Would the player, standing at <paramref name="pos"/> after a shove, be in danger (off the arena)?</summary>
    public virtual bool DestinationUnsafe(int slot, Actor actor, WPos pos) => !this.StopAtWall && !this.Module.Bounds.Contains(this.Module.Center, pos);

    /// <summary>The chain of from→to hops the player is pushed through this frame (one per active source).</summary>
    public List<(WPos from, WPos to)> CalculateMovements(int slot, Actor actor)
    {
        var movements = new List<(WPos, WPos)>();
        if (this.MaxCasts <= 0)
            return movements;

        var from = actor.Position;
        var count = 0;
        var sources = this.ActiveKnockbacks(slot, actor);
        foreach (ref readonly var s in sources)
        {
            if (s.Shape != null && !s.Shape.Check(from, s.Origin, s.Direction))
                continue; // player is outside the shove's AOE

            var dir = s.Kind switch
            {
                Kind.AwayFromOrigin => from != s.Origin ? (from - s.Origin).Normalized() : default,
                Kind.TowardsOrigin => from != s.Origin ? (s.Origin - from).Normalized() : default,
                Kind.DirBackward => (s.Direction + 180f.Degrees()).ToDirection(),
                Kind.DirForward => s.Direction.ToDirection(),
                Kind.DirLeft => s.Direction.ToDirection().OrthoL(),
                Kind.DirRight => s.Direction.ToDirection().OrthoR(),
                _ => default
            };
            if (dir == default)
                continue;

            var distance = s.Distance;
            if (s.Kind == Kind.TowardsOrigin)
                distance = Math.Min(distance, (s.Origin - from).Length() - s.MinDistance);
            if (distance <= 0f)
                continue;

            var to = from + distance * dir;
            movements.Add((from, to));
            from = to;
            if (++count == this.MaxCasts)
                break;
        }
        return movements;
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        foreach (var (_, to) in this.CalculateMovements(slot, actor))
        {
            if (this.DestinationUnsafe(slot, actor, to))
            {
                hints.Add("About to be knocked into danger!");
                return;
            }
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var (from, to) in this.CalculateMovements(pcSlot, pc))
        {
            if (from != to)
            {
                this.Arena.AddLine(from, to, Colors.Danger, 2f);
                this.Arena.AddCircle(to, 0.6f, Colors.Danger, 2f);
            }
        }
    }
}

/// <summary>
/// Knockback/attract from the target location of a watched cast. One line per mechanic — e.g.
/// <c>sealed class Shockwave(ModuleBase m) : Components.SimpleKnockbacks(m, (uint)AID.Shockwave, 15f);</c>
/// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class SimpleKnockbacks(ModuleBase module, uint aid, float distance, bool ignoreImmunes = false, int maxCasts = int.MaxValue, AOEShape? shape = null, GenericKnockback.Kind kind = GenericKnockback.Kind.AwayFromOrigin, float minDistance = default, bool minDistanceBetweenHitboxes = false, bool stopAtWall = false, bool stopAfterWall = false)
    : GenericKnockback(module, aid, maxCasts, stopAtWall)
{
    public readonly float Distance = distance;
    public readonly AOEShape? Shape = shape;
    public readonly Kind KnockbackKind = kind;
    public readonly float MinDistance = minDistance;
    public readonly bool IgnoreImmunes = ignoreImmunes;
    public readonly bool MinDistanceBetweenHitboxes = minDistanceBetweenHitboxes;
    public readonly bool StopAfterWall = stopAfterWall;
    public readonly List<Knockback> Casters = [];

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor) => CollectionsMarshal.AsSpan(this.Casters);

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID != this.WatchedAction)
            return;
        var origin = cast.LocXZ != default ? cast.LocXZ : caster.Position;
        this.Casters.Add(new Knockback(origin, this.Distance, this.Module.CastFinishAt(cast), this.Shape, cast.Rotation, this.KnockbackKind, this.MinDistance, null, caster.InstanceID, this.IgnoreImmunes));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID != this.WatchedAction)
            return;
        for (var i = 0; i < this.Casters.Count; ++i)
        {
            if (this.Casters[i].ActorID == caster.InstanceID)
            {
                this.Casters.RemoveAt(i);
                return;
            }
        }
    }
}

/// <summary>A <see cref="SimpleKnockbacks"/> that watches several actions sharing one distance/shape.</summary>
public class SimpleKnockbackGroups(ModuleBase module, uint[] aids, float distance, bool ignoreImmunes = false, int maxCasts = int.MaxValue, AOEShape? shape = null, GenericKnockback.Kind kind = GenericKnockback.Kind.AwayFromOrigin, float minDistance = default, bool minDistanceBetweenHitboxes = false, bool stopAtWall = false, bool stopAfterWall = false)
    : SimpleKnockbacks(module, default, distance, ignoreImmunes, maxCasts, shape, kind, minDistance, minDistanceBetweenHitboxes, stopAtWall, stopAfterWall)
{
    protected readonly uint[] AIDs = aids;

    private bool Watches(uint id)
    {
        for (var i = 0; i < this.AIDs.Length; ++i)
            if (id == this.AIDs[i])
                return true;
        return false;
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (!this.Watches(cast.Action.ID))
            return;
        var origin = cast.LocXZ != default ? cast.LocXZ : caster.Position;
        this.Casters.Add(new Knockback(origin, this.Distance, this.Module.CastFinishAt(cast), this.Shape, cast.Rotation, this.KnockbackKind, this.MinDistance, null, caster.InstanceID, this.IgnoreImmunes));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (this.Watches(spell.Action.ID))
            ++this.NumCasts;
    }
}
