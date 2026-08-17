namespace Minerva;

/// <summary>
/// A concrete danger zone in the world: a <see cref="AOEShape"/> placed at an origin/rotation,
/// resolving (dealing damage) at <see cref="Activation"/>. Components maintain lists of these and
/// the radar draws them; hint logic warns when the local player stands inside one.
/// </summary>
public struct AOEInstance(AOEShape shape, WPos origin, Angle rotation = default, DateTime activation = default, uint color = default, bool risky = true, ulong actorID = default, ShapeDistance? shapeDistance = null)
{
    // mutable (matching BMR) so components like SimpleAOEs can recolour / re-flag instances per frame
    public AOEShape Shape = shape;
    public WPos Origin = origin;
    public Angle Rotation = rotation;
    public DateTime Activation = activation;
    public uint Color = color;   // 0 = renderer default
    public bool Risky = risky;   // if false, drawn but not counted as "you're in danger"
    public ulong ActorID = actorID;
    public ShapeDistance? ShapeDistance = shapeDistance; // optional cached SDF (matching BMR; unused by Minerva's renderer)

    public readonly bool Check(WPos pos) => this.Shape.Check(pos, this.Origin, this.Rotation);
    public readonly IReadOnlyList<WPos> Contour() => this.Shape.Contour(this.Origin, this.Rotation);
}
