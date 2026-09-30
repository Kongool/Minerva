using System.Numerics;

namespace Minerva.QuestBattle;

/// <summary>
/// The role-play actions a kit rotation (<see cref="UnmanagedRotation"/>) asked for this frame, best first: what Minerva
/// publishes as <c>Minerva.Hints.RoleplayActions</c> for the rotation plugin to press.
///
/// <para>Only <see cref="Roleplay.AID"/> actions. The same queue holds what other ported modules record -- Arm's Length
/// before a knockback, a potion -- and that stays advice, as it always was. Priority ties keep the order the rotation
/// asked in, which is its order of preference (Cure II on Hien before Stone IV).</para>
/// </summary>
public static class RoleplayRequests
{
    public static (uint ActionId, ulong TargetId, float Priority, Vector3 TargetPos, float FacingRad)[] From(ActionQueue queue)
        => queue.Entries
            .Where(e => e.action.Type == ActionType.Spell && Enum.IsDefined(typeof(Roleplay.AID), e.action.ID))
            .OrderByDescending(e => e.priority)
            .Select(e => (e.action.ID, e.target?.InstanceID ?? 0ul, e.priority, e.targetPos, e.facingAngle?.Rad ?? float.NaN))
            .ToArray();
}
