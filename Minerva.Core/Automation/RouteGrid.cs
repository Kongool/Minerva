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

    /// <summary>
    /// What a step costs through ground that will have fired before the character can get across it.
    /// Crossing a telegraph is a normal thing to do -- it fires in four seconds and the walk takes one --
    /// and <see cref="DangerStepPenalty"/> prices that. Crossing one that fires while you are still in it
    /// is not a detour, it is the hit: Pallmagia, 2026-09-06, the escape from a 30-yalm circle ran straight
    /// through a cone with 0.7s left on it, because six yalms of penalty was cheaper than going round.
    /// </summary>
    public const float LethalStepPenalty = 1000f;

    /// <summary>
    /// <see cref="LethalStepPenalty"/> once for every zone over the cell. A cell under two things that will both
    /// hit is worse than a cell under one. Priced flat, they were equal: escaping a breath cone that fires
    /// before the walk ends makes every cell of the escape lethal, and cutting through a standing haze inside
    /// that cone then cost nothing extra (Claret Dragon, 2026-09-14).
    /// </summary>
    private float LethalPenalty(int c) => LethalStepPenalty * Math.Max(1, System.Numerics.BitOperations.PopCount(this.zones[c]));

    /// <summary>Slack on the arrival estimate, in seconds: the route is a grid path and the character
    /// accelerates, so a cell is only counted as crossed in time with a little room to spare.</summary>
    private const float ArrivalSlack = 0.3f;

    private const float Diagonal = 1.41421356f;

    /// <summary>How far outside the arena, in yalms, the grid still reaches for a character standing there. Further
    /// out is someone arriving, not someone who stepped over the edge, and routing them would mean a huge grid.</summary>
    private const float MaxOutsideReach = 10f;

    private readonly WPos origin;   // world position of cell (0,0)
    private readonly float cell;
    private readonly int w, h;
    private readonly bool[] blocked;
    private readonly bool[] outside;  // out of bounds: blocked, but walkable back in by a character who is already out
    private readonly bool[] solid;    // inside an obstacle: blocked, always
    private readonly bool[] risky;
    private readonly float[] dangerIn;  // seconds until each cell is lethal; MaxValue when it never is
    private readonly ulong[] zones;     // which zones make each risky cell risky, as AIHints.ImminentZonesAt
    private readonly float speed;
    private readonly float[] cost;    // penalised: what decides which way to go
    private readonly float[] length;  // plain yalms along that route: what the destination is scored on
    private readonly int[] came;

    public int Width => this.w;
    public int Height => this.h;

    public RouteGrid(AIHints hints, DateTime deadline, WPos player, float cellSize, float margin)
        : this(hints, deadline, player, cellSize, margin, deadline, 0f)
    {
    }

    /// <param name="now">When the solve is happening, so a cell's danger can be compared with the time it
    /// takes to walk there.</param>
    /// <param name="moveSpeed">Yalms per second; zero disables the arrival check entirely.</param>
    public RouteGrid(AIHints hints, DateTime deadline, WPos player, float cellSize, float margin, DateTime now, float moveSpeed)
    {
        this.cell = cellSize;
        this.speed = moveSpeed;
        var center = hints.Center;
        // wide enough to hold a character who is a little way outside the arena, so there is a route back in
        var reach = MathF.Max(hints.Bounds.Radius, MathF.Min((player - center).Length() + cellSize, hints.Bounds.Radius + MaxOutsideReach));
        this.origin = new WPos(center.X - reach, center.Z - reach);
        this.w = Math.Max(1, (int)MathF.Ceiling((reach * 2f) / cellSize) + 1);
        this.h = this.w;

        var n = this.w * this.h;
        this.blocked = new bool[n];
        this.outside = new bool[n];
        this.solid = new bool[n];
        this.risky = new bool[n];
        this.dangerIn = new float[n];
        this.zones = new ulong[n];
        this.cost = new float[n];
        this.length = new float[n];
        this.came = new int[n];

        // The arena does not change between rungs or between frames, so the floor is rasterised once per
        // shape and kept on the bounds itself, the way BossmodReborn keeps its pathfinding map. It was
        // costing a full pass of point-in-polygon over the grid on each of the four grids a solve builds.
        hints.Bounds.CopyOutsideMask(center, this.origin, this.cell, this.w, this.h, this.outside);

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
                this.solid[i] = hints.InObstacle(p);
                this.blocked[i] = this.outside[i] || this.solid[i];
                this.dangerIn[i] = float.MaxValue;
                if (!this.solid[i])   // outside cells too: the way back in from outside the arena is priced like any other ground
                {
                    // One pass over the zones for all three answers; see AIHints.DangerAt.
                    var danger = hints.DangerAt(p, deadline, now, margin);
                    this.risky[i] = danger.Risky;
                    if (danger.Risky)
                    {
                        this.zones[i] = danger.Zones;
                        if (this.speed > 0f)
                            this.dangerIn[i] = danger.SecondsUntilDanger;
                    }
                }
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
    /// The whole route to <paramref name="target"/>, player-exclusive, with every redundant point removed.
    ///
    /// <para>A navmesh follower is handed a path and walks it, so it wants the whole thing: give it one point and it walks the straight chord to that point, straight through whatever
    /// the route was bending around. That is not the follower's fault -- it was told to go there.</para>
    ///
    /// <para>String-pulled: from each kept point, skip ahead to the furthest one still reachable in a straight
    /// line. What survives is the corners. The first corner is also where a mover we steer ourselves aims.</para>
    /// </summary>
    public List<WPos> Route(WPos player, WPos target)
    {
        if (!this.TryCellOf(target, out var tx, out var tz) || this.CostAt(tx, tz) == float.PositiveInfinity)
            return [target];

        // came[] runs destination -> player, so this is reversed to walk the other way
        var back = new List<int>();
        var i = (tz * this.w) + tx;
        while (i >= 0 && back.Count <= this.w * this.h)
        {
            back.Add(i);
            i = this.came[i];
        }
        back.Reverse();

        var route = new List<WPos>();
        var from = player;
        var fromCost = 0f;
        var fromLength = 0f;
        var at = 0;
        var crossed = new ulong[back.Count];
        while (at < back.Count)
        {
            // the zones the path itself passes through from the point we stand on to each cell ahead
            var sofar = at > 0 ? this.zones[back[at - 1]] : 0ul;
            for (var j = at; j < back.Count; ++j)
                crossed[j] = sofar |= this.zones[back[j]];

            var furthest = -1;
            for (var j = back.Count - 1; j >= at; --j)
            {
                if (this.Shortcut(from, fromCost, fromLength, back[j], crossed[j]))
                {
                    furthest = j;
                    break;
                }
            }
            var keep = furthest < 0 ? at : furthest;   // nothing ahead is a fair shortcut: keep the next cell
            var cellIndex = back[keep];
            from = this.Center(cellIndex % this.w, cellIndex / this.w);
            fromCost = this.cost[cellIndex];
            fromLength = this.length[cellIndex];
            route.Add(from);
            at = keep + 1;
        }

        if (route.Count == 0 || !route[^1].AlmostEqual(target, 0.01f))
            route.Add(target);
        return route;
    }

    /// <summary>
    /// May the route jump straight from <paramref name="from"/> to the path cell <paramref name="toIndex"/>?
    /// Only if nothing solid is in the way and the straight line costs no more, at the prices the search used,
    /// than the stretch of path it replaces.
    /// <para>The simplifier used to ask about walls alone. Danger is not a wall -- it is priced, so the search
    /// can still cross it when that is the only way -- and so a path that went carefully round a standing
    /// puddle was handed on as one straight step through it. Cursed Resurgence, 2026-09-13: twice in one pull,
    /// 2.3 and 4.9 yalms deep. On clean ground the straight line is always the cheaper, so routes shorten
    /// exactly as before; through danger it is not, so the detour survives.</para>
    /// <para>Price alone is not enough while escaping ground that fires before the walk ends: then every yalm of
    /// the path is lethal, and a line trading that ground for a puddle's costs the same. Claret Dragon,
    /// 2026-09-14, out of a breath cone and 1-2 yalms through a standing haze. So the line may also cross only
    /// danger the stretch it replaces crosses too (<paramref name="crossed"/>).</para>
    /// </summary>
    private bool Shortcut(WPos from, float fromCost, float fromLength, int toIndex, ulong crossed)
    {
        var to = this.Center(toIndex % this.w, toIndex / this.w);
        if (!this.Walkable(from, to))
            return false;

        var d = to - from;
        var len = d.Length();
        var steps = Math.Max(1, (int)MathF.Ceiling(len / MathF.Max(this.cell * 0.5f, 0.1f)));
        var sub = len / steps;
        var straight = 0f;
        var dearest = 1f;
        for (var s = 1; s <= steps; ++s)
        {
            var p = from + (d * ((float)s / steps));
            if (!this.TryCellOf(p, out var x, out var z))
                return false;
            var c = (z * this.w) + x;
            var penalty = 1f;
            if (this.risky[c])
            {
                if ((this.zones[c] & ~crossed) != 0)
                    return false;
                penalty = DangerStepPenalty;
                if (this.speed > 0f && this.dangerIn[c] <= ((fromLength + (sub * s)) / this.speed) + ArrivalSlack)
                    penalty = this.LethalPenalty(c);
            }
            straight += sub * penalty;
            dearest = MathF.Max(dearest, penalty);
        }

        // One cell of slack at the dearest price the line pays. Sampling a line and stepping a grid disagree by
        // about a cell, and on lethal ground a cell is a thousand yalms of cost: without this, escaping the
        // ground underfoot (where the path and the line cross the same danger) was chopped into cell-sized
        // steps. A real crossing of a puddle is many cells deep and still loses.
        return straight <= (this.cost[toIndex] - fromCost) + (this.cell * dearest);
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
                    if (settled[j])
                        continue;

                    // A character already outside the arena has to cross outside ground to get back in: from the start
                    // or another outside cell, an outside cell is walkable. Once inside, nothing leads back out. Lost on
                    // the Wind, 2026-09-14: 1.8 yalms into the death wall, every neighbour was out of bounds, nothing
                    // inside was reachable, and the dodge stood still in the wall.
                    var escaping = (i == start || this.outside[i]) && this.outside[j] && !this.solid[j];
                    if (this.blocked[j] && !escaping)
                        continue;

                    // no cutting a diagonal between two solid cells -- that is a corner, not a gap
                    var cornerA = (cz * this.w) + nx;
                    var cornerB = (nz * this.w) + cx;
                    if (dx != 0 && dz != 0 && (escaping ? this.solid[cornerA] || this.solid[cornerB] : this.blocked[cornerA] || this.blocked[cornerB]))
                        continue;

                    var step = (dx != 0 && dz != 0 ? Diagonal : 1f) * this.cell;
                    var penalty = 1f;
                    if (this.risky[j])
                    {
                        // Would it still be safe to be in that cell when we got there? The route so far is
                        // this.length[i] yalms, so arrival is that plus this step, divided by walk speed.
                        penalty = DangerStepPenalty;
                        if (this.speed > 0f && this.dangerIn[j] <= ((this.length[i] + step) / this.speed) + ArrivalSlack)
                            penalty = this.LethalPenalty(j);
                    }
                    var next = baseCost + (step * penalty);
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
