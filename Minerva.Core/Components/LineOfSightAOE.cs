using System.Runtime.InteropServices;

namespace Minerva.Components;

/// <summary>
/// A line-of-sight mechanic: an AOE radiates from an origin and is blocked by obstacles, so the safe
/// ground is each blocker's shadow. Instead of drawing the danger, this builds the *safe* zone — the
/// union of the shadows, minus the blockers themselves when you can't stand inside them — and hands it
/// to the auto-dodge inverted. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public abstract class GenericLineOfSightAOE(ModuleBase module, uint aid, float maxRange, bool blockersImpassable = false, bool rect = false, bool safeInsideHitbox = true) : GenericAOEs(module, aid, "Hide behind obstacle!")
{
    public DateTime NextExplosion;
    public readonly bool BlockersImpassable = blockersImpassable;
    public readonly bool SafeInsideHitbox = safeInsideHitbox;
    public readonly float MaxRange = maxRange;
    public readonly bool Rect = rect; // the AOE is a rectangle (a directional beam) rather than a circle
    public BitMask IgnoredPlayers;
    public WPos? Origin; // inactive if null
    public readonly List<(WPos Center, float Radius)> Blockers = [];
    public readonly List<(float Distance, Angle Dir, Angle HalfWidth)> Visibility = [];
    public readonly List<AOEInstance> Safezones = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
        => this.Safezones.Count != 0 && !this.IgnoredPlayers[slot] ? CollectionsMarshal.AsSpan(this.Safezones)[..1] : [];

    /// <summary>Point the mechanic at a new origin and blocker set, recomputing each blocker's shadow.</summary>
    public void Modify(WPos? origin, IEnumerable<(WPos Center, float Radius)> blockers, DateTime nextExplosion = default)
    {
        this.NextExplosion = nextExplosion;
        this.Origin = origin;
        this.Blockers.Clear();
        this.Blockers.AddRange(blockers);
        this.Visibility.Clear();
        if (origin == null)
            return;

        foreach (var b in this.Blockers)
        {
            var toBlock = b.Center - origin.Value;
            var dist = toBlock.Length();
            // angular half-width of the shadow this blocker casts; standing inside it means standing behind it
            var halfWidth = b.Radius < dist ? Angle.Asin(b.Radius / dist) : 90f.Degrees();
            this.Visibility.Add((dist, Angle.FromDirection(toBlock), halfWidth));
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var aoes = this.ActiveAOEs(slot, actor);
        for (var i = 0; i < aoes.Length; ++i)
        {
            ref readonly var c = ref aoes[i];
            // the instance is the SAFE zone, so being outside it (and in range) is what's dangerous
            if (c.Risky && !c.Check(actor.Position) && this.Origin != null && (this.Origin.Value - actor.Position).LengthSq() < this.MaxRange * this.MaxRange)
            {
                hints.Add(this.WarningText);
                return;
            }
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction)
            this.AddSafezone(this.Module.CastFinishAt(cast), cast.Rotation);
    }

    /// <summary>
    /// Tell the dodge which blockers are solid, so it walks around them instead of through them.
    /// <para>The safe ground in this mechanic is the shadow behind an object, which puts the object itself
    /// squarely between the player and where they need to be. Without this the solver scores that shadow by
    /// straight-line distance and steers at it through the very thing casting it.</para>
    /// <para>Gated on the same condition that carves blockers out of the safe zone, because the two are the
    /// same claim: with <see cref="SafeInsideHitbox"/> set and no impassability declared, standing inside
    /// the blocker is explicitly allowed, and making it an obstacle would fence the character out of ground
    /// this component has just called safe.</para>
    /// </summary>
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        if (!this.BlockersImpassable && this.SafeInsideHitbox)
            return;

        for (var i = 0; i < this.Blockers.Count; ++i)
        {
            var b = this.Blockers[i];
            hints.TemporaryObstacles.Add(new SDCircle(b.Center, this.SafeInsideHitbox ? b.Radius + 0.5f : b.Radius));
        }
    }

    /// <summary>Build the safe zone for one resolution from the current origin/blockers.</summary>
    public void AddSafezone(DateTime activation, Angle rotation = default)
    {
        if (this.Origin == null)
            return;

        List<Shape> unionShapes = [];
        List<Shape> differenceShapes = [];
        if (!this.Rect)
        {
            // circular AOE: each shadow is the donut segment behind its blocker, out to max range
            foreach (var v in this.Visibility)
                unionShapes.Add(new DonutSegmentHA(this.Origin.Value, v.Distance + 0.2f, this.MaxRange, v.Dir, v.HalfWidth));
        }
        else
        {
            // directional beam: each shadow is a strip behind its blocker, along the cast's facing
            var dir = rotation.ToDirection();
            foreach (var b in this.Blockers)
                unionShapes.Add(new RectangleSE(b.Center + 0.2f * dir, b.Center + this.MaxRange * dir, b.Radius));
        }

        if (this.BlockersImpassable || !this.SafeInsideHitbox)
            foreach (var b in this.Blockers)
                differenceShapes.Add(new Circle(b.Center, this.SafeInsideHitbox ? b.Radius + 0.5f : b.Radius));

        if (unionShapes.Count == 0)
            return;

        var origin = this.Module.Center;
        var shape = new AOEShapeCustom(origin, [.. unionShapes], [.. differenceShapes], invertForbiddenZone: true);
        this.Safezones.Add(new AOEInstance(shape, origin, default, activation, Colors.SafeFromAOE, shapeDistance: shape.Distance(origin, default)));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (this.Safezones.Count != 0 && cast.Action.ID == this.WatchedAction)
            this.Safezones.RemoveAt(0);
    }
}

