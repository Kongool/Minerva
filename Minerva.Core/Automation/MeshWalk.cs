using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace Minerva;

/// <summary>
/// The navmesh's way to a walk's destination, for walks that are not dodging anything: uptime, a positional, a spot the
/// module assigned. Those walks have no AOE to bend around, so the solve hands over a straight line, and nothing on
/// Minerva's side knows about walls: the steer walks the chord, and the floor probe calls the top of a rock floor.
/// Korha, Eureka Anemos 2026-11-17: two uptime walks from the south side of a rock (z3fa_t1_rck01, at 20.4,-411.8) ran
/// straight into its 88-degree south face, and StuckWalk stood them down and retried the same line. The mesh goes round:
/// east via (26.1,-414.2) to (25.9,-407.1), and to the rock's top only from the west, where it can be climbed.
/// <para>The mesh answers asynchronously, so the route is kept per destination and asked again only when the destination
/// moves (<see cref="ReplanYalms"/>), the character is no longer on it (<see cref="OffRouteYalms"/>), or a walk stuck
/// (<see cref="Replan"/>), and never more than once a <see cref="ReplanSeconds"/>. A route that runs along the chord
/// anyway is not used: the steer walks a straight line better than a follower walks waypoints.</para>
/// </summary>
public sealed class MeshWalk
{
    /// <summary>A walk shorter than this steers straight unless it has already stuck.</summary>
    public const float MinChordYalms = 2f;

    /// <summary>How far the destination may move before the route to it is asked for again.</summary>
    public const float ReplanYalms = 3f;

    /// <summary>How far from the route the character may be before it no longer leads anywhere useful.</summary>
    public const float OffRouteYalms = 3f;

    /// <summary>Never ask more often than this.</summary>
    public const double ReplanSeconds = 1d;

    /// <summary>A route whose corners all lie this close to the chord is the chord.</summary>
    public const float BendYalms = 0.75f;

    private Task<List<Vector3>>? pending;
    private WPos pendingFrom;
    private WPos pendingDest;
    private DateTime askedAt;
    private bool asked;

    private List<Vector3>? route; // empty when the mesh had no route to give
    private WPos routeFrom;
    private WPos routeDest;

    private bool forced; // a walk to this destination stuck: use the mesh however short or straight
    private bool askNow; // ask at once, cooldown or not

    /// <summary>The last <see cref="Route"/> handed back a mesh route.</summary>
    public bool Walking { get; private set; }

    /// <summary>
    /// A walk did not move the character: ask the mesh again from where it stands, at once, and take its route even
    /// for a short or straight walk. Once per destination; false when this destination was already re-planned, which
    /// means the mesh cannot help and the walk should stand down.
    /// </summary>
    public bool Replan()
    {
        if (this.forced)
            return false;
        this.forced = true;
        this.askNow = true;
        return true;
    }

    /// <summary>
    /// The mesh route still ahead of a character at <paramref name="from"/> walking to <paramref name="dest"/>, with its
    /// heights; null to steer the chord (the route is pending, the mesh had none, or it does not bend).
    /// <paramref name="pathfind"/> is the mesh's pathfinder, null when it cannot be asked.
    /// </summary>
    public IReadOnlyList<Vector3>? Route(Vector3 from, WPos dest, DateTime now, Func<Vector3, Vector3, Task<List<Vector3>>?> pathfind)
    {
        this.Walking = false;
        var at = new WPos(from.X, from.Z);

        if (this.pending is { IsCompleted: true } done)
        {
            this.route = done.IsCompletedSuccessfully && done.Result is { } r ? r : [];
            this.routeFrom = this.pendingFrom;
            this.routeDest = this.pendingDest;
            this.pending = null;
        }

        // a new destination is a new walk: the old one's stuck no longer says anything about it
        var known = this.pending != null ? this.pendingDest : this.route != null ? this.routeDest : (WPos?)null;
        if (known is { } k && !k.AlmostEqual(dest, ReplanYalms))
        {
            this.forced = false;
            this.askNow = false;
        }

        var off = 0f;
        var ahead = this.route is { Count: > 0 } rt ? Ahead(this.routeFrom, rt, at, out off) : null;
        var offRoute = ahead != null && off > OffRouteYalms;
        // no route yet asks at once; one the mesh could not give is asked again on the cooldown (a mesh still loading)
        var stale = this.route is not { Count: > 0 } || !this.routeDest.AlmostEqual(dest, ReplanYalms) || offRoute;
        if (this.pending == null && (stale || this.askNow) && (this.askNow || !this.asked || (now - this.askedAt).TotalSeconds >= ReplanSeconds))
        {
            this.askNow = false;
            this.asked = true;
            this.askedAt = now;
            this.pendingFrom = at;
            this.pendingDest = dest;
            this.pending = pathfind(from, new Vector3(dest.X, from.Y, dest.Z));
            if (this.pending == null)
            {
                this.route = [];
                this.routeFrom = at;
                this.routeDest = dest;
            }
        }

        if (ahead is not { Count: > 0 } || offRoute)
            return null;
        if (!this.forced && ((dest - at).Length() < MinChordYalms || Straight(at, dest, ahead)))
            return null;
        this.Walking = true;
        return ahead;
    }

    /// <summary>
    /// The mesh route is safe to walk now: clear of every zone on the clock of the walk, inside the arena, and off the
    /// remembered obstacles. Known ground (<see cref="AIHints.WalkableGround"/>) is left out: it stands in for a floor
    /// map Minerva does not have, and calls every cell nobody has stood on solid, which is most of any way round.
    /// </summary>
    public static bool Clear(AIHints hints, WPos player, IReadOnlyList<Vector3> route, DateTime now, float margin, float moveSpeed)
    {
        var flat = new List<WPos>(route.Count);
        foreach (var p in route)
            flat.Add(new WPos(p.X, p.Z));
        var ground = hints.WalkableGround;
        hints.WalkableGround = null;
        try
        {
            return ArenaPathfinder.RouteIsClear(hints, player, flat, now, margin, moveSpeed)
                && ArenaPathfinder.RouteStaysOnFloor(hints, player, flat);
        }
        finally
        {
            hints.WalkableGround = ground;
        }
    }

    // the waypoints from the end of the leg nearest the character (a leg from where the route was asked to its first
    // waypoint included); at a corner the later leg wins, so the corner reached is dropped
    private static List<Vector3> Ahead(WPos start, List<Vector3> route, WPos at, out float off)
    {
        var from = start;
        var leg = 0;
        off = float.MaxValue;
        for (var i = 0; i < route.Count; ++i)
        {
            var to = new WPos(route[i].X, route[i].Z);
            var d = DistanceToSegment(at, from, to);
            if (d <= off)
            {
                off = d;
                leg = i;
            }
            from = to;
        }
        return route.GetRange(leg, route.Count - leg);
    }

    private static bool Straight(WPos at, WPos dest, List<Vector3> ahead)
    {
        foreach (var p in ahead)
            if (DistanceToSegment(new WPos(p.X, p.Z), at, dest) > BendYalms)
                return false;
        return true;
    }

    private static float DistanceToSegment(WPos p, WPos a, WPos b)
    {
        var ab = b - a;
        var len = ab.LengthSq();
        var t = len > 0f ? Math.Clamp((p - a).Dot(ab) / len, 0f, 1f) : 0f;
        return (p - (a + (ab * t))).Length();
    }
}
