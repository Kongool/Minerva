namespace Minerva;

/// <summary>
/// A concrete danger zone in the world: a <see cref="AOEShape"/> placed at an origin/rotation,
/// resolving (dealing damage) at <see cref="Activation"/>. Components maintain lists of these and
/// the radar draws them; hint logic warns when the local player stands inside one.
/// </summary>
public readonly struct AOEInstance(AOEShape shape, WPos origin, Angle rotation = default, DateTime activation = default, uint color = default, bool risky = true, ulong actorID = default)
{
    public readonly AOEShape Shape = shape;
    public readonly WPos Origin = origin;
    public readonly Angle Rotation = rotation;
    public readonly DateTime Activation = activation;
    public readonly uint Color = color;   // 0 = renderer default
    public readonly bool Risky = risky;   // if false, drawn but not counted as "you're in danger"
    public readonly ulong ActorID = actorID;

    public bool Check(WPos pos) => this.Shape.Check(pos, this.Origin, this.Rotation);
    public IReadOnlyList<WPos> Contour() => this.Shape.Contour(this.Origin, this.Rotation);
}
