namespace Minerva;

/// <summary>
/// Ground somebody has stood on, by two-yalm cell: the one fact about a floor that cannot be wrong, kept so
/// the floor probe (a downward raycast) is overruled where it contradicts it. Eureka Orthos, 2026-09-06:
/// the probe rejected a dodge target three yalms from the player on every frame for six seconds, on a flat
/// floor the whole party had just walked, and the character stood in a Nerve Gas cone instead.
/// </summary>
public sealed class KnownGround
{
    public const float Cell = 2f;

    /// <summary>A whole deep-dungeon floor set at two yalms is well under this; the cap only bounds a leak.</summary>
    public const int MaxCells = 32768;

    private readonly HashSet<(int X, int Z)> cells = [];

    public int Count => this.cells.Count;

    public void Reset() => this.cells.Clear();

    public void Observe(WPos p)
    {
        if (this.cells.Count < MaxCells)
            this.cells.Add(Key(p));
    }

    /// <summary>Has anyone stood in this cell?</summary>
    public bool Contains(WPos p) => this.cells.Contains(Key(p));

    private static (int X, int Z) Key(WPos p) => ((int)MathF.Floor(p.X / Cell), (int)MathF.Floor(p.Z / Cell));
}
