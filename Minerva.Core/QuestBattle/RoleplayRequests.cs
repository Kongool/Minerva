using System.Numerics;

namespace Minerva.QuestBattle;

/// <summary>
/// The role-play actions a kit rotation (<see cref="UnmanagedRotation"/>) asked for this frame, best first: what Minerva
/// publishes as <c>Minerva.Hints.RoleplayActions</c> for the rotation plugin to press.
///
/// <para>Which entries are requests and not advice:</para>
/// <list type="bullet">
/// <item><see cref="Roleplay.AID"/> actions, always.</item>
/// <item>While a kit plays (<paramref name="kitDutyActions"/>), the duty's own actions it names: Thancred's in Coming
/// Clean is a duty action, not a role-play one.</item>
/// <item>While a quest battle script plays the duty (<paramref name="questDriven"/>), every action it asks for: a job
/// quest's Physick on the wounded or Hide before the sneak is the duty's objective, not a suggestion. No boss module
/// is in charge then (they stand the script down), so nothing else is on the queue.</item>
/// </list>
/// <para>Otherwise the queue holds what ported modules record -- Arm's Length before a knockback, a potion -- and that
/// stays advice, as it always was. Priority ties keep the order asked in, which is the rotation's order of preference
/// (Cure II on Hien before Stone IV).</para>
/// </summary>
public static class RoleplayRequests
{
    public static (uint ActionId, ulong TargetId, float Priority, Vector3 TargetPos, float FacingRad)[] From(ActionQueue queue, bool questDriven = false, ClientState.DutyAction[]? kitDutyActions = null)
        => queue.Entries
            .Where(e => e.action.Type == ActionType.Spell && (questDriven || Enum.IsDefined(typeof(Roleplay.AID), e.action.ID) || IsDutyAction(kitDutyActions, e.action.ID)))
            .OrderByDescending(e => e.priority)
            .Select(e => (e.action.ID, e.target?.InstanceID ?? 0ul, e.priority, e.targetPos, e.facingAngle?.Rad ?? float.NaN))
            .ToArray();

    private static bool IsDutyAction(ClientState.DutyAction[]? duty, uint id)
    {
        if (duty == null || id == 0)
            return false;
        foreach (var d in duty)
            if (d.Action.ID == id)
                return true;
        return false;
    }
}
