using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Minerva;

namespace Minerva.Radar;

/// <summary>
/// ImGui-backed <see cref="Arena"/>: maps world (XZ) coordinates to a square canvas (north-up) and
/// draws with the window draw list. Renders each shape type the best way the binding allows —
/// native filled circles, triangulated donut rings, and triangle-fan fills for convex shapes —
/// since this ImGui build has no concave-poly fill.
/// </summary>
public sealed class ImGuiArena : Arena
{
    private ImDrawListPtr draw;
    private Vector2 screenCenter;
    private Vector2 canvasTopLeft;
    private Vector2 canvasSize;
    private float scale = 1f;

    /// <summary>
    /// Cut AOE fills off at the arena edge by geometry rather than by painting over them. On by default;
    /// the radar turns it off when the user does. Painting over stops working the moment the window is
    /// made transparent -- a translucent mask dims the overspill instead of hiding it -- and a FATE's
    /// 50-yalm cone on a 25-yalm ring showed exactly that.
    /// </summary>
    public bool ClipZones = true;

    private readonly PolygonClipper clipper = new();
    private ArenaBounds? clipBoundsFor;
    private PolygonClipper.Operand? clipBounds;

    /// <summary>
    /// Screen-space rotation (radians) applied about the arena centre. 0 = north-up (default). Set to
    /// <c>player.Rotation.Rad + π</c> to make the local player's facing point up ("rotate with character").
    /// </summary>
    public float Rotation;

    /// <summary>Set the transform for this frame from a canvas rectangle.</summary>
    private bool clipPushed;

    /// <summary>
    /// Start a drawing pass over the given canvas.
    /// <para><paramref name="clip"/> confines everything drawn until <see cref="End"/> to the canvas
    /// rectangle. Without it a shape larger than the arena paints over whatever else the window holds:
    /// Pallmagia's Dark II is a 100x50 rect on a 20y arena, so at scale it covers the canvas and keeps
    /// going, straight over the toolbar above it. ImGui clips to the *window*, and the canvas is only part
    /// of one.</para>
    /// <para>Pass <c>false</c> for a pass that only reads coordinates (hit-testing a click), which needs no
    /// clip and would otherwise have to remember to balance it.</para>
    /// </summary>
    public void Begin(Vector2 canvasTopLeft, Vector2 canvasSize, float margin = 14f, bool clip = true)
    {
        this.End(); // a caller that missed its End must not leave the clip stack unbalanced
        this.draw = ImGui.GetWindowDrawList();
        this.screenCenter = canvasTopLeft + canvasSize * 0.5f;
        this.canvasTopLeft = canvasTopLeft;
        this.canvasSize = canvasSize;
        var half = MathF.Min(canvasSize.X, canvasSize.Y) * 0.5f - margin;
        this.scale = this.Bounds.Radius > 0f ? half / this.Bounds.Radius : 1f;
        if (clip)
        {
            this.draw.PushClipRect(canvasTopLeft, canvasTopLeft + canvasSize, true);
            this.clipPushed = true;
        }
    }

    /// <summary>Finish the pass and release the canvas clip. Safe to call when none is held.</summary>
    public void End()
    {
        if (!this.clipPushed)
            return;
        this.clipPushed = false;
        this.draw.PopClipRect();
    }

    private Vector2 W2S(WPos p)
    {
        var o = p - this.Center;
        var x = o.X;
        var z = o.Z;
        if (this.Rotation != 0f)
        {
            var (sin, cos) = MathF.SinCos(this.Rotation);
            (x, z) = (x * cos - z * sin, x * sin + z * cos);
        }
        return this.screenCenter + new Vector2(x, z) * this.scale; // +Z (south) => +Y (down)
    }

