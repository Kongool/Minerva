using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Minerva;

/// <summary>
/// Dalamud injects these services on load via <c>PluginInterface.Create&lt;Service&gt;()</c>.
/// The lower group is not consumed yet; it is declared ahead of the GameSync work
/// (Phase 2) that turns live game state into a WorldState.
/// </summary>
internal sealed class Service
{
    // --- Core (used from Phase 0) ---
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IDutyState DutyState { get; private set; } = null!;
    [PluginService] internal static IFateTable FateTable { get; private set; } = null!;

    /// <summary>
    /// Encounter-specific settings, addressed by node type: <c>Service.Config.Get&lt;FRUConfig&gt;()</c>.
    /// <para>Spelled the way BossmodReborn spells it so ported modules reach their strategy settings
    /// unchanged. The root itself lives in Minerva.Core and is always usable — modules read their config in
    /// their constructors, and the offline validator builds modules with no plugin at all.</para>
    /// </summary>
    internal static ConfigRoot Config => ConfigRoot.Instance;

    // --- GameSync / radar (declared ahead of Phase 2) ---
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IPartyList PartyList { get; private set; } = null!;
    [PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;
    [PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInterop { get; private set; } = null!;
    [PluginService] internal static IGameConfig GameConfig { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static IDtrBar DtrBar { get; private set; } = null!;
}
