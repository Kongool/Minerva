namespace Minerva;

/// <summary>
/// The radar's filled zones as triangle meshes: offsets from the arena centre, cut to the field, and kept from one
/// frame to the next while a zone stays where it is.
///
/// <para>The radar used to rebuild every zone every frame: contour, clip, triangulate. Measured against fourteen
/// recordings (2026-11-01), that was up to 1.75 ms a frame with Borgny's twenty clouds up, and most of it was
/// avoidable:</para>
/// <list type="bullet">
/// <item>Most zones never move. They are kept, keyed by the shape's values rather than the object, because
/// <c>Arena.ZoneCircle</c> and the swept-path forecasts make a new shape every frame. Loosefrox's six sand pits were
/// rebuilt sixty times a second for that alone.</item>
/// <item>Each shape is triangulated the way its outline is laid out: fans, and strips for the ring shapes. The general
/// ear-clipper spent about 700 us on one orbiting puddle (an arc capsule), and Elm Gigas has ten.</item>
/// <item>On a convex field (circle, square, rect, a one-ring polygon) a zone is cut with Sutherland-Hodgman, only
/// against the edges it can reach. Every zone on an Occult Crescent polygon arena used to go through Clipper2, at
/// 40-60 us each. Fields with holes or dents still do.</item>
/// </list>
/// <para>Game-free so the self-test can hold every fast path against Clipper2 (<see cref="BuildWithClipper"/>).</para>
/// </summary>
public sealed class ZoneMeshes
{
    /// <summary>An outline run, offsets from the arena centre. Open where the field cut the shape.</summary>
    public readonly record struct Outline(WDir[] Points, bool Closed);

    /// <summary>Triangles (three indices each into <see cref="Vertices"/>) and outlines, offsets from the arena
    /// centre.</summary>
    public sealed class Mesh(WDir[] vertices, int[] indices, Outline[] outlines)
    {
        public readonly WDir[] Vertices = vertices;
        public readonly int[] Indices = indices;
        public readonly Outline[] Outlines = outlines;
        internal int LastUsed;

        public float Area()
        {
            var sum = 0f;
            for (var i = 0; i + 2 < this.Indices.Length; i += 3)
            {
                var a = this.Vertices[this.Indices[i]];
                sum += MathF.Abs((this.Vertices[this.Indices[i + 1]] - a).Cross(this.Vertices[this.Indices[i + 2]] - a)) * 0.5f;
            }
            return sum;
        }
    }

    private enum Layout { General, Fan, RingStrip, ArcCapsuleStrip }

    private readonly record struct Key(object Shape, float X, float Z, float Rotation);

    private readonly PolygonClipper clipper = new();
    private readonly Dictionary<Key, Mesh> cache = [];
    private readonly List<Key> stale = [];
    private WPos center;
    private ArenaBounds? bounds;
    private bool clip = true;
    private int frame = int.MinValue;

    // the field, worked out once per bounds
    private PolygonClipper.Operand? fieldOperand;
    private bool fieldClassified;
    private WDir[]? fieldRing;
    private float fieldSign;
    private float fieldInradiusSq;

    // scratch
    private readonly List<WDir> scratchVerts = [];
    private readonly List<int> scratchIdx = [];
    private readonly List<int> nearEdges = [];
    private WDir[] polyA = new WDir[64];
    private WDir[] polyB = new WDir[64];

    /// <summary>Kept meshes, for diagnostics and tests.</summary>
    public int CachedCount => this.cache.Count;

    /// <summary>
    /// Start a frame. A different centre, bounds object or clip setting drops everything kept (bounds are treated as
    /// immutable, as the clipper's field operand always was: a module swaps in a new one). A new
    /// <paramref name="frameId"/> drops meshes not used in the previous frame; a window that begins twice in one
    /// frame (a hit-test pass, then the drawing) passes the same id both times.
    /// </summary>
    public void BeginFrame(int frameId, WPos center, ArenaBounds bounds, bool clip)
    {
        if (!ReferenceEquals(bounds, this.bounds) || !center.AlmostEqual(this.center, 1e-4f) || clip != this.clip)
        {
            this.cache.Clear();
            this.bounds = bounds;
            this.center = center;
            this.clip = clip;
            this.fieldOperand = null;
            this.fieldClassified = false;
            this.fieldRing = null;
        }

        if (frameId == this.frame)
            return;
        this.frame = frameId;
        if (this.cache.Count == 0)
            return;
        this.stale.Clear();
        foreach (var (key, mesh) in this.cache)
            if (mesh.LastUsed < frameId - 1)
                this.stale.Add(key);
        foreach (var key in this.stale)
            this.cache.Remove(key);
    }

