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
    public static SafeSpot Solve(AIHints hints, DateTime now, float horizonSeconds = 3f, float cellSize = 1f)
    {
        var deadline = now.AddSeconds(horizonSeconds);
        var player = hints.PlayerPosition;

        // already safe for the look-ahead window: hold position (don't twitch)
        if (!hints.InImminentDanger(player, deadline))
            return SafeSpot.Stay;

        var center = hints.Center;
        var reach = hints.Bounds.Radius;
        var found = false;
        var best = player;
        var bestDistSq = float.MaxValue;

        for (var x = center.X - reach; x <= center.X + reach; x += cellSize)
        {
            for (var z = center.Z - reach; z <= center.Z + reach; z += cellSize)
            {
                var p = new WPos(x, z);
                if (!hints.Bounds.Contains(center, p))
                    continue;
                if (hints.InImminentDanger(p, deadline))
                    continue;

                var d = (p - player).LengthSq();
                if (d < bestDistSq)
                {
                    bestDistSq = d;
                    best = p;
                    found = true;
                }
            }
        }

        if (!found)
            return new SafeSpot(true, false, player, default); // whole reachable arena is dangerous

        return new SafeSpot(true, true, best, (best - player).Normalized());
    }
}