    /// <summary>Inverse transform: screen pixel back to a world position (for click-to-place, debug).</summary>
    public WPos ScreenToWorld(Vector2 screen)
    {
        var o = (screen - this.screenCenter) / this.scale;
        var x = o.X;
        var z = o.Y;
        if (this.Rotation != 0f)
        {
            var (sin, cos) = MathF.SinCos(this.Rotation);
            (x, z) = (x * cos + z * sin, -x * sin + z * cos); // transpose = inverse rotation
        }
        return new WPos(this.Center.X + x, this.Center.Z + z);
    }

    public override void ZoneShape(AOEShape shape, WPos origin, Angle rotation, uint color)
    {
        // A caller that named no colour means danger, not "invisible with a black ring round it". Both
        // AOEShape.Draw and Arena.ZoneCircle default the argument to zero, and zero here painted a
        // transparent fill and then an outline forced to full alpha -- (0 & 0x00FFFFFF) | (255 << 24) is
        // opaque black. Tiny Terror, 2026-09-06: the first flare drew as a hollow black circle.
        if (color == 0u)
            color = Colors.AOE;

        if (this.ClipZones)
        {
            var loops = shape.Contours(origin, rotation);
            if (this.NeedsClip(loops))
            {
                this.FillClipped(loops, color);
                return;
            }
        }

        switch (shape)
        {
            case AOEShapeCircle c:
                this.draw.AddCircleFilled(this.W2S(origin), c.Radius * this.scale, color, 48);
                break;
            case AOEShapeDonut d:
                this.FillDonut(origin, d.InnerRadius, d.OuterRadius, color);
                break;
            case AOEShapeCross x:
                // decompose into two convex rectangles (arms)
                this.FillConvex(RectPts(origin, rotation, x.Length, x.Length, x.HalfWidth), color);
                this.FillConvex(RectPts(origin, rotation + new Angle(MathF.PI / 2f), x.Length, x.Length, x.HalfWidth), color);
                break;
            default: // cone, rect, and boolean combinations — one fill per loop, any of them possibly concave
                foreach (var loop in shape.Contours(origin, rotation))
                    this.FillPolygon(loop, color);
                break;
        }
        this.OutlineShape(shape, origin, rotation, WithAlpha(color, 255), 1.5f);
    }

    public override void OutlineShape(AOEShape shape, WPos origin, Angle rotation, uint color, float thickness = 1f)
    {
        if (color == 0u)
            color = Colors.Danger;

        if (shape is AOEShapeCircle c)
        {
            this.draw.AddCircle(this.W2S(origin), c.Radius * this.scale, color, 48, thickness);
            return;
        }
        foreach (var loop in shape.Contours(origin, rotation))
            this.Polyline(loop, color, thickness, closed: true);
    }

    public override void AddCircle(WPos center, float radius, uint color, float thickness = 1f)
        => this.draw.AddCircle(this.W2S(center), radius * this.scale, color, 48, thickness);

    public override void AddCircleFilled(WPos center, float radius, uint color)
        => this.draw.AddCircleFilled(this.W2S(center), radius * this.scale, color, 48);

    public override Vector2 WorldPositionToScreenPosition(WPos p) => this.W2S(p);

    /// <summary>Text centred on a world position, with an optional outline for legibility over the arena.</summary>
    public override void TextWorld(WPos center, string text, uint color, float fontSize = 17f, uint outlineColor = 0u, float outlineWidth = 0f)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var size = ImGui.CalcTextSize(text);
        var at = this.W2S(center) - (size * 0.5f);

        // the outline is four offset copies rather than a real stroke: ImGui has no stroked text, and a
        // label sitting on a bright AOE is unreadable without one
        if (outlineWidth > 0f && outlineColor != 0u)
            for (var dx = -1; dx <= 1; dx += 2)
                for (var dy = -1; dy <= 1; dy += 2)
                    this.draw.AddText(at + new Vector2(dx * outlineWidth, dy * outlineWidth), outlineColor, text);