    /// <summary>The zone's mesh, kept while the same shape stays at the same origin and rotation.</summary>
    public Mesh Get(AOEShape shape, WPos origin, Angle rotation)
    {
        if (ShapeKey(shape) is not { } value)
            return this.Build(shape, origin, rotation);
        return this.Lookup(new Key(value, origin.X, origin.Z, rotation.Rad), () => this.Build(shape, origin, rotation));
    }

    /// <summary>A filled triangle cut to the field, kept like a shape.</summary>
    public Mesh GetTriangle(WPos a, WPos b, WPos c)
        => this.Lookup(new Key((b.X, b.Z, c.X, c.Z), a.X, a.Z, 0f), () => this.BuildLoops([[a, b, c]], Layout.Fan, convex: true, splitRing: false));

    private Mesh Lookup(Key key, Func<Mesh> build)
    {
        ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(this.cache, key, out var exists);
        if (!exists || slot == null)
            slot = build();
        slot.LastUsed = this.frame;
        return slot;
    }

    /// <summary>
    /// The geometry that decides a shape's mesh, or null for one that is rebuilt every time. A custom shape can be
    /// rewritten in place (<c>ReplacePolygon</c>), and a type not listed here has fields this cannot see.
    /// </summary>
    private static object? ShapeKey(AOEShape shape) => shape switch
    {
        AOEShapeCircle c => (1, c.Radius),
        AOEShapeDonut d => (2, d.InnerRadius, d.OuterRadius),
        AOEShapeCone c => (3, c.Radius, c.HalfAngle.Rad, c.DirectionOffset.Rad),
        AOEShapeDonutSector d => (4, d.InnerRadius, d.OuterRadius, d.HalfAngle.Rad, d.DirectionOffset.Rad),
        AOEShapeArcCapsule a => (5, a.Radius, a.AngularLength.Rad, a.OrbitCenter.X, a.OrbitCenter.Z),
        AOEShapeRect r => (6, r.LenFront, r.HalfWidth, r.LenBack, r.DirectionOffset.Rad),
        AOEShapeCross x => (7, x.Length, x.HalfWidth, x.DirectionOffset.Rad),
        AOEShapeTriCone t => (8, t.SideLength, t.HalfAngle.Rad, t.DirectionOffset.Rad),
        AOEShapeCapsule c => (9, c.Radius, c.Length, c.DirectionOffset.Rad),
        _ => null,
    };

    /// <summary>The zone's mesh, built now and not kept.</summary>
    public Mesh Build(AOEShape shape, WPos origin, Angle rotation)
    {
        if (shape is AOEShapeCustom custom)
            return this.BuildCustom(custom);

        var loops = shape.Contours(origin, rotation);
        var (layout, convex) = shape switch
        {
            AOEShapeCircle or AOEShapeCapsule or AOEShapeRect => (Layout.Fan, true),
            // a sector of any width is star-shaped from its apex, the outline's first point
            AOEShapeCone c => (Layout.Fan, c.HalfAngle.Rad <= MathF.PI / 2f + 1e-4f),
            AOEShapeTriCone t => (Layout.Fan, t.HalfAngle.Rad <= MathF.PI / 2f + 1e-4f),
            AOEShapeDonut or AOEShapeDonutSector => (Layout.RingStrip, false),
            AOEShapeArcCapsule => (Layout.ArcCapsuleStrip, false),
            _ => (Layout.General, false),
        };
        return this.BuildLoops(loops, layout, convex, splitRing: shape is AOEShapeDonut);
    }

    /// <summary>
    /// The same zone through Clipper2 alone, as the radar drew every zone before 2026-11-01: simplified, or intersected
    /// with the field when any of it leaves. What the self-test holds the fast paths against.
    /// </summary>
    public Mesh BuildWithClipper(AOEShape shape, WPos origin, Angle rotation)
    {
        if (shape is AOEShapeCustom custom)
            return this.BuildCustom(custom);
        var loops = shape.Contours(origin, rotation);
        var subject = new PolygonClipper.Operand();
        foreach (var loop in loops)
            if (loop.Count >= 3)
                subject.AddContour(this.Offsets(loop));
        return this.FromOperand(subject, this.clip && this.Leaves(loops));
    }

