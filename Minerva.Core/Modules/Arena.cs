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

    /// <summary>A line in the default danger colour, as ported draw code spells it.</summary>
    /// <summary>
    /// Draw text at a world position — platform numbers, tower counts, whatever a fight labels its ground
    /// with.
    ///
    /// <para>Virtual with an empty body rather than abstract: every existing <see cref="Arena"/>
    /// implementation predates it, and a fight that labels its platforms is still worth having with the
    /// labels missing. A renderer that can draw text overrides this; one that cannot silently omits it,
    /// which is the right failure for a decoration.</para>
    /// </summary>
    public virtual void TextWorld(WPos center, string text, uint color, float fontSize = 17f, uint outlineColor = 0u, float outlineWidth = 0f)
    {
    }

    /// <summary>
    /// Draw a single icon glyph centred on a world position.
    ///
    /// <para>Separate from <see cref="TextWorld"/> because an icon codepoint has to be drawn in the ICON
    /// font — rendered in the body font it comes out as a missing-glyph box. A renderer without that font
    /// omits it, same bargain as text.</para>
    /// </summary>
    public virtual void IconWorld(WPos center, char glyph, uint color, float fontSize = 17f)
    {
    }

    public void AddLine(WPos a, WPos b) => this.AddLine(a, b, Colors.Danger);

    /// <summary>Draw an actor as a facing arrow of the given radius.</summary>
    public abstract void ActorMarker(WPos pos, Angle rotation, float radius, uint color);

    /// <summary>
    /// Mark a gaze source with an eye glyph, coloured by whether it currently has you.
    /// <para>A plain ring says "something is here"; among a screenful of AOE rings that is most of what it
    /// says. The shape is what makes it read as a gaze at a glance, which is the whole job of the marker —
    /// you are turning, not reading.</para>
    /// <para>Virtual rather than abstract: a renderer that has no way to draw one falls back to the ring
    /// it drew before, which is worse but not wrong.</para>
    /// </summary>
    public virtual void Eye(WPos pos, float radius, uint color) => this.AddCircle(pos, radius, color, 2f);

    /// <summary>
    /// Where a world position lands on screen. Needed by components that draw a fixed-size glyph — an eye,
    /// an icon — which must not scale with the arena zoom. Defaults to the world position so a headless
    /// renderer (the tests, the offline validator) has something coherent rather than throwing.
    /// </summary>
    public virtual Vector2 WorldPositionToScreenPosition(WPos p) => new(p.X, p.Z);

    /// <summary>Draw the arena boundary.</summary>
    public abstract void DrawBoundary();

    // BMR-compatible actor-drawing helpers so ported modules that call Arena.Actor / Arena.Actors compile
    /// <summary>
    /// Draw an actor marker. <paramref name="allowDeadAndUntargetable"/> defaults to <c>false</c>, matching
    /// BossmodReborn, whose modules are written against that default.
    ///
    /// <para>It was <c>true</c> here, and the difference is not cosmetic: a dead add keeps its marker until
    /// the server despawns the object, which is many seconds later. Measured in Double Trouble -- 18
    /// Entanglement adds, each drawn for a median of 12.9s AFTER dying, 210s of corpses over one clear.
    /// Seven were on screen at once, and they read as a mechanic that had frozen. Anything that genuinely
    /// wants to show a corpse or an untargetable boss passes <c>true</c> explicitly, as BossmodReborn's own
    /// modules do.</para>
    /// </summary>
    public void Actor(Actor? actor, uint color = default, bool allowDeadAndUntargetable = false)
    {
        if (actor != null && (allowDeadAndUntargetable || (!actor.IsDeadOrDestroyed && actor.IsTargetable)))
            this.ActorMarker(actor.Position, actor.Rotation, actor.HitboxRadius, color == default ? Colors.Enemy : color);
    }

    /// <summary>A marker at a bare position and facing, for something that is not an actor — a mirror, a
    /// remembered spot, a predicted destination.</summary>
    public void Actor(WPos position, Angle rotation, uint color = default)
        => this.ActorMarker(position, rotation, 1f, color == default ? Colors.Enemy : color);

    public void Actors(IEnumerable<Actor> actors, uint color = default, bool allowDeadAndUntargetable = false)
    {
        foreach (var a in actors)
            this.Actor(a, color, allowDeadAndUntargetable);
    }

    public void Actors(ModuleBase module, uint[] oids, uint color = default, bool allowDeadAndUntargetable = false)
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

    // --- path building -------------------------------------------------------------------------------
    //
    // Modules draw one-off shapes -- a knockback's safe arc, a platform outline -- by accumulating points
    // and stroking them. Built on AddLine so every renderer gets it without implementing anything, and the
    // buffer is reused across calls rather than reallocated: this runs per party member per frame.

    private readonly List<WPos> path = [];

    /// <summary>Append a point to the path being built.</summary>
    public void PathLineTo(WPos p) => this.path.Add(p);

    /// <summary>
    /// Append an arc of <paramref name="radius"/> about <paramref name="center"/>, from
    /// <paramref name="angleFrom"/> to <paramref name="angleTo"/> in radians. Segment count follows the
    /// arc's length so a small arc does not pay for 48 segments and a large one does not look faceted.
    /// </summary>
    public void PathArcTo(WPos center, float radius, float angleFrom, float angleTo)
    {
        var span = angleTo - angleFrom;
        var segments = Math.Clamp((int)MathF.Ceiling(MathF.Abs(span) * 8f), 2, 96);
        for (var i = 0; i <= segments; ++i)
        {
            var a = angleFrom + (span * i / segments);
            this.path.Add(center + (radius * new Angle(a).ToDirection()));
        }
    }

    /// <summary>Draw the accumulated path and clear it. Always clears, even when there is nothing to draw,
    /// so a module that builds a path and takes an early return cannot leak points into the next one.</summary>
    public void PathStroke(bool closed, uint color, float thickness = 1f)
    {
        for (var i = 1; i < this.path.Count; ++i)
            this.AddLine(this.path[i - 1], this.path[i], color, thickness);
        if (closed && this.path.Count > 2)
            this.AddLine(this.path[^1], this.path[0], color, thickness);
        this.path.Clear();
    }

    /// <summary>Outline a closed polygon.</summary>
    public void AddPolygon(IEnumerable<WPos> points, uint color, float thickness = 1f)
    {
        foreach (var p in points)
            this.path.Add(p);
        this.PathStroke(true, color, thickness);
    }

    /// <summary>A filled triangle. Virtual so a renderer with a real fill primitive can use it; the default
    /// outlines, which is visible but not solid.</summary>
    public virtual void AddTriangleFilled(WPos a, WPos b, WPos c, uint color)
    {
        this.AddLine(a, b, color);
        this.AddLine(b, c, color);
        this.AddLine(c, a, color);
    }

    /// <summary>Fill a triangle given its three corners.</summary>
    public void ZoneTri(WPos a, WPos b, WPos c, uint color)
        => this.AddTriangleFilled(a, b, c, color == default ? Colors.AOE : color);

    /// <summary>Fill an isosceles triangle from its apex, given height and half-base as vectors.</summary>
    public void ZoneIsoscelesTri(WPos apex, WDir height, WDir halfBase, uint color)
        => this.AddTriangleFilled(apex, apex + height + halfBase, apex + height - halfBase, color == default ? Colors.AOE : color);

    /// <summary>The same, described the way a cone is: a direction, a half-angle and a height.</summary>
    public void ZoneIsoscelesTri(WPos apex, Angle direction, Angle halfAngle, float height, uint color)
    {
        var dir = direction.ToDirection();
        var h = height * dir;
        var halfBase = height * halfAngle.Tan() * dir.OrthoL();
        this.ZoneIsoscelesTri(apex, h, halfBase, color);
    }

    /// <summary>Outline a circular zone (safe spots, orb footprints) without filling it.</summary>
    public void ZoneCircleOutline(WPos center, float radius, uint color = default, float thickness = 1f)
        => this.AddCircle(center, radius, color == default ? Colors.Safe : color, thickness);

    /// <summary>
    /// The same outline, under BossmodReborn's name for the variant that skips arena clipping.
    /// <para>An alias rather than a second implementation: BossmodReborn clips zone drawing to the arena
    /// polygon and offers this to opt out, while Minerva's arena drawing never clipped. Carried so ports
    /// compile and so the distinction is documented rather than quietly dropped -- if Minerva ever grows
    /// clipping, this is the call site that must not get it.</para>
    /// </summary>
    public void ZoneCircleOutlineUnclipped(WPos center, float radius, uint color = default, float thickness = 1f)
        => this.ZoneCircleOutline(center, radius, color, thickness);

    // Filled zones by shape, spelled the way BossmodReborn spells them. Modules use these for the inverse of
    // an AOE — the safe spot, the tower, the tile you have to stand on — which is why they take an explicit
    // colour rather than defaulting to the danger colour the AOE components use.

    public void ZoneCircle(WPos center, float radius, uint color)
        => this.ZoneShape(new AOEShapeCircle(radius), center, default, color);

    public void ZoneDonut(WPos center, float innerRadius, float outerRadius, uint color)
        => this.ZoneShape(new AOEShapeDonut(innerRadius, outerRadius), center, default, color);

    public void ZoneCone(WPos center, float innerRadius, float outerRadius, Angle centerDirection, Angle halfAngle, uint color)
        => this.ZoneShape(
            innerRadius > 0f ? new AOEShapeDonutSector(innerRadius, outerRadius, halfAngle) : new AOEShapeCone(outerRadius, halfAngle),
            center, centerDirection, color);

    public void ZoneRect(WPos origin, Angle direction, float lenFront, float lenBack, float halfWidth, uint color)
        => this.ZoneShape(new AOEShapeRect(lenFront, halfWidth, lenBack), origin, direction, color);

    public void ZoneRect(WPos origin, WDir direction, float lenFront, float lenBack, float halfWidth, uint color)
        => this.ZoneRect(origin, direction.ToAngle(), lenFront, lenBack, halfWidth, color);

    /// <summary>A rectangle spanning two points — the shape a line between two markers sweeps.</summary>
    public void ZoneRect(WPos start, WPos end, float halfWidth, uint color)
    {
        var delta = end - start;
        this.ZoneRect(start, delta.ToAngle(), delta.Length(), 0f, halfWidth, color);
    }

    /// <summary>Outline a rectangle. Built from four lines so every renderer gets it for free.</summary>
    public void AddRect(WPos origin, WDir direction, float lenFront, float lenBack, float halfWidth, uint color = default, float thickness = 1f)
    {
        var side = direction.OrthoR() * halfWidth;
        var front = origin + direction * lenFront;
        var back = origin - direction * lenBack;
        var c = color == default ? Colors.Safe : color;
        this.AddLine(front + side, front - side, c, thickness);
        this.AddLine(back + side, back - side, c, thickness);
        this.AddLine(front + side, back + side, c, thickness);
        this.AddLine(front - side, back - side, c, thickness);
    }

    public void ZoneConeOutline(WPos center, float innerRadius, float outerRadius, Angle centerDirection, Angle halfAngle, uint color = default, float thickness = 1f)
        => this.OutlineShape(
            innerRadius > 0f ? new AOEShapeDonutSector(innerRadius, outerRadius, halfAngle) : new AOEShapeCone(outerRadius, halfAngle),
            center, centerDirection, color == default ? Colors.Safe : color, thickness);

    /// <summary>Draw the actor only if it is standing inside the arena.</summary>
    public void ActorInsideBounds(WPos position, Angle rotation, uint color = default)
    {
        if (this.InBounds(position))
            this.ActorMarker(position, rotation, 1f, color == default ? Colors.Enemy : color);
    }

    /// <summary>Draw the actor projected onto the boundary when it is outside — an add winding up off-arena
    /// is still about to hit you, so it needs to appear somewhere rather than vanish.</summary>
    public void ActorOutsideBounds(WPos position, Angle rotation, uint color = default)
    {
        if (!this.InBounds(position))
            this.ActorProjected(position, position, rotation, color == default ? Colors.Enemy : color);
    }

    /// <summary>
    /// The nearest point inside the arena. Modules clamp a computed destination through this so a suggested
    /// spot never lands outside the walkable floor — off an edge the player cannot reach is worse advice
    /// than a spot slightly inside it.
    /// </summary>
    public WPos ClampToBounds(WPos position)
    {
        if (this.InBounds(position))
            return position;
        var delta = position - this.Center;
        var dist = delta.Length();
        if (dist < 1e-4f)
            return position;
        var dir = delta / dist;
        return this.Center + dir * MathF.Min(dist, this.IntersectRayBounds(this.Center, dir));
    }
}
