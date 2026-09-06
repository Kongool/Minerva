using System;
using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
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

    /// <summary>
    /// Quest id -> ContentFinderCondition id of the solo duty that quest runs. A ContentFinderCondition of
    /// link type 5 is a quest battle; its Content row is a QuestBattle, which names the quest. Empty (and
    /// logged) if the sheets cannot be read, in which case quest modules simply stay dormant as before.
    /// </summary>
    public static Dictionary<uint, uint> QuestBattleDuties()
    {
        var map = new Dictionary<uint, uint>();
        try
        {
            var cfcs = Service.DataManager.GetExcelSheet<Lumina.Excel.Sheets.ContentFinderCondition>();
            var battles = Service.DataManager.GetExcelSheet<Lumina.Excel.Sheets.QuestBattle>();
            if (cfcs == null || battles == null)
                return map;
            foreach (var cfc in cfcs)
            {
                if (cfc.ContentLinkType != 5)
                    continue;
                var qb = battles.GetRowOrDefault(cfc.Content.RowId);
                if (qb is { } battle && battle.Quest.RowId != 0)
                    map.TryAdd(battle.Quest.RowId, cfc.RowId);
            }
        }
        catch (Exception ex)
        {
            Service.Log.Warning(ex, "Minerva: could not read quest battle duties from game data; quest modules will not activate.");
        }
        return map;
    }

    /// <summary>
    /// Switch auto-attack off. While it is on, the character re-faces its target every frame and no facing
    /// set here survives it: a Warrior stood facing a Holy Sphere through a whole gaze while a Sage on the
    /// same pull turned away fine (2026-09-05). BossmodReborn gets the same effect by dropping the target for
    /// the last half second of a gaze. The next action the rotation fires turns auto-attack back on.
    /// </summary>
    public static void StopAutoAttack()
    {
        var ui = UIState.Instance();
        if (ui == null)
            return;
        ref var aa = ref ui->WeaponState.AutoAttackState;
        if (aa.IsAutoAttacking)
            aa.Set(false);
    }

    /// <summary>
    /// Interrupt the character's own cast. Movement written below the input layer does not cancel a cast
    /// -- the character simply does not move, and a Sage stood through an eight second raise in a seven
    /// second puddle. The same call BossmodReborn's mechanic AI makes.
    /// </summary>
    public static void CancelCast()
    {
        var ui = UIState.Instance();
        if (ui != null)
            ui->Hotbar.CancelCast();
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
    /// The game's own target classification — the red/yellow/green nameplate logic — reporting whether this
    /// object is an <c>Enemy</c>. This is what BossmodReborn uses for ally/enemy, and it is far more reliable
    /// than the <c>Hostile</c> status flag (which reads false on out-of-combat enemies). Note: an untargetable
    /// helper is not classified as an enemy here, so callers must special-case <see cref="ActorType.Helper"/>.
    /// </summary>
    public static bool IsClassifiedEnemy(nint objectAddress)
        => objectAddress != 0 && ActionManager.ClassifyTarget((Character*)objectAddress) == ActionManager.TargetCategory.Enemy;

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
    /// An actor's Occult Crescent Knowledge Level and elemental affinity.
    /// <para>False outside Occult Crescent, where the game does not attach the block at all — callers keep
    /// the default rather than writing a zero, so a level-0 reading always means "genuinely level 0" and
    /// never "we could not tell".</para>
    /// </summary>
    public static bool TryForayInfo(nint characterAddress, out byte level, out byte element)
    {
        level = 0;
        element = 0;
        if (characterAddress == 0)
            return false;
        var info = ((BattleChara*)characterAddress)->GetForayInfo();
        if (info == null)
            return false;
        level = info->Level;
        element = info->Element;
        return true;
    }

    /// <summary>
    /// The direction an in-progress cast is <i>aimed</i>, which is not the same as where the caster is
    /// facing.
    /// <para>The game snapshots this when the cast begins and keeps it fixed; the actor's own rotation goes
    /// on changing. Reading the actor instead draws a directional AOE wherever the caster happens to be
    /// pointing at the moment it is rendered, which is visibly wrong for any helper that is still turning
    /// when its cast starts, and for anything aimed away from its own facing. BossmodReborn reads this
    /// field, and the difference showed up as Necrophobia's Dark Current lines — parallel north-south in the
    /// fight — being drawn on a diagonal.</para>
    /// <para>False when the actor is not a BattleChara or is not casting; callers fall back to the actor's
    /// rotation, which is the old behaviour and still better than nothing.</para>
    /// </summary>
    public static bool TryCastRotation(nint characterAddress, out float rotation)
    {
        rotation = default;
        if (characterAddress == 0)
            return false;
        var chr = (BattleChara*)characterAddress;
        if (chr->GetCastInfo() == null)
            return false;
        rotation = chr->CastRotation;
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
        // The routine only sets the facing; the game keeps interpolating rotation toward its own desired
        // rotation -- for a melee, the target it is hitting -- and turns the character back next frame.
        // Eye to Eye, 2026-09-05: five seconds facing the boss through See No Evil, petrified, and the log
        // showed the facing never moving a degree. Rewriting the desire is what BossmodReborn does too.
        var pm = (PlayerMove*)obj;
        pm->Move.Interpolation.DesiredRotation = directionRad;
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
    /// <summary>The actor's current mount, or false when it is not a character / not mounted.</summary>
    public static unsafe bool TryMountId(nint addr, out uint mountId)
    {
        mountId = 0u;
        if (addr == nint.Zero)
            return false;
        var chr = (Character*)addr;
        mountId = chr->Mount.MountId;
        return mountId != 0u;
    }

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
