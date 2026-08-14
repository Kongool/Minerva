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
            default: // cone, rect and any other convex contour
                this.FillConvex(shape.Contour(origin, rotation), color);
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
        this.Polyline(shape.Contour(origin, rotation), color, thickness, closed: true);
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
        var fwd = rotation.ToDirection();
        var side = fwd.OrthoL();
        var tip = this.W2S(pos + fwd * r);
        var bl = this.W2S(pos - fwd * r * 0.6f + side * r * 0.7f);
        var br = this.W2S(pos - fwd * r * 0.6f - side * r * 0.7f);
        this.draw.AddTriangleFilled(tip, bl, br, color);
        this.draw.AddCircle(this.W2S(pos), r * this.scale, WithAlpha(color, 160), 24, 1.5f);
    }

    public override void DrawBoundary()
    {
        this.Polyline(this.Bounds.Contour(this.Center), Colors.Border, 2f, closed: true);
        var inner = this.Bounds.InnerContour(this.Center);
        if (inner != null)
            this.Polyline(inner, Colors.Border, 2f, closed: true); // donut hole
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
    }

    // --- helpers ---
    private void FillConvex(IReadOnlyList<WPos> contour, uint color)
    {
        if (contour.Count < 3)
            return;
        var p0 = this.W2S(contour[0]);
        for (var i = 1; i < contour.Count - 1; ++i)
            this.draw.AddTriangleFilled(p0, this.W2S(contour[i]), this.W2S(contour[i + 1]), color);
    }

    private void FillDonut(WPos center, float inner, float outer, uint color)
    {
        const int seg = 48;
        var step = Angle.TwoPI / seg;
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