    private Mesh BuildCustom(AOEShapeCustom custom)
    {
        var subject = new PolygonClipper.Operand();
        var delta = custom.PolygonOrigin - this.center;
        foreach (var part in custom.Polygon.Parts)
        {
            AddRing(part.Exterior);
            for (var h = 0; h < part.HoleStarts.Count; ++h)
                AddRing(part.Interior(h));
        }
        return this.FromOperand(subject, this.clip);

        void AddRing(ReadOnlySpan<WDir> ring)
        {
            if (ring.Length < 3)
                return;
            var offsets = new WDir[ring.Length];
            for (var i = 0; i < ring.Length; ++i)
                offsets[i] = ring[i] + delta;
            subject.AddContour(offsets);
        }
    }

    private Mesh BuildLoops(IReadOnlyList<IReadOnlyList<WPos>> loops, Layout layout, bool convex, bool splitRing)
    {
        var cut = this.clip && this.Leaves(loops);
        var field = cut ? this.ConvexField() : null;
        if (cut && field == null)
        {
            var subject = new PolygonClipper.Operand();
            foreach (var loop in loops)
                if (loop.Count >= 3)
                    subject.AddContour(this.Offsets(loop));
            return this.FromOperand(subject, true);
        }

        var verts = new List<WDir>();
        var idx = new List<int>();
        var outlines = new List<Outline>();
        foreach (var loop in loops)
        {
            if (loop.Count < 3)
                continue;
            var ring = this.Offsets(loop);
            WDir[][] rings = splitRing && ring.Length == RingLength ? [ring[..(RingLength / 2)], ring[(RingLength / 2)..]] : [ring];
            var near = field != null ? this.NearEdges(ring, field) : null;
            if (near == null || near.Count == 0)
            {
                foreach (var r in rings)
                    outlines.Add(new(r, true));
                Triangulate(ring, layout, verts.Count, idx);
                verts.AddRange(ring);
                continue;
            }

            foreach (var r in rings)
                this.ClipOutline(r, field!, near, outlines);
            if (convex)
            {
                this.ClipConvex(ring, field!, near, verts, idx);
                continue;
            }
            this.scratchVerts.Clear();
            this.scratchIdx.Clear();
            this.scratchVerts.AddRange(ring);
            Triangulate(ring, layout, 0, this.scratchIdx);
            this.ClipTriangles(field!, near, verts, idx);
        }
        return new Mesh([.. verts], [.. idx], [.. outlines]);
    }

    // --------------------------------------------------------------------------------------------- triangulation

    // AOEShape.ArcSegments is 40: a ring (donut, donut sector) is 41 points out and 41 back; an arc capsule adds a
    // 21-point cap at each end. Any other count is not the layout these strips assume, and gets the general path.
    private const int RingLength = 82;
    private const int ArcCapsuleLength = 124;

    private static void Triangulate(WDir[] ring, Layout layout, int b, List<int> idx)
    {
        switch (layout)
        {
            case Layout.Fan:
                for (var i = 1; i < ring.Length - 1; ++i)
                {
                    idx.Add(b);
                    idx.Add(b + i);
                    idx.Add(b + i + 1);
                }
                return;
            case Layout.RingStrip when ring.Length == RingLength:
                // outer arc 0..40, inner arc 41..81 running back
                Strip(b, 0, 41, 40, idx);
                return;
            case Layout.ArcCapsuleStrip when ring.Length == ArcCapsuleLength:
                // outer arc 0..40, end cap 41..61, inner arc 62..102 running back, start cap 103..123
                Strip(b, 0, 62, 40, idx);
                for (var j = 42; j < 61; ++j)
                {
                    idx.Add(b + 41);
                    idx.Add(b + j);
                    idx.Add(b + j + 1);
                }
                for (var j = 104; j < 123; ++j)
                {
                    idx.Add(b + 103);
                    idx.Add(b + j);
                    idx.Add(b + j + 1);
                }
                return;
            default:
                var pts = new WPos[ring.Length];
                for (var i = 0; i < ring.Length; ++i)
                    pts[i] = new WPos(ring[i].X, ring[i].Z);
                foreach (var (p, q, r) in EarClip.Triangulate(pts))
                {
                    idx.Add(b + p);
                    idx.Add(b + q);
                    idx.Add(b + r);
                }
                return;
        }

        // two arcs of segments + 1 points, the second running back: arc A's i-th point faces arc B's (segments - i)-th
        static void Strip(int b, int outer, int inner, int segments, List<int> idx)
        {
            for (var i = 0; i < segments; ++i)
            {
                var o0 = b + outer + i;
                var o1 = o0 + 1;
                var i0 = b + inner + (segments - i);
                var i1 = i0 - 1;
                idx.Add(o0);
                idx.Add(o1);
                idx.Add(i1);
                idx.Add(o0);
                idx.Add(i1);
                idx.Add(i0);
            }
        }
    }

