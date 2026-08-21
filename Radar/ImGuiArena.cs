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
    /// Screen-space rotation (radians) applied about the arena centre. 0 = north-up (default). Set to
    /// <c>player.Rotation.Rad + π</c> to make the local player's facing point up ("rotate with character").
    /// </summary>
    public float Rotation;

    /// <summary>Set the transform for this frame from a canvas rectangle.</summary>
    public void Begin(Vector2 canvasTopLeft, Vector2 canvasSize, float margin = 14f)
    {
        this.draw = ImGui.GetWindowDrawList();
        this.screenCenter = canvasTopLeft + canvasSize * 0.5f;
        this.canvasTopLeft = canvasTopLeft;
        this.canvasSize = canvasSize;
        var half = MathF.Min(canvasSize.X, canvasSize.Y) * 0.5f - margin;
        this.scale = this.Bounds.Radius > 0f ? half / this.Bounds.Radius : 1f;
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

    public override void AddLine(WPos a, WPos b, uint color, float thickness = 1f)
        => this.draw.AddLine(this.W2S(a), this.W2S(b), color, thickness);

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
