using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace Minerva.GameSync;

/// <summary>
/// Thin bridge to values Dalamud's managed services don't surface, read via FFXIVClientStructs.
/// Kept small and isolated so the rest of the sync stays managed. Everything here is persistent
/// per-frame state (safe to poll); transient events (icons, map effects, RSV) go through packet
/// hooks in <see cref="WorldStateGameSync"/>, not here.
/// </summary>
internal static unsafe class GameData
{
    /// <summary>Content Finder Condition id of the current duty (0 in the open world).</summary>
    public static ushort CurrentContentFinderConditionId()
    {
        var gm = GameMain.Instance();
        return gm != null ? gm->CurrentContentFinderConditionId : (ushort)0;
    }

    /// <summary>Shield as a percentage (0-100+) of max HP; 0 if none.</summary>
    public static byte ShieldPercent(nint characterAddress)
        => characterAddress != 0 ? ((Character*)characterAddress)->ShieldValue : (byte)0;

    /// <summary>
    /// True if the actor has a live cast-info block. Dalamud's cast getters (<c>IsCasting</c>,
    /// <c>CastActionId</c>, …) dereference this pointer <i>without</i> a null check and throw when it's
    /// absent — which it is for any BattleChara not currently casting — so callers must gate on this.
    /// </summary>
    public static bool HasCastInfo(nint characterAddress)
        => characterAddress != 0 && ((BattleChara*)characterAddress)->GetCastInfo() != null;

    /// <summary>
    /// True if the actor has a live status manager. Same hazard as <see cref="HasCastInfo"/>: Dalamud's
    /// <c>StatusList</c> wraps this pointer and reading <c>.Length</c> throws when it's null (some special
    /// BattleChara-typed objects have none), so gate status reads on this.
    /// </summary>
    public static bool HasStatusManager(nint characterAddress)
        => characterAddress != 0 && ((BattleChara*)characterAddress)->GetStatusManager() != null;

    /// <summary>
    /// Ground-target location of an in-progress area-targeted cast. False if the actor isn't a
    /// BattleChara / isn't casting. Used to place location-targeted AOEs correctly.
    /// </summary>
    public static bool TryCastLocation(nint characterAddress, out Vector3 location)
    {
        location = default;
        if (characterAddress == 0)
            return false;
        var ci = ((BattleChara*)characterAddress)->GetCastInfo();
        if (ci == null)
            return false;
        location = ci->TargetLocation;
        return true;
    }
}