    // --------------------------------------------------------------------------------------------- the field

    /// <summary>Whether any of the loops reaches outside the field.</summary>
    private bool Leaves(IReadOnlyList<IReadOnlyList<WPos>> loops)
    {
        var field = this.ConvexField();
        if (field != null)
        {
            foreach (var loop in loops)
                foreach (var p in loop)
                    if (!this.InField(p - this.center, field))
                        return true;
            return false;
        }

        // a donut, polygon or custom field can be crossed by an edge whose ends are both inside, so only the convex
        // bounds with holes or pillars trust the vertex test (as the radar always did)
        if (this.bounds is not (ArenaBoundsCircle or ArenaBoundsSquare or ArenaBoundsRect))
            return true;
        foreach (var loop in loops)
            foreach (var p in loop)
                if (!this.bounds.Contains(this.center, p))
                    return true;
        return false;
    }

    /// <summary>The field as one convex ring, offsets from the centre, or null (holes, pillars, dents): those cut
    /// through Clipper2.</summary>
    private WDir[]? ConvexField()
    {
        if (this.fieldClassified)
            return this.fieldRing;
        this.fieldClassified = true;
        this.fieldRing = null;
        if (this.bounds == null)
            return null;

        WDir[]? raw = null;
        if (this.bounds is ArenaBoundsCustom custom)
        {
            if (custom.Center.AlmostEqual(this.center, 0.1f) && custom.Polygon.Parts.Count == 1 && custom.Polygon.Parts[0].HoleStarts.Count == 0)
                raw = custom.Polygon.Parts[0].Exterior.ToArray();
        }
        else if (this.bounds.InnerContour(this.center) == null && !this.bounds.Obstacles(this.center).Any())
        {
            raw = this.Offsets(this.bounds.Contour(this.center));
        }
        if (raw == null)
            return null;

        // a circle's contour repeats its first point at the end
        var ring = new List<WDir>(raw.Length);
        foreach (var v in raw)
            if (ring.Count == 0 || (v - ring[^1]).LengthSq() > 1e-8f)
                ring.Add(v);
        if (ring.Count > 1 && (ring[0] - ring[^1]).LengthSq() <= 1e-8f)
            ring.RemoveAt(ring.Count - 1);
        if (ring.Count < 3)
            return null;

        var sign = 0f;
        var inradius = float.MaxValue;
        for (var i = 0; i < ring.Count; ++i)
        {
            var a = ring[i];
            var b = ring[(i + 1) % ring.Count];
            var c = ring[(i + 2) % ring.Count];
            var turn = (b - a).Cross(c - b);
            if (MathF.Abs(turn) > 1e-6f)
            {
                var s = MathF.Sign(turn);
                if (sign != 0f && s != sign)
                    return null;
                sign = s;
            }
            var e = b - a;
            inradius = MathF.Min(inradius, MathF.Abs(e.Cross(-a)) / e.Length());
        }
        if (sign == 0f)
            return null;
        // the centre must be inside for the inradius shortcut to mean anything
        for (var i = 0; i < ring.Count; ++i)
            if (sign * (ring[(i + 1) % ring.Count] - ring[i]).Cross(-ring[i]) < 0f)
                return null;

        this.fieldSign = sign;
        this.fieldInradiusSq = inradius * inradius;
        this.fieldRing = [.. ring];
        return this.fieldRing;
    }

    private bool InField(WDir p, WDir[] field)
    {
        if (p.LengthSq() <= this.fieldInradiusSq)
            return true;
        for (var i = 0; i < field.Length; ++i)
        {
            var a = field[i];
            if (this.fieldSign * (field[(i + 1) % field.Length] - a).Cross(p - a) < 0f)
                return false;
        }
        return true;
    }

