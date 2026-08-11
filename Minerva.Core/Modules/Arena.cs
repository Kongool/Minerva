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
}
