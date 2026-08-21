using System.Collections.Generic;

namespace Minerva;

/// <summary>
/// Triangulates a simple polygon that may be concave. The radar fills convex shapes with a triangle fan
/// from vertex 0, which is wrong the moment a shape has a hollow: a line-of-sight safe zone is built from
/// annular sectors — a boulder's shadow — and a fan across one paints straight over the hole it must leave
/// open. Ear clipping is O(n²) but only runs for custom shapes, and it is exact for any simple polygon.
/// </summary>
public static class EarClip
{
    /// <summary>
    /// Triangle indices into <paramref name="contour"/>. A simple polygon of n vertices always yields
    /// n-2 triangles; a self-intersecting or degenerate one yields fewer rather than looping forever.
    /// </summary>
    public static List<(int A, int B, int C)> Triangulate(IReadOnlyList<WPos> contour)
    {
        var n = contour.Count;
        var tris = new List<(int, int, int)>(n > 2 ? n - 2 : 0);
        if (n < 3)
            return tris;

        // the shoelace sign tells us the winding, so the convexity test below works for either direction
        var area2 = 0f;
        for (int i = 0, j = n - 1; i < n; j = i++)
            area2 += (contour[j].X * contour[i].Z) - (contour[i].X * contour[j].Z);
        var ccw = area2 > 0f;

        var live = new List<int>(n);
        for (var i = 0; i < n; ++i)
            live.Add(i);

        var guard = n * n; // a malformed contour must not spin: bail out and keep what we have
        while (live.Count > 3 && guard-- > 0)
        {
            var clipped = false;
            for (var i = 0; i < live.Count; ++i)
            {
                var a = live[(i + live.Count - 1) % live.Count];
                var b = live[i];
                var c = live[(i + 1) % live.Count];
                if (!IsEar(contour, live, a, b, c, ccw))
                    continue;
                tris.Add((a, b, c));
                live.RemoveAt(i);
                clipped = true;
                break;
            }

            if (!clipped)
                break;
        }

        if (live.Count == 3)
            tris.Add((live[0], live[1], live[2]));
        return tris;
    }

    private static bool IsEar(IReadOnlyList<WPos> p, List<int> live, int a, int b, int c, bool ccw)
    {
        var cross = Cross(p[a], p[b], p[c]);
        if (ccw ? cross <= 0f : cross >= 0f)
            return false; // reflex corner — cutting here would take ground outside the polygon

        for (var i = 0; i < live.Count; ++i)
        {
            var k = live[i];
            if (k != a && k != b && k != c && InTriangle(p[k], p[a], p[b], p[c]))
                return false; // another vertex is inside, so this is not a free-standing ear
        }

        return true;
    }

    private static float Cross(WPos a, WPos b, WPos c)
        => ((b.X - a.X) * (c.Z - a.Z)) - ((b.Z - a.Z) * (c.X - a.X));

    private static bool InTriangle(WPos p, WPos a, WPos b, WPos c)
    {
        var d1 = Cross(a, b, p);
        var d2 = Cross(b, c, p);
        var d3 = Cross(c, a, p);
        var neg = d1 < 0f || d2 < 0f || d3 < 0f;
        var pos = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(neg && pos);
    }
}
