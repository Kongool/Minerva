namespace Minerva;

/// <summary>
/// BossmodReborn's <c>BossModuleInfo</c>, reduced to the one part ported zone modules name.
///
/// <para>Kept separate from <see cref="ModuleMaturity"/> on purpose. Minerva's own scale is two values
/// and boss modules are declared against it; BossmodReborn's is five, and its ordering matters because
/// modules are filtered with <c>&gt;=</c>. Folding the extra values into the existing enum would shift
/// every boss module's meaning to make a handful of zone modules compile, which is the wrong way round.
/// This exists so a ported zone module's <c>BossModuleInfo.Maturity.Verified</c> resolves; the mapping
/// to Minerva's scale happens in <see cref="ZoneModuleInfoAttribute"/>.</para>
/// </summary>
public static class BossModuleInfo
{
    public enum Maturity
    {
        Dummy,
        WIP,
        Contributed,
        Verified,
        AISupport,
    }
}

/// <summary>
/// Metadata a zone module must carry to be found. Keyed on the content-finder condition, matching how
/// <see cref="ModuleRegistry"/> keys boss modules.
/// </summary>
/// <remarks>
/// <paramref name="territoryID"/> is carried for source compatibility and is not used to select a module:
/// BossmodReborn keys the lookup on CFC alone, and a zone whose CFC is 0 has no zone module either way.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ZoneModuleInfoAttribute(BossModuleInfo.Maturity maturity, uint cfcId, uint territoryID = 0) : Attribute
{
    public BossModuleInfo.Maturity Maturity => maturity;
    public uint CFCID => cfcId;
    public uint TerritoryID => territoryID;

    /// <summary>The same maturity on Minerva's two-value scale: anything below Contributed is WIP.</summary>
    public ModuleMaturity Minerva => maturity >= BossModuleInfo.Maturity.Contributed ? ModuleMaturity.Verified : ModuleMaturity.WIP;
}

/// <summary>
/// A module for a whole zone rather than for one boss.
///
/// <para>The distinction that matters to Minerva is lifetime: a <see cref="ModuleBase"/> activates when
/// its boss appears and tears down when the fight ends, while a zone module is up for as long as you are
/// in the zone. That makes it the right home for hazards with no encounter attached — open-world field
/// effects, deep dungeon traps, the standing dangers of a Foray zone — which a boss module cannot express
/// because there is no boss to hang them on.</para>
///
/// <para>Deliberately free of drawing. BossmodReborn's base draws its own hints through ImGui; Minerva
/// keeps <c>Minerva.Core</c> free of Dalamud so the whole decision engine stays testable headless, and the
/// plugin layer renders. The <c>WantDrawExtra</c>/<c>DrawExtra</c>/<c>WindowName</c> hooks are still here
/// because ported modules override them, and a module that overrides them can use ImGui freely — it lives
/// in the plugin project, not in Core.</para>
/// </summary>
public abstract class ZoneModule(WorldState ws) : IDisposable
{
    public readonly WorldState World = ws;

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    public virtual void Update() { }

    /// <summary>Advice lines for the whole zone, shown when no encounter is speaking.</summary>
    public virtual List<string> CalculateGlobalHints() => [];

    /// <summary>
    /// Contribute danger zones for the auto-dodge engine. Called after the boss module (if any) has had
    /// its turn, so a zone hazard adds to an encounter rather than replacing it.
    /// </summary>
    public virtual void CalculateAIHints(int playerSlot, Actor player, AIHints hints) { }

    /// <summary>Does this module have hints worth showing while no boss module is active?</summary>
    public virtual bool WantDrawHints() => false;

    // --- carried for ported modules; the plugin layer decides whether to honour them ---
    public virtual bool WantDrawExtra() => false;
    public virtual void DrawExtra() { }
    public virtual string WindowName() => "";
    public virtual void OnWindowClose() { }
}
