using Lumina.Excel;
using Minerva.Generation;
using LuminaAction = Lumina.Excel.Sheets.Action;
using LuminaOmen = Lumina.Excel.Sheets.Omen;

namespace Minerva.Generation;

/// <summary>
/// Reads an action's shape out of the game's Action sheet (CastType/EffectRange/XAxisModifier) via Lumina.
/// This is what turns the generator's output from "compiling stubs" into real <c>AOEShape*</c>
/// constructions for the majority of mechanics.
/// <para>Only the lookup lives here. The CastType table itself is <see cref="CastTypeShapes"/>, in the
/// game-free core, so it can be tested — it is a copied table with no derivable logic, and two wrong
/// entries in it produced shapes that silently contained nothing.</para>
/// <para>The Action sheet does not carry a cone's angle; that lives in the Omen row it points at, whose
/// VFX path encodes it by name (<c>gl_fan120_1bf</c> = a 120-degree cone), so the path is passed along.</para>
/// </summary>
public sealed class LuminaShapeResolver : IShapeResolver
{
    private readonly ExcelSheet<LuminaAction>? sheet;
    private readonly ExcelSheet<LuminaOmen>? omens;

    public LuminaShapeResolver()
    {
        this.sheet = Service.DataManager.GetExcelSheet<LuminaAction>();
        this.omens = Service.DataManager.GetExcelSheet<LuminaOmen>();
    }

    public ShapeHint Resolve(uint aid)
    {
        if (this.sheet == null || !this.sheet.TryGetRow(aid, out var a))
            return ShapeHint.Unknown;

        var omenID = a.Omen.RowId;
        var omenPath = omenID != 0 && this.omens != null && this.omens.TryGetRow(omenID, out var omen)
            ? omen.Path.ExtractText()
            : null;

        return CastTypeShapes.Resolve(a.CastType, a.EffectRange, a.XAxisModifier, omenPath);
    }
}
