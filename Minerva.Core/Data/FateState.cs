namespace Minerva;

/// <summary>The FATE the player is inside: its id, its centre, and the radius the game itself declares.</summary>
public readonly record struct FateState(uint ID, WPos Center, float Radius)
{
    public bool Active => this.Radius > 0f;

    /// <summary>Is this point inside the FATE? Outside it, participation lapses.</summary>
    public bool Contains(WPos p) => this.Active && (p - this.Center).LengthSq() <= this.Radius * this.Radius;
}
