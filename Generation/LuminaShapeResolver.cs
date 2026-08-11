using System;
using Lumina.Excel;
using Minerva.Generation;
using LuminaAction = Lumina.Excel.Sheets.Action;

namespace Minerva.Generation;

/// <summary>
/// Resolves action shapes from the game's Action sheet (CastType/EffectRange/XAxisModifier) via
/// Lumina. This is what turns the generator's output from "compiling stubs" into real
/// <c>AOEShape*</c> constructions for the majority of mechanics. Values the sheet doesn't carry —
/// cone half-angle, donut inner radius (they live in the Omen data) — are filled with sensible
/// defaults and flagged <see cref="ShapeHint.NeedsReview"/> so the generator emits a TODO.
/// </summary>
public sealed class LuminaShapeResolver : IShapeResolver
{
    private readonly ExcelSheet<LuminaAction>? sheet;

    public LuminaShapeResolver() => this.sheet = Service.DataManager.GetExcelSheet<LuminaAction>();

    public ShapeHint Resolve(uint aid)
    {
        if (this.sheet == null || !this.sheet.TryGetRow(aid, out var a))
            return ShapeHint.Unknown;

        float range = a.EffectRange;
        float halfWidth = a.XAxisModifier / 2f;

        // CastType classifies the aim geometry; EffectRange/XAxisModifier size it.
        return a.CastType switch
        {
            2 or 5 or 6 => new ShapeHint(ShapeKind.Circle, Radius: range),
            3 or 13 => new ShapeHint(ShapeKind.Cone, Radius: range, HalfAngleDeg: 45f, NeedsReview: true), // angle is in the omen, not the sheet
            4 or 7 or 10 or 12 => new ShapeHint(ShapeKind.Rect, Radius: range, HalfWidth: halfWidth),
            8 => new ShapeHint(ShapeKind.Donut, Radius: range, InnerRadius: MathF.Round(range * 0.4f), NeedsReview: true),
            11 => new ShapeHint(ShapeKind.Cross, Radius: range, HalfWidth: halfWidth, NeedsReview: true),
            1 => new ShapeHint(ShapeKind.SingleTarget),
            _ => ShapeHint.Unknown,
        };
    }
}
