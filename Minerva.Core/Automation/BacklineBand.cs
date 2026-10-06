namespace Minerva;

/// <summary>
/// Which band the backline keeps, and when.
/// <para>
/// Healers, casters and physical ranged want different things from the distance to a boss. A healer has to keep the
/// whole party inside its heals, so it stands close and barely backs off. A caster wants room: further out, a dashing
/// boss sweeps a narrower arc of its view, so line of sight holds. Physical ranged have no cast times, so moving costs
/// them little. One shared band (8/12/15) walked everyone in and out on every dash (the user, 2026-10-06: "a move to go
/// in and then a move to go out").
/// </para>
/// <para>
/// And only in a boss fight. On trash there is no band: the backline dodges what the trash casts and otherwise stays
/// put, walking in only when nothing it has can reach — a band on trash ran casters into walls and corridors as the
/// tank dragged packs together. The same rule Minerva already keeps for flank/rear (<see cref="UptimeTargeting.SideFor"/>).
/// </para>
/// </summary>
public static class BacklineBand
{
    /// <summary>The band for a backline character: by class in a boss fight, reach only otherwise.</summary>
    public static RangeBand For(ClassCategory category, bool bossFight, RangeBand healer, RangeBand caster, RangeBand physRanged)
        => !bossFight
            ? RangeBand.TrashReach
            : category switch
            {
                ClassCategory.Healer => healer,
                ClassCategory.PhysRanged => physRanged,
                _ => caster,
            };
}

/// <summary>
/// Has the target stopped moving long enough to judge the band against it?
/// <para>
/// A dashing boss crosses the backline's band edges mid-dash; walking after every leg is the in-and-out the band was
/// costing. So a walk does not START until the boss has stood still for <see cref="SettleSeconds"/> — one decision
/// after it lands. A walk already under way carries on, and danger dodges never wait for this.
/// </para>
/// </summary>
public sealed class TargetSettle
{
    /// <summary>The boss counts as still while it stays within this of where it stopped.</summary>
    public const float MovedYalms = 0.5f;

    /// <summary>How long it must stay still.</summary>
    public const double SettleSeconds = 1.0;

    private ulong targetId;
    private WPos anchor;
    private System.DateTime stillSince;
    private bool seen;

    /// <summary>Note the target this frame; true once it has held still for <see cref="SettleSeconds"/>.
    /// A new target counts as settled: nothing has been seen dashing.</summary>
    public bool Settled(ulong id, WPos position, System.DateTime now)
    {
        if (!this.seen || id != this.targetId)
        {
            this.seen = true;
            this.targetId = id;
            this.anchor = position;
            this.stillSince = now.AddSeconds(-SettleSeconds);
            return true;
        }

        if ((position - this.anchor).LengthSq() > MovedYalms * MovedYalms)
        {
            this.anchor = position;
            this.stillSince = now;
        }

        return (now - this.stillSince).TotalSeconds >= SettleSeconds;
    }
}
