using Lumina.Excel;
using Minerva.Generation;
using LuminaAction = Lumina.Excel.Sheets.Action;

namespace Minerva.Generation;

/// <summary>
/// Names actions from the game's Action sheet via Lumina, so generated AID enums read as
/// <c>PunutiyPress = 36492</c> instead of <c>A36492</c>. Object names aren't looked up here — the
/// replay already carries each actor's in-game name, which the generator uses directly.
/// </summary>
public sealed class LuminaNameResolver : INameResolver
{
    private readonly ExcelSheet<LuminaAction>? sheet;

    public LuminaNameResolver() => this.sheet = Service.DataManager.GetExcelSheet<LuminaAction>();

    public string? ActionName(uint actionId)
    {
        if (this.sheet == null || !this.sheet.TryGetRow(actionId, out var action))
            return null;
        var name = action.Name.ExtractText();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    public string? ObjectName(uint oid) => null; // replay carries actor names already
}
