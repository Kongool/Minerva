namespace Minerva;

/// <summary>
/// A walkable-cost map of the arena, built once per solve, answering "what does it actually cost to get
/// there" instead of "how far away is it in a straight line".
/// <para>Straight-line distance is a fine tiebreak on open ground and a lie anywhere else. Told to hide
/// behind a boulder, a nearest-cell solver picks the safe wedge with the smallest euclidean distance and
/// steers at it — through the boulder casting the wedge, because nothing in the scoring knows the rock is
/// there. Costing a route rather than a displacement is what makes "go round it" expressible at all.</para>
/// <para>Obstacles are impassable; imminent danger is passable at a penalty, because it usually has to be.
/// Line-of-sight mechanics forbid everything except a few shadows, so reaching the next shadow across open
/// ground is normal and correct — but only when there is no safer way round.</para>
/// </summary>
public sealed class RouteGrid
{
    /// <summary>What a step through ground that is about to become lethal costs, against 1 for safe ground.
    /// High enough to go the long way when one exists, finite so that crossing is still possible.</summary>
    public const float DangerStepPenalty = 6f;

    private const float Diagonal = 1.41421356f;

    private readonly WPos origin;   // world position of cell (0,0)
    private readonly float cell;
    private readonly int w, h;
    private readonly bool[] blocked;
    private readonly bool[] risky;
    private readonly float[] cost;    // penalised: what decides which way to go
    private readonly float[] length;  // plain yalms along that route: what the destination is scored on
    private readonly int[] came;

    public int Width => this.w;
    public int Height => this.h;

    public RouteGrid(AIHints hints, DateTime deadline, WPos player, float cellSize, float margin)
    {
        this.cell = cellSize;
        var reach = hints.Bounds.Radius;
        var center = hints.Center;
        this.origin = new WPos(center.X - reach, center.Z - reach);
        this.w = Math.Max(1, (int)MathF.Ceiling((reach * 2f) / cellSize) + 1);
        this.h = this.w;

        var n = this.w * this.h;
        this.blocked = new bool[n];
        this.risky = new bool[n];
        this.cost = new float[n];
        this.length = new float[n];
        this.came = new int[n];

        for (var z = 0; z < this.h; ++z)
        {
            for (var x = 0; x < this.w; ++x)
            {
                var i = (z * this.w) + x;
                var p = this.Center(x, z);
                this.cost[i] = float.MaxValue;
                this.length[i] = float.MaxValue;
                this.came[i] = -1;

                // out of bounds and inside something solid are both "cannot be here"; danger is not
                this.blocked[i] = !hints.Bounds.Contains(center, p) || hints.InObstacle(p);
                if (!this.blocked[i])
                    this.risky[i] = hints.InImminentDanger(p, deadline, margin);
            }
        }

        this.Flood(player);
    }

    public WPos Center(int x, int z) => new(this.origin.X + (x * this.cell), this.origin.Z + (z * this.cell));

    public bool TryCellOf(WPos p, out int x, out int z)
    {
        x = (int)MathF.Round((p.X - this.origin.X) / this.cell);
        z = (int)MathF.Round((p.Z - this.origin.Z) / this.cell);
        return x >= 0 && x < this.w && z >= 0 && z < this.h;
    }

    public bool Blocked(int x, int z) => this.blocked[(z * this.w) + x];

    public bool Risky(int x, int z) => this.risky[(z * this.w) + x];

    /// <summary>
    /// How far the character actually walks to reach this cell, in yalms. Infinity when it cannot.
    /// <para>The route length, not the penalised cost. The penalty exists to choose between routes; letting
    /// it into the destination score would make a cell that happens to sit past a telegraph look several
    /// times further away than it is, and every weight tuned against plain distance — goal zones, uptime,
    /// positionals — would be swamped by it.</para>
    /// </summary>
    public float CostAt(int x, int z)
    {
        var c = this.length[(z * this.w) + x];
        return c == float.MaxValue ? float.PositiveInfinity : c;
    }

    /// <summary>
    /// Where to steer right now to follow the route to <paramref name="target"/> — the furthest point along
    /// it still reachable in a straight line.
    /// <para>Handing the mover the final destination is what puts it back into the obstacle: it walks the
    /// chord, not the path. Handing it the very next cell makes it crawl one yalm at a time and jitter as
    /// the route re-solves. The furthest visible point is the one that both goes round the corner and keeps
    /// moving in a straight line for as long as the ground allows.</para>
    /// </summary>
    public WPos Waypoint(WPos player, WPos target)
    {
        if (!this.TryCellOf(target, out var tx, out var tz) || this.CostAt(tx, tz) == float.PositiveInfinity)
            return target;

        var path = new List<WPos>();
        var i = (tz * this.w) + tx;
        while (i >= 0 && path.Count <= this.w * this.h)
        {
            path.Add(this.Center(i % this.w, i / this.w));
            i = this.came[i];
        }

        // path runs destination -> player, so the first entry that can be walked to directly is the
        // furthest one along the route
        foreach (var p in path)
            if (this.Walkable(player, p))
                return p;

        return target;
    }

    /// <summary>Is the straight line between these two points clear of anything solid?</summary>
    public bool Walkable(WPos from, WPos to)
    {
        var d = to - from;
        var len = d.Length();
        var steps = (int)MathF.Ceiling(len / MathF.Max(this.cell * 0.5f, 0.1f));
        for (var s = 1; s <= steps; ++s)
        {
            var p = from + (d * ((float)s / steps));
            if (!this.TryCellOf(p, out var x, out var z) || this.blocked[(z * this.w) + x])
                return false;
        }

        return true;
    }

    /// <summary>
    /// Dijkstra outward from the player. A plain queue is not enough — a step into danger costs several
    /// times a step across clear ground, so cells do not settle in the order they are reached.
    /// </summary>
    private void Flood(WPos player)
    {
        if (!this.TryCellOf(player, out var px, out var pz))
            return;

        // standing inside something solid is recoverable: treat the cell we occupy as passable, or the
        // whole solve returns nothing and the character freezes exactly when it most needs to move
        var start = (pz * this.w) + px;
        this.blocked[start] = false;
        this.cost[start] = 0f;
        this.length[start] = 0f;

        var open = new PriorityQueue<int, float>();
        open.Enqueue(start, 0f);
        var settled = new bool[this.w * this.h];

        while (open.TryDequeue(out var i, out _))
        {
            if (settled[i])
                continue;
            settled[i] = true;

            var cx = i % this.w;
            var cz = i / this.w;
            var baseCost = this.cost[i];

            for (var dz = -1; dz <= 1; ++dz)
            {
                for (var dx = -1; dx <= 1; ++dx)
                {
                    if (dx == 0 && dz == 0)
                        continue;
                    var nx = cx + dx;
                    var nz = cz + dz;
                    if (nx < 0 || nx >= this.w || nz < 0 || nz >= this.h)
                        continue;
                    var j = (nz * this.w) + nx;
                    if (this.blocked[j] || settled[j])
                        continue;

                    // no cutting a diagonal between two solid cells -- that is a corner, not a gap
                    if (dx != 0 && dz != 0 && (this.blocked[(cz * this.w) + nx] || this.blocked[(nz * this.w) + cx]))
                        continue;

                    var step = (dx != 0 && dz != 0 ? Diagonal : 1f) * this.cell;
                    var next = baseCost + (this.risky[j] ? step * DangerStepPenalty : step);
                    if (next >= this.cost[j])
                        continue;
                    this.cost[j] = next;
                    this.length[j] = this.length[i] + step;
                    this.came[j] = i;
                    open.Enqueue(j, next);
                }
            }
        }
    }
}