/// <summary>
/// Line-of-sight AOE that resolves at the end of a cast. Subclasses supply the blocking actors via
/// <see cref="BlockerActors"/>. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public abstract class CastLineOfSightAOE : GenericLineOfSightAOE
{
    public readonly List<Actor> Casters = [];

    /// <summary>The caster resolving soonest — the one the current safe zone is built for.</summary>
    public Actor? ActiveCaster
    {
        get
        {
            Actor? activeCaster = null;
            var minRemainingTime = double.MaxValue;
            foreach (var caster in this.Casters)
            {
                if (caster.CastInfo != null && caster.CastInfo.RemainingTime < minRemainingTime)
                {
                    minRemainingTime = caster.CastInfo.RemainingTime;
                    activeCaster = caster;
                }
            }
            return activeCaster;
        }
    }

    protected CastLineOfSightAOE(ModuleBase module, uint aid, float maxRange, bool blockersImpassable = false, bool rect = false, bool safeInsideHitbox = true)
        : base(module, aid, maxRange, blockersImpassable, rect, safeInsideHitbox) => this.Refresh();

    public abstract ReadOnlySpan<Actor> BlockerActors();

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction)
        {
            this.Casters.Add(caster);
            this.Refresh();
            this.AddSafezone(this.Module.CastFinishAt(cast), cast.Rotation);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID != this.WatchedAction)
            return;
        this.Casters.Remove(caster);
        this.Refresh();
        if (this.Safezones.Count != 0)
            this.Safezones.RemoveAt(0);
    }

    /// <summary>Re-point the mechanic at the soonest caster and the current blocker positions.</summary>
    protected void Refresh()
    {
        var caster = this.ActiveCaster;
        // The AOE radiates from where it is cast. Location is only filled for area-targeted casts, so a boss
        // self-casting a line-of-sight mechanic reports (0,0) — and shadows computed from the world origin
        // arrive as near-parallel bands across the arena instead of wedges behind each blocker, pointing the
        // player at cover that isn't there. Fall back to the caster, matching SimpleAOEs' own convention.
        WPos? position = caster?.CastInfo is { } ci ? (ci.LocXZ != default ? ci.LocXZ : caster.Position) : null;
        var blockers = this.BlockerActors();
        var blockerData = new (WPos, float)[blockers.Length];
        for (var i = 0; i < blockers.Length; ++i)
            blockerData[i] = (blockers[i].Position, blockers[i].HitboxRadius);
        this.Modify(position, blockerData, caster?.CastInfo != null ? this.Module.CastFinishAt(caster.CastInfo) : default);
    }
}
