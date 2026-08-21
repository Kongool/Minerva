using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;

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

    /// <summary>The object's event state (used by some mechanics to gate on a boss's phase/prop state).</summary>
    public static byte EventState(nint objectAddress)
        => objectAddress != 0 ? ((GameObject*)objectAddress)->EventState : (byte)0;

    /// <summary>The object's render flags (0 = fully visible; non-zero hides/desaturates — used to detect (in)active props).</summary>
    public static int RenderFlags(nint objectAddress)
        => objectAddress != 0 ? (int)((GameObject*)objectAddress)->RenderFlags : 0;

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

    /// <summary>
    /// Local player's world position and facing (radians, game convention: 0 = south). Object-table
    /// slot 0 (index-sorted) is the local player. False before the player object exists (e.g. loading).
    /// Read fresh here rather than from WorldState because the movement hook can fire off-frame.
    /// </summary>
    public static bool TryLocalPlayerPose(out Vector3 position, out float rotation)
    {
        position = default;
        rotation = 0f;
        var mgr = GameObjectManager.Instance();
        var player = mgr != null ? mgr->Objects.IndexSorted[0].Value : null;
        if (player == null)
            return false;
        position = player->Position;
        rotation = player->Rotation;
        return true;
    }


    /// <summary>
    /// Turn the character to face <paramref name="direction"/> using the game's own auto-face routine.
    /// <para>Writing rotation directly fights the client: the game interpolates rotation each frame and will
    /// turn you back. Calling the routine the game uses when an ability auto-faces its target, and then
    /// clearing the in-flight interpolation target, is what makes the turn stick.</para>
    /// </summary>
    public static bool TryFace(float directionRad)
    {
        var mgr = GameObjectManager.Instance();
        var obj = mgr != null ? mgr->Objects.IndexSorted[0].Value : null;
        if (obj == null)
            return false;

        var am = FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();
        if (am == null)
            return false;

        var (sin, cos) = MathF.SinCos(directionRad);
        var pos = obj->Position;
        var target = new Vector3(pos.X + sin, pos.Y, pos.Z + cos);
        am->AutoFaceTargetPosition(&target);
        return true;
    }

    /// <summary>
    /// The Occult Crescent critical encounter currently being fought, or null.
    /// <para>Occult content is not a duty and its encounters are not named after their bosses — the name
    /// belongs to the dynamic event, which is why searching for a boss by name finds nothing there. The
    /// director tracks each event's lifecycle, so "a fight is underway" is a fact we can read rather than
    /// infer from HP.</para>
    /// </summary>
    public static string? ActiveCriticalEncounter()
    {
        var director = PublicContentOccultCrescent.GetInstance();
        if (director == null)
            return null;

        foreach (var ev in director->DynamicEventContainer.Events)
        {
            // Register is the join window and Warmup is the seal; only Battle means it is actually running
            if (ev.State != DynamicEventState.Battle)
                continue;
            var name = ev.Name.ToString();
            if (!string.IsNullOrWhiteSpace(name))
                return name;
        }

        return null;
    }

    /// <summary>
    /// Active camera's azimuth in radians, derived from its view matrix (needed to steer under the
    /// "legacy" movement scheme, where forward is relative to the camera rather than the character).
    /// False when no camera is resolvable.
    /// </summary>
    public static bool TryCameraAzimuth(out float azimuth)
    {
        azimuth = 0f;
        var cam = CameraManager.Instance()->GetActiveCamera();
        var render = cam != null ? cam->SceneCamera.RenderCamera : null;
        if (render == null)
            return false;
        var view = render->ViewMatrix;
        azimuth = MathF.Atan2(view.M13, view.M33);
        return true;
    }

    /// <summary>How far above a candidate spot the floor probe starts. Enough to clear a step or a ramp
    /// without starting inside the geometry, which would report a miss on solid ground.</summary>
    private const float ProbeRise = 3f;

    /// <summary>How far below that a floor still counts. Past this it is a drop, not a step down.</summary>
    private const float ProbeDrop = 8f;

    /// <summary>
    /// Is there ground under this spot?
    /// <para>A downward raycast against the zone's collision. BossmodReborn never asks this — it raycasts
    /// only horizontally, for line of sight — which is why with no obstacle bitmap for the zone its AI will
    /// path a character straight off a platform. Asking the floor is the check that needs no data baked
    /// ahead of time and works in content nobody has mapped.</para>
    /// <para>Returns true when collision cannot be reached, so a failure here degrades to today's
    /// behaviour rather than freezing the dodge.</para>
    /// </summary>
    public static bool HasFloorAt(float x, float z, float y)
    {
        var origin = new Vector3(x, y + ProbeRise, z);
        var down = new Vector3(0f, -1f, 0f);
        var module = Framework.Instance() != null ? Framework.Instance()->BGCollisionModule : null;
        if (module == null)
            return true;
        return BGCollisionModule.RaycastMaterialFilter(origin, down, out _, ProbeRise + ProbeDrop);
    }

    /// <summary>
    /// Is the whole walk from <paramref name="from"/> to <paramref name="to"/> over ground?
    /// <para>Probing only the destination is not enough: a gap between here and there is walked into at
    /// full speed. Sampled rather than swept — a ray every few yalms costs nothing next to a fall.</para>
    /// </summary>
    public static bool PathHasFloor(Vector3 from, Vector3 to, float step = 4f)
    {
        var dx = to.X - from.X;
        var dz = to.Z - from.Z;
        var dist = MathF.Sqrt((dx * dx) + (dz * dz));
        var steps = Math.Clamp((int)MathF.Ceiling(dist / MathF.Max(step, 1f)), 1, 8);
        for (var i = 1; i <= steps; ++i)
        {
            var t = (float)i / steps;
            if (!HasFloorAt(from.X + (dx * t), from.Z + (dz * t), from.Y))
                return false;
        }

        return true;
    }
}
