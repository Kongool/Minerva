namespace Minerva;

/// <summary>Result of a dodge solve.</summary>
public readonly record struct SafeSpot(bool NeedToMove, bool Found, WPos Target, WDir Direction)
{
    public static readonly SafeSpot Stay = new(false, true, default, default);
}

/// <summary>
/// The auto-dodge solver. If the player is (or is about to be) inside a danger zone, it rasterizes
/// the arena into a grid, discards every cell that is out of bounds or inside an imminent forbidden
/// zone, and returns the nearest surviving cell as the dodge target. Deliberately simple and
/// deterministic — nearest-safe rather than full navmesh — which is enough for telegraphed AOEs and
/// keeps it fully testable with no game.
/// </summary>
public static class ArenaPathfinder
{
    public static SafeSpot Solve(AIHints hints, DateTime now, float horizonSeconds = 3f, float cellSize = 1f, float safetyMargin = 0f)
    {
        var deadline = now.AddSeconds(horizonSeconds);
        var player = hints.PlayerPosition;

        // already safe for the look-ahead window: hold position (don't twitch)
        if (!hints.InImminentDanger(player, deadline))
            return SafeSpot.Stay;

        // prefer a target that keeps `safetyMargin` clearance from every zone; if the arena is too constrained
        // to find one, fall back to the nearest merely-safe cell so we still move out of the AOE.
        if (TryNearestSafe(hints, deadline, player, cellSize, safetyMargin, out var spot))
            return spot;
        if (safetyMargin > 0f && TryNearestSafe(hints, deadline, player, cellSize, 0f, out spot))
            return spot;

        return new SafeSpot(true, false, player, default); // whole reachable arena is dangerous
    }

    // a point inside a goal zone is worth this many yards² of extra travel — enough to nudge the dodge
    // toward objectives (uptime spots) without ever overriding safety or making the AI run across the arena
    private const float GoalBias = 9f;

    private static bool TryNearestSafe(AIHints hints, DateTime deadline, WPos player, float cellSize, float margin, out SafeSpot spot)
    {
        var center = hints.Center;
        var reach = hints.Bounds.Radius;
        var hasGoals = hints.GoalZones.Count > 0;
        var found = false;
        var best = player;
        var bestCost = float.MaxValue;

        for (var x = center.X - reach; x <= center.X + reach; x += cellSize)
        {
            for (var z = center.Z - reach; z <= center.Z + reach; z += cellSize)
            {
                var p = new WPos(x, z);
                if (!hints.Bounds.Contains(center, p))
                    continue;
                if (hints.InImminentDanger(p, deadline, margin))
                    continue;

                // cost is travel distance, reduced for cells inside goal zones (a gentle attractor)
                var cost = (p - player).LengthSq();
                if (hasGoals)
                    cost -= hints.GoalScore(p) * GoalBias;
                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = p;
                    found = true;
                }
            }
        }

        spot = found ? new SafeSpot(true, true, best, (best - player).Normalized()) : default;
        return found;
    }
}