    /// <summary>The field edges a ring's bounding circle reaches: the only ones that can cut it.</summary>
    private List<int> NearEdges(WDir[] ring, WDir[] field)
    {
        this.nearEdges.Clear();
        var mid = default(WDir);
        foreach (var v in ring)
            mid += v;
        mid /= ring.Length;
        var reach = 0f;
        foreach (var v in ring)
            reach = MathF.Max(reach, (v - mid).LengthSq());
        reach = MathF.Sqrt(reach);
        for (var e = 0; e < field.Length; ++e)
        {
            var a = field[e];
            var edge = field[(e + 1) % field.Length] - a;
            if (this.fieldSign * edge.Cross(mid - a) / edge.Length() < reach)
                this.nearEdges.Add(e);
        }
        return this.nearEdges;
    }

    private bool InsideNear(WDir p, WDir[] field, List<int> near)
    {
        foreach (var e in near)
        {
            var a = field[e];
            if (this.fieldSign * (field[(e + 1) % field.Length] - a).Cross(p - a) < 0f)
                return false;
        }
        return true;
    }

    /// <summary>Sutherland-Hodgman: <paramref name="n"/> points of polyA against the near edges. Returns the count left
    /// in polyA.</summary>
    private int CutPolygon(int n, WDir[] field, List<int> near)
    {
        var src = this.polyA;
        var dst = this.polyB;
        for (var k = 0; k < near.Count && n > 0; ++k)
        {
            var e = near[k];
            var a = field[e];
            var edge = field[(e + 1) % field.Length] - a;
            var m = 0;
            for (var i = 0; i < n; ++i)
            {
                var cur = src[i];
                var next = src[(i + 1) % n];
                var dc = this.fieldSign * edge.Cross(cur - a);
                var dn = this.fieldSign * edge.Cross(next - a);
                if (dc >= 0f)
                    dst[m++] = cur;
                if ((dc >= 0f) != (dn >= 0f))
                    dst[m++] = cur + ((next - cur) * (dc / (dc - dn)));
            }
            (src, dst) = (dst, src);
            n = m;
        }
        if (!ReferenceEquals(src, this.polyA))
            Array.Copy(src, this.polyA, n);
        return n;
    }

    private void EnsurePoly(int count)
    {
        if (this.polyA.Length >= count)
            return;
        this.polyA = new WDir[count * 2];
        this.polyB = new WDir[count * 2];
    }

    /// <summary>A convex ring against a convex field: one cut, then a fan.</summary>
    private void ClipConvex(WDir[] ring, WDir[] field, List<int> near, List<WDir> verts, List<int> idx)
    {
        this.EnsurePoly(ring.Length + near.Count + 4);
        ring.CopyTo(this.polyA, 0);
        var n = this.CutPolygon(ring.Length, field, near);
        if (n < 3)
            return;
        var b = verts.Count;
        for (var i = 0; i < n; ++i)
            verts.Add(this.polyA[i]);
        for (var i = 1; i < n - 1; ++i)
        {
            idx.Add(b);
            idx.Add(b + i);
            idx.Add(b + i + 1);
        }
    }

    /// <summary>Any shape against a convex field: each triangle of scratchVerts/scratchIdx cut on its own.</summary>
    private void ClipTriangles(WDir[] field, List<int> near, List<WDir> verts, List<int> idx)
    {
        this.EnsurePoly(near.Count + 8);
        for (var t = 0; t + 2 < this.scratchIdx.Count; t += 3)
        {
            var a = this.scratchVerts[this.scratchIdx[t]];
            var b = this.scratchVerts[this.scratchIdx[t + 1]];
            var c = this.scratchVerts[this.scratchIdx[t + 2]];
            if (this.InsideNear(a, field, near) && this.InsideNear(b, field, near) && this.InsideNear(c, field, near))
            {
                idx.Add(verts.Count);
                verts.Add(a);
                idx.Add(verts.Count);
                verts.Add(b);
                idx.Add(verts.Count);
                verts.Add(c);
                continue;
            }
            this.polyA[0] = a;
            this.polyA[1] = b;
            this.polyA[2] = c;
            var n = this.CutPolygon(3, field, near);
            if (n < 3)
                continue;
            var b0 = verts.Count;
            for (var i = 0; i < n; ++i)
                verts.Add(this.polyA[i]);
            for (var i = 1; i < n - 1; ++i)
            {
                idx.Add(b0);
                idx.Add(b0 + i);
                idx.Add(b0 + i + 1);
            }
        }
    }

