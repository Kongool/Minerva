using System;
using System.Collections.Generic;
using System.IO;
using Lumina;
using Lumina.Excel;
using Minerva.Generation;
using LuminaAction = Lumina.Excel.Sheets.Action;
using LuminaBNpcName = Lumina.Excel.Sheets.BNpcName;
using LuminaOmen = Lumina.Excel.Sheets.Omen;

namespace Minerva.Validate;

/// <summary>
/// The game's Excel sheets read straight off the install, with no Dalamud and no running game.
/// <para>Module generation needed the Action sheet (CastType/EffectRange/XAxisModifier, plus the Omen row
/// that encodes a cone's angle) and the BNpcName sheet for readable names. In the plugin those come from
/// <c>Service.DataManager</c>, which only exists in-process — so generation could only ever happen
/// in-game, and every draft had to be carried back to the dev box by hand. Lumina reads <c>sqpack</c>
/// directly, so the same lookups work here and the extract → generate → validate loop closes.</para>
/// </summary>
public sealed class OfflineGameSheets : IShapeResolver, INameResolver
{
    private readonly ExcelSheet<LuminaAction>? actions;
    private readonly ExcelSheet<LuminaOmen>? omens;
    private readonly ExcelSheet<LuminaBNpcName>? bnpcNames;

    private OfflineGameSheets(GameData data)
    {
        this.actions = data.GetExcelSheet<LuminaAction>();
        this.omens = data.GetExcelSheet<LuminaOmen>();
        this.bnpcNames = data.GetExcelSheet<LuminaBNpcName>();
    }

    /// <summary>Open the sheets at a game path, or return null with a reason.</summary>
    public static OfflineGameSheets? TryOpen(string sqpackPath, out string error)
    {
        error = string.Empty;
        try
        {
            if (!Directory.Exists(sqpackPath))
            {
                error = $"no such directory: {sqpackPath}";
                return null;
            }

            var sheets = new OfflineGameSheets(new GameData(sqpackPath));
            if (sheets.actions == null)
            {
                error = $"opened {sqpackPath} but it has no Action sheet — is this the 'game/sqpack' folder?";
                return null;
            }

            return sheets;
        }
        catch (Exception ex)
        {
            error = $"{sqpackPath}: {ex.Message}";
            return null;
        }
    }

    /// <summary>
    /// Where FFXIV usually lives, so the common case needs no argument. Ordered most-likely first; the
    /// caller can always pass an explicit path.
    /// </summary>
    public static IEnumerable<string> LikelyPaths()
    {
        string[] roots =
        [
            @"C:\",                       // a bare install: XIVLauncher reports GamePath "C:\" for it
            @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn",
            @"C:\Program Files\SquareEnix\FINAL FANTASY XIV - A Realm Reborn",
            @"C:\SquareEnix\FINAL FANTASY XIV - A Realm Reborn",
            @"C:\Program Files (x86)\Steam\steamapps\common\FINAL FANTASY XIV Online",
            @"D:\SquareEnix\FINAL FANTASY XIV - A Realm Reborn",
            @"D:\Games\FINAL FANTASY XIV - A Realm Reborn",
            @"D:\SteamLibrary\steamapps\common\FINAL FANTASY XIV Online",
        ];
        foreach (var r in roots)
            yield return Path.Combine(r, "game", "sqpack");
    }

    public ShapeHint Resolve(uint aid)
    {
        if (this.actions == null || !this.actions.TryGetRow(aid, out var a))
            return ShapeHint.Unknown;

        var omenID = a.Omen.RowId;
        var omenPath = omenID != 0 && this.omens != null && this.omens.TryGetRow(omenID, out var omen)
            ? omen.Path.ExtractText()
            : null;

        // the same table the plugin uses: it lives in Core precisely so both callers share one reading
        return CastTypeShapes.Resolve(a.CastType, a.EffectRange, a.XAxisModifier, omenPath);
    }

    public string? ActionName(uint actionId)
        => this.actions != null && this.actions.TryGetRow(actionId, out var a) && a.Name.ExtractText() is { Length: > 0 } n ? n : null;

    public string? ObjectName(uint oid)
        => this.bnpcNames != null && this.bnpcNames.TryGetRow(oid, out var b) && b.Singular.ExtractText() is { Length: > 0 } n ? n : null;
}