        this.draw.AddText(at, color, text);
    }

    /// <summary>An icon glyph centred on a world position, drawn in Dalamud's icon font.</summary>
    public override void IconWorld(WPos center, char glyph, uint color, float fontSize = 17f)
    {
        var s = glyph.ToString();
        using var font = Service.PluginInterface.UiBuilder.IconFontHandle?.Push();
        var size = ImGui.CalcTextSize(s);
        this.draw.AddText(this.W2S(center) - (size * 0.5f), color, s);
    }

    public override void AddLine(WPos a, WPos b, uint color, float thickness = 1f)
        => this.draw.AddLine(this.W2S(a), this.W2S(b), color, thickness);

    public override void AddTriangleFilled(WPos a, WPos b, WPos c, uint color)
    {
        if (this.ClipZones)
        {
            IReadOnlyList<IReadOnlyList<WPos>> loops = [new[] { a, b, c }];
            if (this.NeedsClip(loops))
            {
                this.FillClipped(loops, color);
                return;
            }
        }

        this.draw.AddTriangleFilled(this.W2S(a), this.W2S(b), this.W2S(c), color);
    }

    public override void ActorMarker(WPos pos, Angle rotation, float radius, uint color)
    {
        var r = MathF.Max(radius, 0.5f);
        // the ring is the real hitbox, but the facing arrow is only a readability cue — sizing it off the
        // hitbox too makes a large boss a screen-filling triangle, so cap it and let the ring carry the size
        var ar = MathF.Min(r, 1.5f);
        var fwd = rotation.ToDirection();
        var side = fwd.OrthoL();
        var tip = this.W2S(pos + fwd * ar);
        var bl = this.W2S(pos - fwd * ar * 0.6f + side * ar * 0.7f);
        var br = this.W2S(pos - fwd * ar * 0.6f - side * ar * 0.7f);
        this.draw.AddTriangleFilled(tip, bl, br, color);
        this.draw.AddCircle(this.W2S(pos), r * this.scale, WithAlpha(color, 160), 24, 1.5f);
    }

    /// <summary>
    /// An eye, drawn in screen space so it stays upright.
    /// <para>Built from world coordinates it would tumble with the radar whenever the view is camera-
    /// aligned, and an upside-down eye is just a shape. Its size is also clamped in pixels rather than
    /// yalms: the marker has to stay readable at any zoom, unlike an AOE, whose size <i>is</i> the
    /// information.</para>
    /// </summary>
    public override void Eye(WPos pos, float radius, uint color)
    {
        const float minHalfWidth = 7f;   // pixels; below this the lids merge into a blob
        const float maxHalfWidth = 16f;
        var c = this.W2S(pos);
        var w = Math.Clamp(radius * this.scale, minHalfWidth, maxHalfWidth);
        var h = w * 0.62f;

        // two parabolic lids meeting at the corners: cheap, and reads as an eye at marker size
        const int segments = 10;
        for (var side = -1; side <= 1; side += 2)
        {
            var prev = new Vector2(c.X - w, c.Y);
            for (var i = 1; i <= segments; ++i)
            {
                var tt = -1f + (2f * i / segments);
                var next = new Vector2(c.X + (tt * w), c.Y + (side * h * (1f - (tt * tt))));
                this.draw.AddLine(prev, next, color, 1.6f);
                prev = next;
            }
        }

        this.draw.AddCircleFilled(c, h * 0.52f, color, 12);           // pupil
        this.draw.AddCircle(c, h * 0.52f, WithAlpha(color, 90), 12, 1f);
    }

    public override void DrawBoundary()
    {
        this.Polyline(this.Bounds.Contour(this.Center), Colors.Border, 2f, closed: true);
        var inner = this.Bounds.InnerContour(this.Center);
        if (inner != null)
            this.Polyline(inner, Colors.Border, 2f, closed: true); // donut hole
        foreach (var obstacle in this.Bounds.Obstacles(this.Center))
            this.Polyline(obstacle, Colors.Border, 2f, closed: true); // boulders/pillars cut out of the field
    }

    // world directions, in Minerva's (X = east, Z = south) frame
    private static readonly (string Label, WDir Dir)[] Cardinals =
    [
        ("N", new WDir(0f, -1f)),
        ("E", new WDir(1f, 0f)),
        ("S", new WDir(0f, 1f)),
        ("W", new WDir(-1f, 0f)),
    ];

    /// <summary>
    /// Cardinal letters just outside the boundary. They go through <see cref="W2S"/> like everything else,
    /// so they follow the radar's rotation rather than being painted at fixed screen corners — which is
    /// what keeps a camera-aligned radar readable instead of disorienting.
    /// </summary>
    public void DrawCompass()
    {
        var radius = this.Bounds.Radius;
        if (radius <= 0f)
            return;

        foreach (var (label, dir) in Cardinals)
        {
            var at = this.W2S(this.Center + dir * radius);
            var outward = at - this.screenCenter;
            var len = outward.Length();
            if (len > 0.01f)
                at += outward / len * 8f; // clear the boundary stroke; Begin() reserves the margin for this
            this.draw.AddText(at - ImGui.CalcTextSize(label) * 0.5f, Colors.Border, label);
        }
    }

    /// <summary>
    /// Paint over everything drawn outside the arena boundary with <paramref name="background"/>, so
    /// AOE fills that extend past the arena are cut off at the edge (we have no polygon-clipping, so
    /// this masks rather than clips). Works for any arena that is star-shaped from its centre — every
    /// shape we use (circle, square, rect, donut, convex polygon): each boundary edge is extruded
    /// radially outward far enough to cover the canvas, tiling the whole exterior with no corner gaps.
    /// For a donut it also fills the inner hole. Call after module content, then redraw the boundary.
    /// </summary>
    /// <summary>
    /// Hide everything drawn beyond the arena edge by painting over it with <paramref name="background"/>.
    /// <para>The colour is used exactly as given, alpha included. A translucent one dims the overspill
    /// rather than erasing it — painting over is all this can do, so partial opacity means partial hiding.
    /// </para>
    /// </summary>
    public void ClipOutsideArena(uint background)
    {
        const float k = 4f; // radial blow-up factor; boundary sits at ~half-canvas so 4x always clears the corners
        // The mask is emitted after any widgets the host window already submitted, so without a clip rect it
        // paints straight over them — a playback toolbar disappears under the very mask meant to tidy the
        // field. Confine it to the drawing canvas; the blow-up can then be as generous as it likes.
        this.draw.PushClipRect(this.canvasTopLeft, this.canvasTopLeft + this.canvasSize, true);
        var aa = this.SuspendAntiAliasedFill();
        var contour = this.Bounds.Contour(this.Center);
        if (contour.Count >= 3)
        {
            for (var i = 0; i < contour.Count; ++i)
            {
                var a = this.W2S(contour[i]);
                var b = this.W2S(contour[(i + 1) % contour.Count]);
                var oa = this.screenCenter + (a - this.screenCenter) * k;
                var ob = this.screenCenter + (b - this.screenCenter) * k;
                this.draw.AddTriangleFilled(a, b, ob, background);
                this.draw.AddTriangleFilled(a, ob, oa, background);
            }
        }

        var inner = this.Bounds.InnerContour(this.Center);
        if (inner != null)
            this.FillConvex(inner, background); // mask the donut hole too
        foreach (var obstacle in this.Bounds.Obstacles(this.Center))
            this.FillConvex(obstacle, background); // and each interior obstacle
        this.draw.Flags = aa;
        this.draw.PopClipRect();
    }

    // --- helpers ---

    /// <summary>
    /// Turns anti-aliased fill off for a run of triangles that tile one surface, returning the previous
    /// flags to restore. ImGui's AA fill insets each triangle by half a pixel and feathers the edge, so
    /// two triangles sharing an edge never reach full opacity along it and whatever is underneath bleeds
    /// through as a hairline. Fanning a polygon or extruding a contour therefore paints itself with seams:
    /// on the arena mask that reads as a sunburst of spokes, and on an AOE fill as a crease across it.
    /// </summary>
    private ImDrawListFlags SuspendAntiAliasedFill()
    {
        var saved = this.draw.Flags;
        this.draw.Flags = saved & ~ImDrawListFlags.AntiAliasedFill;
        return saved;
    }

    /// <summary>
    /// Fill a closed loop that may be concave. A boulder's shadow is an annular sector, and the convex fan
    /// used elsewhere would paint straight across its hollow — turning "stand behind the rock" into a solid
    /// wedge that covers the rock itself.
    /// </summary>
    private void FillPolygon(IReadOnlyList<WPos> contour, uint color)
    {
        if (contour.Count < 3)
            return;
        var aa = this.SuspendAntiAliasedFill();
        foreach (var (a, b, c) in EarClip.Triangulate(contour))
            this.draw.AddTriangleFilled(this.W2S(contour[a]), this.W2S(contour[b]), this.W2S(contour[c]), color);
        this.draw.Flags = aa;
    }

    /// <summary>
    /// Whether any of <paramref name="loops"/> reaches outside the field. For the convex bounds a vertex
    /// test is exact; a donut, polygon or custom field can be crossed by an edge whose ends are both inside,
    /// so those always clip.
    /// </summary>
    private bool NeedsClip(IReadOnlyList<IReadOnlyList<WPos>> loops)
    {
        if (this.Bounds is not (ArenaBoundsCircle or ArenaBoundsSquare or ArenaBoundsRect))
            return true;
        foreach (var loop in loops)
            foreach (var p in loop)
                if (!this.Bounds.Contains(this.Center, p))
                    return true;
        return false;
    }

    /// <summary>
    /// Intersect the loops with the field and fill what is left: the triangles of the result, then its
    /// outline -- which now runs along the arena edge where the shape used to leave it.
    /// </summary>
    private void FillClipped(IReadOnlyList<IReadOnlyList<WPos>> loops, uint color)
    {
        var subject = new PolygonClipper.Operand();
        var offsets = new List<WDir>();
        foreach (var loop in loops)
        {
            if (loop.Count < 3)
                continue;
            offsets.Clear();
            foreach (var p in loop)
                offsets.Add(p - this.Center);
            subject.AddContour(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(offsets));
        }

        var clipped = this.clipper.Intersect(subject, this.BoundsOperand());
        if (clipped.Parts.Count == 0)
            return;

        var aa = this.SuspendAntiAliasedFill();
        foreach (var t in clipped.Triangulate())
            this.draw.AddTriangleFilled(this.W2S(this.Center + t.A), this.W2S(this.Center + t.B), this.W2S(this.Center + t.C), color);
        this.draw.Flags = aa;

        var outline = WithAlpha(color, 255);
        foreach (var part in clipped.Parts)
        {
            this.PolylineRel(part.Exterior, outline);
            for (var h = 0; h < part.HoleStarts.Count; ++h)
                this.PolylineRel(part.Interior(h), outline);
        }
    }

    private void PolylineRel(ReadOnlySpan<WDir> ring, uint color)
    {
        if (ring.Length < 2)
            return;
        var first = this.W2S(this.Center + ring[0]);
        var prev = first;
        for (var i = 1; i < ring.Length; ++i)
        {
            var next = this.W2S(this.Center + ring[i]);
            this.draw.AddLine(prev, next, color, 1.5f);
            prev = next;
        }
        this.draw.AddLine(prev, first, color, 1.5f);
    }

    /// <summary>
    /// The field as a clipper operand, relative to the centre: the outer contour minus the donut hole and
    /// any obstacles, built with the clipper itself so hole orientation is its problem, not ours. Cached
    /// per bounds instance -- a FATE swaps in a new one when its radius changes, which invalidates it.
    /// </summary>
    private PolygonClipper.Operand BoundsOperand()
    {
        if (this.clipBounds != null && ReferenceEquals(this.Bounds, this.clipBoundsFor))
            return this.clipBounds;

        RelSimplifiedComplexPolygon field;
        if (this.Bounds is ArenaBoundsCustom custom && custom.Center.AlmostEqual(this.Center, 0.1f))
        {
            field = custom.Polygon;
        }
        else
        {
            var outer = new PolygonClipper.Operand(this.Offsets(this.Bounds.Contour(this.Center)));
            var cutouts = new PolygonClipper.Operand();
            var any = false;
            if (this.Bounds.InnerContour(this.Center) is { } inner)
            {
                cutouts.AddContour(this.Offsets(inner));
                any = true;
            }
            foreach (var obstacle in this.Bounds.Obstacles(this.Center))
            {
                cutouts.AddContour(this.Offsets(obstacle));
                any = true;
            }
            field = any ? this.clipper.Difference(outer, cutouts) : this.clipper.Simplify(outer);
        }

        this.clipBoundsFor = this.Bounds;
        this.clipBounds = new PolygonClipper.Operand(field);
        return this.clipBounds;
    }

    private WDir[] Offsets(IReadOnlyList<WPos> contour)
    {
        var result = new WDir[contour.Count];
        for (var i = 0; i < contour.Count; ++i)
            result[i] = contour[i] - this.Center;
        return result;
    }

    private void FillConvex(IReadOnlyList<WPos> contour, uint color)
    {
        if (contour.Count < 3)
            return;
        var aa = this.SuspendAntiAliasedFill();
        var p0 = this.W2S(contour[0]);
        for (var i = 1; i < contour.Count - 1; ++i)
            this.draw.AddTriangleFilled(p0, this.W2S(contour[i]), this.W2S(contour[i + 1]), color);
        this.draw.Flags = aa;
    }

    private void FillDonut(WPos center, float inner, float outer, uint color)
    {
        const int seg = 48;
        var step = Angle.TwoPI / seg;
        var aa = this.SuspendAntiAliasedFill();
        for (var i = 0; i < seg; ++i)
        {
            var a0 = new Angle(step * i).ToDirection();
            var a1 = new Angle(step * (i + 1)).ToDirection();
            var o0 = this.W2S(center + a0 * outer);
            var o1 = this.W2S(center + a1 * outer);
            var i0 = this.W2S(center + a0 * inner);
            var i1 = this.W2S(center + a1 * inner);
            this.draw.AddTriangleFilled(o0, o1, i1, color);
            this.draw.AddTriangleFilled(o0, i1, i0, color);
        }

        this.draw.Flags = aa;
    }

    private void Polyline(IReadOnlyList<WPos> contour, uint color, float thickness, bool closed)
    {
        for (var i = 0; i < contour.Count - 1; ++i)
            this.draw.AddLine(this.W2S(contour[i]), this.W2S(contour[i + 1]), color, thickness);
        if (closed && contour.Count > 2)
            this.draw.AddLine(this.W2S(contour[^1]), this.W2S(contour[0]), color, thickness);
    }

    private static List<WPos> RectPts(WPos origin, Angle rotation, float lenFront, float lenBack, float halfWidth)
    {
        var f = rotation.ToDirection();
        var s = f.OrthoL();
        return
        [
            origin + f * lenFront + s * halfWidth,
            origin + f * lenFront - s * halfWidth,
            origin - f * lenBack - s * halfWidth,
            origin - f * lenBack + s * halfWidth,
        ];
    }

    private static uint WithAlpha(uint color, byte alpha) => (color & 0x00FFFFFFu) | ((uint)alpha << 24);
}
