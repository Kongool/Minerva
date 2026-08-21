namespace Minerva;

/// <summary>
/// Abstract drawing surface for the radar. Components draw through this in world coordinates; the
/// plugin supplies a concrete ImGui-backed implementation that knows how to render each shape type
/// optimally (native circles, triangulated donuts, convex fills). Keeping it abstract is what lets
/// the whole module/component layer live in the game-free core and be tested without a renderer.
/// </summary>
public abstract class Arena
{
    public WPos Center;
    public ArenaBounds Bounds = new ArenaBoundsCircle(20f);

    /// <summary>Filled danger zone for a shape at origin/rotation.</summary>
    public abstract void ZoneShape(AOEShape shape, WPos origin, Angle rotation, uint color);

    /// <summary>Outline of a shape at origin/rotation.</summary>
    public abstract void OutlineShape(AOEShape shape, WPos origin, Angle rotation, uint color, float thickness = 1f);

    public abstract void AddCircle(WPos center, float radius, uint color, float thickness = 1f);
    public abstract void AddCircleFilled(WPos center, float radius, uint color);
    public abstract void AddLine(WPos a, WPos b, uint color, float thickness = 1f);

    /// <summary>Draw an actor as a facing arrow of the given radius.</summary>
    public abstract void ActorMarker(WPos pos, Angle rotation, float radius, uint color);

    /// <summary>Draw the arena boundary.</summary>
    public abstract void DrawBoundary();

    // BMR-compatible actor-drawing helpers so ported modules that call Arena.Actor / Arena.Actors compile
    public void Actor(Actor? actor, uint color = default, bool allowDeadAndUntargetable = true)
    {
        if (actor != null && (allowDeadAndUntargetable || (!actor.IsDeadOrDestroyed && actor.IsTargetable)))
            this.ActorMarker(actor.Position, actor.Rotation, actor.HitboxRadius, color == default ? Colors.Enemy : color);
    }

    public void Actors(IEnumerable<Actor> actors, uint color = default, bool allowDeadAndUntargetable = true)
    {
        foreach (var a in actors)
            this.Actor(a, color, allowDeadAndUntargetable);
    }

    public void Actors(ModuleBase module, uint[] oids, uint color = default, bool allowDeadAndUntargetable = true)
        => this.Actors(module.Enemies(oids), color, allowDeadAndUntargetable);

    /// <summary>Is the point within the arena boundary?</summary>
    public bool InBounds(WPos p) => this.Bounds.Contains(this.Center, p);

    /// <summary>Distance from <paramref name="origin"/> along <paramref name="dir"/> to the boundary.</summary>
    public float IntersectRayBounds(WPos origin, WDir dir) => this.Bounds.IntersectRay(this.Center, origin, dir);

    /// <summary>
    /// Draw an actor marker at a projected destination — where a forced march or knockback would put it.
    /// A destination outside the arena is clamped to the boundary so the marker stays visible.
    /// </summary>
    public void ActorProjected(WPos from, WPos to, Angle rotation, uint color)
    {
        if (this.InBounds(to))
        {
            this.ActorMarker(to, rotation, 0.5f, color);
            return;
        }
        var dir = to - from;
        var len = dir.Length();
        if (len <= 0f)
            return;
        var t = this.IntersectRayBounds(from, dir / len);
        this.ActorMarker(from + Math.Min(t, len) * (dir / len), rotation, 0.5f, color);
    }

    /// <summary>Draw only the actors that are currently within the arena boundary.</summary>
    public void ActorsInBounds(IEnumerable<Actor> actors, uint color = default)
    {
        foreach (var a in actors)
            if (this.InBounds(a.Position))
                this.Actor(a, color);
    }

    public void ActorsInBounds(ModuleBase module, uint[] oids, uint color = default)
        => this.ActorsInBounds(module.Enemies(oids), color);

    /// <summary>Outline a circular zone (safe spots, orb footprints) without filling it.</summary>
    public void ZoneCircleOutline(WPos center, float radius, uint color = default, float thickness = 1f)
        => this.AddCircle(center, radius, color == default ? Colors.Safe : color, thickness);
}
