namespace Minerva.Components;

/// <summary>
/// Which knockback-immunity buffs a party member is holding, and until when.
///
/// <para>Three slots rather than one expiry because the groups stack: a player may hold a role action, a job
/// action and a duty action at once, and losing one must not clear the others. A single "immune until"
/// field would be wrong the moment Arm's Length ran out under a Lost Manawall.</para>
/// </summary>
public struct PlayerImmuneState
{
    public DateTime RoleBuffExpire;
    public DateTime JobBuffExpire;
    public DateTime DutyBuffExpire;

    public readonly bool ImmuneAt(DateTime time)
        => this.RoleBuffExpire > time || this.JobBuffExpire > time || this.DutyBuffExpire > time;
}

/// <summary>
/// The statuses that make a knockback do nothing, shared by every component that cares.
///
/// <para>Single-sourced deliberately. Two components track this and the table is game data neither of them
/// owns; a second copy would go stale silently, and the failure — a knockback the AI still walks you out of
/// — looks like a pathing bug rather than a missing status id.</para>
/// </summary>
public static class ImmunityStatuses
{
    // a player can hold one of each group at a time, which is why they are tracked separately
    private static readonly uint[] Role = [3054u, 160u, 1209u];  // Guard (PVP), Surecast, Arm's Length
    private static readonly uint[] Job = [1722u, 1176u];         // Diamondback (BLU), Inner Strength (WAR)
    private static readonly uint[] Duty = [2345u];               // Lost Manawall (Bozja)

    /// <summary>Record a status gain or loss. <paramref name="expireAt"/> is <c>default</c> on loss.</summary>
    public static void Track(ref PlayerImmuneState state, uint sid, DateTime expireAt)
    {
        if (Array.IndexOf(Role, sid) >= 0)
            state.RoleBuffExpire = expireAt;
        else if (Array.IndexOf(Job, sid) >= 0)
            state.JobBuffExpire = expireAt;
        else if (Array.IndexOf(Duty, sid) >= 0)
            state.DutyBuffExpire = expireAt;
    }
}
