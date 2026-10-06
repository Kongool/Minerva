using System.Runtime.InteropServices;

namespace Minerva.Components;

/// <summary>
/// Generic knockback / attract component: subclasses expose a set of active <see cref="Knockback"/>
/// sources and this base predicts where the player is shoved, draws the arrow, and warns when the
/// landing spot leaves the arena. Ported from BossmodReborn's GenericKnockback (BSD-3; see
/// THIRD-PARTY-NOTICES.txt), simplified for Minerva's local-player focus — <see cref="Knockback.SafeWalls"/> are carried
/// but not ray-tested. Unlike BossmodReborn it also tells the dodge where not to stand (<see cref="AddAIHints"/>).
/// </summary>
public abstract class GenericKnockback(ModuleBase module, uint aid = default, int maxCasts = int.MaxValue, bool stopAtWall = false, bool stopAfterWall = false) : CastCounter(module, aid)
{
    /// <summary>
    /// Draw where a knockback puts someone: a ghost marker at the destination and a line showing the path.
    /// Static because ported modules call it from components that are not knockbacks themselves, to preview
    /// a displacement they are reasoning about.
    /// </summary>
    public static void DrawKnockback(WPos from, WPos to, Angle rot, Arena arena)
    {
        if (from == to)
            return;
        arena.ActorProjected(from, to, rot, Colors.Danger);
        arena.AddLine(from, to);
    }

