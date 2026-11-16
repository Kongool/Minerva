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

    /// <summary>
    /// Full, the ground starts over from where everyone stands now. It used to stop learning instead, and in one long
    /// visit to an open zone everything not stood on before the cap stayed solid. Occult Crescent, 2026-11-15: about an
    /// hour in (after Eye to Eye), the ground under every toon read as wall outside a fight, and the dodge ran them
    /// about looking for the floor; with a module loaded its own arena replaces this, so fights were fine.
    /// </summary>
    public void Observe(WPos p)
    {
        if (this.cells.Count >= MaxCells)
            this.cells.Clear();
        this.cells.Add(Key(p));
    }

    /// <summary>Has anyone stood in this cell?</summary>
    public bool Contains(WPos p) => this.cells.Contains(Key(p));

    /// <summary>
    /// Has anyone stood in this cell or one touching it? The looser test, for deciding where the dodge may
    /// send someone rather than whether a single point is solid: the edge of the ground people have walked
    /// is still ground, and insisting on an exact cell would pin the character to the party's own footprints.
    /// One cell of slack is two yalms.
    /// </summary>
    public bool Near(WPos p)
    {
        var (x, z) = Key(p);
        for (var dx = -1; dx <= 1; ++dx)
            for (var dz = -1; dz <= 1; ++dz)
                if (this.cells.Contains((x + dx, z + dz)))
                    return true;
        return false;
    }

    private static (int X, int Z) Key(WPos p) => ((int)MathF.Floor(p.X / Cell), (int)MathF.Floor(p.Z / Cell));
}
