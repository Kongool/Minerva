namespace Minerva;

/// <summary>One polygon edge, stored as origin + delta with the inverse squared length precomputed.
/// <para>The reciprocal is cached because the point-to-segment projection divides by it on every query, and
/// these are queried per grid cell per rasterisation step. The epsilon keeps a degenerate (zero-length)
/// edge from producing an infinity rather than a large finite number.</para></summary>
public readonly struct Edge(float ax, float ay, float dx, float dy)
{
    private const float Epsilon = 1e-8f;

    public readonly float Ax = ax, Ay = ay, Dx = dx, Dy = dy, InvLengthSq = 1f / (dx * dx + dy * dy + Epsilon);
}

/// <summary>
/// A uniform grid over a polygon's edges: given a point, hand back only the edges in its cell.
///
/// <para>Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Coarser than
/// <c>PolygonBoundaryIndex2D</c> and deliberately so — the distance query it serves samples several points
/// per arena cell anyway, so exactness per query buys nothing that the sampling does not already average
/// out.</para>
///
/// <para><b>Locality is load-bearing, not just an optimisation.</b> A point far from every edge lands in an
/// empty cell and gets back nothing, so a caller that loops over the result without handling "no edges
/// here" reads a distance of infinity. That is why the distance functions in
/// <see cref="SDPolygonWithHolesBase"/> answer the inside/outside test first and only then measure.</para>
/// </summary>
public sealed class SpatialIndex
{
    private const float InvGridSize = 1f / 5f;

    private readonly int[][] grid;
    private readonly Edge[] edges;
    private readonly int minX, minY, gridWidth, gridHeight;

    public SpatialIndex(Edge[] edges)
    {
        this.edges = edges;

        this.minX = this.minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        for (var i = 0; i < edges.Length; ++i)
        {
            ref readonly var e = ref edges[i];
            var bx = e.Ax + e.Dx;
            var by = e.Ay + e.Dy;
            this.minX = Math.Min(this.minX, (int)MathF.Floor(Math.Min(e.Ax, bx) * InvGridSize));
            this.minY = Math.Min(this.minY, (int)MathF.Floor(Math.Min(e.Ay, by) * InvGridSize));
            maxX = Math.Max(maxX, (int)MathF.Floor(Math.Max(e.Ax, bx) * InvGridSize));
            maxY = Math.Max(maxY, (int)MathF.Floor(Math.Max(e.Ay, by) * InvGridSize));
        }

        this.gridWidth = maxX - this.minX + 1;
        this.gridHeight = maxY - this.minY + 1;

        var cellCount = Math.Max(this.gridWidth * this.gridHeight, 0);
        var buckets = new List<int>[cellCount];
        for (var i = 0; i < cellCount; ++i)
            buckets[i] = [];

        for (var i = 0; i < edges.Length; ++i)
        {
            ref readonly var e = ref edges[i];
            var bx = e.Ax + e.Dx;
            var by = e.Ay + e.Dy;
            var x0 = (int)MathF.Floor(Math.Min(e.Ax, bx) * InvGridSize) - this.minX;
            var x1 = (int)MathF.Floor(Math.Max(e.Ax, bx) * InvGridSize) - this.minX;
            var y0 = (int)MathF.Floor(Math.Min(e.Ay, by) * InvGridSize) - this.minY;
            var y1 = (int)MathF.Floor(Math.Max(e.Ay, by) * InvGridSize) - this.minY;
            for (var y = y0; y <= y1; ++y)
                for (var x = x0; x <= x1; ++x)
                    buckets[(y * this.gridWidth) + x].Add(i);
        }

        this.grid = new int[cellCount][];
        for (var i = 0; i < cellCount; ++i)
            this.grid[i] = [.. buckets[i]];
    }

    /// <summary>Edge indices near the point, or empty when its cell holds none.</summary>
    public int[] Query(float px, float py)
    {
        var cellX = (int)MathF.Floor(px * InvGridSize) - this.minX;
        var cellY = (int)MathF.Floor(py * InvGridSize) - this.minY;
        return (uint)cellX >= (uint)this.gridWidth || (uint)cellY >= (uint)this.gridHeight
            ? []
            : this.grid[(cellY * this.gridWidth) + cellX];
    }
}