    public static void DrawKnockback(Actor actor, WPos adjustedPos, Arena arena)
        => DrawKnockback(actor.Position, adjustedPos, actor.Rotation, arena);

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
        bool ignoreImmunes = false,
        int? arenaProjectionLayer = null,
        bool? restrictToArenaProjectionLayer = false)
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

        /// <summary>Floor this knockback belongs to, for BossmodReborn parity. Inert: see <see cref="ModuleComponent.ArenaProjectionLayer"/>.</summary>
        public readonly int? ArenaProjectionLayer = arenaProjectionLayer;
        public readonly bool? RestrictToArenaProjectionLayer = restrictToArenaProjectionLayer;
    }

    public bool StopAtWall = stopAtWall;   // wall is solid: the push stops at the boundary rather than crossing it

    /// <summary>The push carries you INTO the wall and leaves you there, rather than being stopped by it.
    /// <para>Different from <see cref="StopAtWall"/> in what it means for safety: stopping at the wall keeps
    /// you inside the arena, so the destination is fine; ending up in the wall does not, so a destination
    /// outside the boundary is still a destination -- it just is not a death by falling.</para></summary>
    public bool StopAfterWall = stopAfterWall;
    public readonly int MaxCasts = maxCasts;

    /// <summary>
    /// Immunity buffs held by each party member. Modules may set an entry directly for an immunity the
    /// status table cannot see — a duty mechanic that grants it without a status.
    /// </summary>
    public readonly PlayerImmuneState[] PlayerImmunes = new PlayerImmuneState[PartyState.MaxSlots];

    /// <summary>
    /// Whether the player in <paramref name="slot"/> is knockback-immune at <paramref name="time"/>.
    ///
    /// <para>This was hardcoded to <c>false</c>, on the reasoning that showing a knockback that cannot
    /// happen is safer than hiding one that can. True as far as safety goes, but the cost was uptime: a
    /// dozen landed modules already ask this before forbidding ground, so a player holding Arm's Length was
    /// still being walked out of a shove that could not move them. Tracking it properly makes those modules
    /// behave the way they were written to.</para>
    /// </summary>
    public bool IsImmune(int slot, DateTime time)
        => slot >= 0 && slot < PartyState.MaxSlots && this.PlayerImmunes[slot].ImmuneAt(time);

    public override void OnStatusGain(Actor actor, ref ActorStatus status) => this.TrackImmunity(actor, status.ID, status.ExpireAt);

    public override void OnStatusLose(Actor actor, ref ActorStatus status) => this.TrackImmunity(actor, status.ID, default);

    private void TrackImmunity(Actor actor, uint sid, DateTime expireAt)
    {
        var slot = this.World.Party.FindSlot(actor.InstanceID);
        if (slot >= 0)
            ImmunityStatuses.Track(ref this.PlayerImmunes[slot], sid, expireAt);
    }

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
            var to = this.Displace(s, from);
            if (to == from)
                continue;
            movements.Add((from, to));
            from = to;
            if (++count == this.MaxCasts)
                break;
        }
        return movements;
    }

    // BossmodReborn's allowance for where the wall test meets the boundary: 0.5 less its approximate hitbox, 0.499
    private const float MaxIntersectionError = 0.001f;

    /// <summary>Where one source puts someone standing at <paramref name="from"/>; <paramref name="from"/> itself when it
    /// does not move them (outside its shape, on its origin, or already at the pull's minimum).</summary>
    public WPos Displace(in Knockback s, WPos from)
    {
        if (s.Shape != null && !s.Shape.Check(from, s.Origin, s.Direction))
            return from; // outside the shove's AOE

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
            return from;

        var distance = s.Distance;
        if (s.Kind == Kind.TowardsOrigin)
            distance = Math.Min(distance, (s.Origin - from).Length() - s.MinDistance);
        // BossmodReborn's clamps: a solid wall stops the push at the boundary, StopAfterWall carries it just past
        if (this.StopAtWall)
            distance = Math.Min(distance, this.Module.Bounds.IntersectRay(this.Module.Center, from, dir) - MaxIntersectionError);
        if (this.StopAfterWall)
            distance = Math.Min(distance, this.Module.Bounds.IntersectRay(this.Module.Center, from, dir) + MaxIntersectionError);
        return distance > 0f ? from + distance * dir : from;
    }

    /// <summary>A zone that goes off this long after a shove is one it can throw you into; any later and there is time
    /// to walk out of it.</summary>
    public const float LandingWindowSeconds = 3f;

    // A zone resolving with the shove has already hit where you stood, not where you land. Casts that end together are
    // stamped a frame or two apart.
    private const float SameMomentSeconds = 0.2f;

    /// <summary>
    /// Forbid standing where the shove lands you in a zone going off just after it. The Oracle of Light, newtoon2
    /// 2026-10-04: clear of all eight Burns, she was thrown 11.8y by Unbridled Wrath into one with a second left. The
    /// arrow and the text warning said where she would land; nothing told the dodge. BossmodReborn leaves this to each
    /// module, and only some of them do it.
    /// <para>One zone per source, timed at its shove: by the next one the character has had time to move. Skipped
    /// while immune. A subclass that writes its own hints replaces this, as before.</para>
    /// </summary>
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (this.MaxCasts <= 0)
            return;
        var sources = this.ActiveKnockbacks(slot, actor);
        foreach (ref readonly var s in sources)
        {
            if (s.Activation == default || !s.IgnoreImmunes && this.IsImmune(slot, s.Activation))
                continue;
            if (this.ZonesAfter(slot, actor, s.Activation) is { Length: > 0 } after)
                hints.AddForbiddenZone(new SDLanding(this, s, after), s.Activation);
        }
    }

    /// <summary>The module's risky zones going off within <see cref="LandingWindowSeconds"/> after a shove at
    /// <paramref name="shove"/>.</summary>
    private ShapeDistance[] ZonesAfter(int slot, Actor actor, DateTime shove)
    {
        List<ShapeDistance>? zones = null;
        var components = this.Module.Components;
        for (var i = 0; i < components.Count; ++i)
        {
            if (components[i] is not GenericAOEs aoes)
                continue;
            var active = aoes.ActiveAOEs(slot, actor);
            foreach (ref readonly var aoe in active)
            {
                var after = (aoe.Activation - shove).TotalSeconds;
                if (aoe.Risky && after >= SameMomentSeconds && after <= LandingWindowSeconds
                    && this.Module.MechanicAppliesToArenaProjectionLayer(actor, aoe.ArenaProjectionLayer, aoe.RestrictToArenaProjectionLayer))
                    (zones ??= []).Add(aoe.ShapeDistance ?? aoe.Shape.Distance(aoe.Origin, aoe.Rotation));
            }
        }
        return zones != null ? [.. zones] : [];
    }

    /// <summary>"Standing here, the shove lands you in one of these": each zone's distance, taken at the landing point.
    /// Ground the shove does not move you from is clear as far as this is concerned; the zones forbid it themselves.</summary>
    private sealed class SDLanding(GenericKnockback owner, Knockback source, ShapeDistance[] zones) : ShapeDistance
    {
        public override float Distance(WPos p)
        {
            var to = owner.Displace(source, p);
            if (to == p)
                return 1000f; // "far", as the other fields say it
            var d = float.MaxValue;
            for (var i = 0; i < zones.Length; ++i)
                d = Math.Min(d, zones[i].Distance(to));
            return d;
        }
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
    : GenericKnockback(module, aid, maxCasts, stopAtWall, stopAfterWall)
{
    public readonly float Distance = distance;
    public readonly AOEShape? Shape = shape;
    public readonly Kind KnockbackKind = kind;
    public readonly float MinDistance = minDistance;
    public readonly bool IgnoreImmunes = ignoreImmunes;
    public readonly bool MinDistanceBetweenHitboxes = minDistanceBetweenHitboxes;
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