    /// <summary>Cyrus-Beck: the parts of a closed outline inside the field, as open runs. Closed if nothing was cut.
    /// The field's own edge is drawn by the boundary, so the cut is not stroked along it.</summary>
    private void ClipOutline(WDir[] ring, WDir[] field, List<int> near, List<Outline> outlines)
    {
        var run = new List<WDir>();
        var cut = false;
        for (var i = 0; i < ring.Length; ++i)
        {
            var a = ring[i];
            var d = ring[(i + 1) % ring.Length] - a;
            float t0 = 0f, t1 = 1f;
            var keep = true;
            for (var k = 0; k < near.Count && keep; ++k)
            {
                var e = near[k];
                var p = field[e];
                var edge = field[(e + 1) % field.Length] - p;
                var num = this.fieldSign * edge.Cross(a - p);
                var den = this.fieldSign * edge.Cross(d);
                if (MathF.Abs(den) < 1e-9f)
                {
                    keep = num >= 0f;
                    continue;
                }
                var t = -num / den;
                if (den > 0f)
                    t0 = MathF.Max(t0, t);
                else
                    t1 = MathF.Min(t1, t);
                keep = t0 <= t1;
            }
            if (!keep)
            {
                cut = true;
                Flush();
                continue;
            }
            if (t0 > 0f)
            {
                cut = true;
                Flush();
            }
            if (run.Count == 0)
                run.Add(a + (d * t0));
            run.Add(a + (d * t1));
            if (t1 < 1f)
            {
                cut = true;
                Flush();
            }
        }
        if (!cut)
        {
            outlines.Add(new(ring, true));
            return;
        }
        Flush();

        void Flush()
        {
            if (run.Count >= 2)
                outlines.Add(new([.. run], false));
            run.Clear();
        }
    }

    // --------------------------------------------------------------------------------------------- Clipper2

    private Mesh FromOperand(PolygonClipper.Operand subject, bool cut)
    {
        var result = cut ? this.clipper.Intersect(subject, this.FieldOperand()) : this.clipper.Simplify(subject);
        var verts = new List<WDir>();
        var idx = new List<int>();
        foreach (var t in result.Triangulate())
        {
            idx.Add(verts.Count);
            verts.Add(t.A);
            idx.Add(verts.Count);
            verts.Add(t.B);
            idx.Add(verts.Count);
            verts.Add(t.C);
        }
        var outlines = new List<Outline>();
        foreach (var part in result.Parts)
        {
            outlines.Add(new(part.Exterior.ToArray(), true));
            for (var h = 0; h < part.HoleStarts.Count; ++h)
                outlines.Add(new(part.Interior(h).ToArray(), true));
        }
        return new Mesh([.. verts], [.. idx], [.. outlines]);
    }

    /// <summary>The field for Clipper2: the outer contour minus the donut hole and any pillars, built with the clipper
    /// so hole orientation is its problem.</summary>
    private PolygonClipper.Operand FieldOperand()
    {
        if (this.fieldOperand != null)
            return this.fieldOperand;
        var bounds = this.bounds ?? new ArenaBoundsCircle(20f);
        RelSimplifiedComplexPolygon field;
        if (bounds is ArenaBoundsCustom custom && custom.Center.AlmostEqual(this.center, 0.1f))
        {
            field = custom.Polygon;
        }
        else
        {
            var outer = new PolygonClipper.Operand(this.Offsets(bounds.Contour(this.center)));
            var cutouts = new PolygonClipper.Operand();
            var any = false;
            if (bounds.InnerContour(this.center) is { } inner)
            {
                cutouts.AddContour(this.Offsets(inner));
                any = true;
            }
            foreach (var obstacle in bounds.Obstacles(this.center))
            {
                cutouts.AddContour(this.Offsets(obstacle));
                any = true;
            }
            field = any ? this.clipper.Difference(outer, cutouts) : this.clipper.Simplify(outer);
        }
        return this.fieldOperand = new PolygonClipper.Operand(field);
    }

    private WDir[] Offsets(IReadOnlyList<WPos> contour)
    {
        var result = new WDir[contour.Count];
        for (var i = 0; i < contour.Count; ++i)
            result[i] = contour[i] - this.center;
        return result;
    }
}
