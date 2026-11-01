using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Minerva;

namespace Minerva.Radar;

/// <summary>
/// ImGui-backed <see cref="Arena"/>: maps world (XZ) coordinates to a square canvas (north-up) and
/// draws with the window draw list. Filled zones come from <see cref="ZoneMeshes"/>, which cuts them to
/// the field and keeps them between frames; this class only turns their offsets into screen vertices.
/// </summary>
public sealed unsafe class ImGuiArena : Arena
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

    private readonly ZoneMeshes meshes = new();

    /// <summary>
    /// Screen-space rotation (radians) applied about the arena centre. 0 = north-up (default). Set to
    /// <c>player.Rotation.Rad + π</c> to make the local player's facing point up ("rotate with character").
    /// </summary>
    public float Rotation;

    // the rotation's sine and cosine, worked out once per value rather than once per vertex
    private float rotationFor = float.NaN;
    private float rotSin;
    private float rotCos = 1f;

    // screen points for one polyline, reused
    private Vector2[] points = new Vector2[128];

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
        this.scale = this.Bounds.Extent > 0f ? half / this.Bounds.Extent : 1f;
        this.meshes.BeginFrame(ImGui.GetFrameCount(), this.Center, this.Bounds, this.ClipZones);
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

    private Vector2 W2S(WPos p) => this.R2S(p - this.Center);

    /// <summary>An offset from the arena centre to a screen pixel.</summary>
    private Vector2 R2S(WDir o)
    {
        var x = o.X;
        var z = o.Z;
        if (this.Rotation != 0f)
        {
            if (this.Rotation != this.rotationFor)
            {
                (this.rotSin, this.rotCos) = MathF.SinCos(this.Rotation);
                this.rotationFor = this.Rotation;
            }
            (x, z) = (x * this.rotCos - z * this.rotSin, x * this.rotSin + z * this.rotCos);
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

        // A custom shape is drawn from its combined polygon, cut-outs and all (ZoneMeshes builds it that way). Its
        // contour loops are only the shapes it unites, and filled one by one they paint its holes too. Moko the
        // Restless, 2026-10-18: the band outside a shrinking floor (a 24.5y square less a 20y one) filled the whole
        // arena red while the dodge, which reads the real shape, said nothing was imminent.
        this.DrawMesh(this.meshes.Get(shape, origin, rotation), color, outline: true);
    }

    public override void OutlineShape(AOEShape shape, WPos origin, Angle rotation, uint color, float thickness = 1f)
    {
        if (color == 0u)
            color = Colors.Danger;

        switch (shape)
        {
            case AOEShapeCircle c:
                this.draw.AddCircle(this.W2S(origin), c.Radius * this.scale, color, 48, thickness);
                return;
            case AOEShapeDonut d:
                // two circles: its contour is one loop with a seam, and stroked whole it draws a spoke at east
                this.draw.AddCircle(this.W2S(origin), d.OuterRadius * this.scale, color, 48, thickness);
                this.draw.AddCircle(this.W2S(origin), d.InnerRadius * this.scale, color, 48, thickness);
                return;
            case AOEShapeCustom custom:
                var delta = custom.PolygonOrigin - this.Center;
                foreach (var part in custom.Polygon.Parts)
                {
                    this.PolylineRel(part.Exterior, delta, color, thickness, closed: true);
                    for (var h = 0; h < part.HoleStarts.Count; ++h)
                        this.PolylineRel(part.Interior(h), delta, color, thickness, closed: true);
                }
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

    /// <summary>A filled triangle, cut to the field when zones are. No outline: modules tile areas with these, and an
    /// outline would draw every seam.</summary>
    public override void AddTriangleFilled(WPos a, WPos b, WPos c, uint color)
    {
        if (this.ClipZones)
        {
            this.DrawMesh(this.meshes.GetTriangle(a, b, c), color, outline: false);
            return;
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
        var radius = this.Bounds.Extent;
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
    /// Hide everything drawn beyond the arena edge by painting over it with <paramref name="background"/>.
    /// Works for any arena that is star-shaped from its centre — every shape we use (circle, square, rect,
    /// donut, convex polygon): each boundary edge is extruded radially outward far enough to cover the
    /// canvas, tiling the whole exterior with no corner gaps. For a donut it also fills the inner hole.
    /// Call after module content, then redraw the boundary.
    /// <para>The colour is used exactly as given, alpha included. A translucent one dims the overspill
    /// rather than erasing it — painting over is all this can do, so partial opacity means partial hiding.
    /// A fully transparent one hides nothing, so nothing is drawn.</para>
    /// </summary>
    public void ClipOutsideArena(uint background)
    {
        if ((background >> 24) == 0u)
            return;

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
    /// A zone's mesh: its triangles written straight into the draw list (one reservation, not one call per
    /// triangle), then its outline at full alpha.
    /// <para>Raw triangles carry no anti-aliased fringe, so two sharing an edge meet without the hairline seam
    /// ImGui's own fill leaves there; the outline gives the shape its smooth edge.</para>
    /// </summary>
    private void DrawMesh(ZoneMeshes.Mesh mesh, uint color, bool outline)
    {
        var verts = mesh.Vertices;
        var indices = mesh.Indices;
        if (verts.Length > 0 && indices.Length > 0)
        {
            if (verts.Length >= ushort.MaxValue)
            {
                // past what one 16-bit reservation can address: a custom polygon this large is not expected, but
                // drawn a triangle at a time it still draws
                var aa = this.SuspendAntiAliasedFill();
                for (var i = 0; i + 2 < indices.Length; i += 3)
                    this.draw.AddTriangleFilled(this.R2S(verts[indices[i]]), this.R2S(verts[indices[i + 1]]), this.R2S(verts[indices[i + 2]]), color);
                this.draw.Flags = aa;
            }
            else
            {
                // PrimReserve may open a new draw command (a new vertex offset) when this list nears 65,536 vertices,
                // so the base index is read after it
                this.draw.PrimReserve(indices.Length, verts.Length);
                var uv = this.draw.Data.Handle->TexUvWhitePixel;
                var vtx = this.draw.VtxWritePtr.Handle;
                var idx = this.draw.IdxWritePtr;
                var baseIndex = this.draw.VtxCurrentIdx;
                for (var i = 0; i < verts.Length; ++i)
                    vtx[i] = new ImDrawVert(this.R2S(verts[i]), uv, color);
                for (var i = 0; i < indices.Length; ++i)
                    idx[i] = (ushort)(baseIndex + (uint)indices[i]);
                this.draw.VtxWritePtr = new ImDrawVertPtr(vtx + verts.Length);
                this.draw.IdxWritePtr = idx + indices.Length;
                this.draw.VtxCurrentIdx = baseIndex + (uint)verts.Length;
            }
        }

        if (!outline)
            return;
        var outlineColor = WithAlpha(color, 255);
        foreach (var o in mesh.Outlines)
            this.PolylineRel(o.Points, default, outlineColor, 1.5f, o.Closed);
    }

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

    /// <summary>One polyline call per ring rather than one line per edge: half the vertices, and joined corners.</summary>
    private void PolylineRel(ReadOnlySpan<WDir> ring, WDir delta, uint color, float thickness, bool closed)
    {
        if (ring.Length < 2)
            return;
        if (this.points.Length < ring.Length)
            this.points = new Vector2[ring.Length * 2];
        for (var i = 0; i < ring.Length; ++i)
            this.points[i] = this.R2S(ring[i] + delta);
        this.draw.AddPolyline(ref this.points[0], ring.Length, color, closed ? ImDrawFlags.Closed : ImDrawFlags.None, thickness);
    }

    private void Polyline(IReadOnlyList<WPos> contour, uint color, float thickness, bool closed)
    {
        if (contour.Count < 2)
            return;
        if (this.points.Length < contour.Count)
            this.points = new Vector2[contour.Count * 2];
        for (var i = 0; i < contour.Count; ++i)
            this.points[i] = this.W2S(contour[i]);
        this.draw.AddPolyline(ref this.points[0], contour.Count, color, closed && contour.Count > 2 ? ImDrawFlags.Closed : ImDrawFlags.None, thickness);
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

    private static uint WithAlpha(uint color, byte alpha) => (color & 0x00FFFFFFu) | ((uint)alpha << 24);
}
