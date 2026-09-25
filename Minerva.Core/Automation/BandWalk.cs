namespace Minerva;

/// <summary>
/// Whether the character is walking back to its range band, and so what the uptime goal looks like this frame.
///
/// <para>The band has two edges for two different questions. <b>When to move</b> is "outside [Min, Max]": inside it the
/// character is useful and should be left alone. <b>Where to stop</b> is "within a yalm of Preferred": stopping at the
/// edge it crossed would put it straight back out on the boss's next step. One stateless goal cannot say both, because
/// a walk that has crossed back into the band would read as finished before it reached Preferred. So this remembers
/// that a walk is under way, and while it is, narrows the goal to the stop window.</para>
///
/// <para>Mechanic dodges need nothing from here to take priority: the solve puts safety first and only walks for uptime
/// when nothing is coming. A walk interrupted by a dodge is still under way when the dodge ends, so it resumes.</para>
/// </summary>
public sealed class BandWalk
{
    /// <summary>
    /// How far outside the band a character must be before a walk may start while it is casting.
    /// <para>Smaller drift is not worth a cast: the character cannot move during a hard cast anyway, and steering then
    /// tells the rotation not to begin the next one. Two yalms out is worth that; half a yalm is not. The Daedalus
    /// slidecast signal is meant to plug in here: inside the slide window, any band walk may start.</para>
    /// </summary>
    public const float CastSlack = 2f;

    /// <summary>A walk back to the band is under way.</summary>
    public bool Walking { get; private set; }

    /// <summary>How far outside the band as configured the character stood this frame -- the band, not the stop
    /// window, since the cast rule is about drifting out of range and not about arriving.</summary>
    public float Excess { get; private set; }

    /// <summary>
    /// This frame's goal: the band as configured while standing still, the stop window while walking.
    /// </summary>
    public UptimeGoal Apply(UptimeGoal goal, WPos player, bool casting)
    {
        var standing = (player - goal.Target).Length();
        var (low, high) = goal.Aim;
        this.Excess = goal.ExcessRange(player);

        if (standing >= low && standing <= high)
            this.Walking = false;
        else if (this.Excess > 0f && (!casting || this.Excess > CastSlack))
            this.Walking = true;

        return this.Walking ? goal with { MinRange = low, Range = high } : goal;
    }

    /// <summary>May a walk to the band steer while the character is casting? Only when badly out of it.</summary>
    public bool MayStartDuringCast => this.Excess > CastSlack;

    /// <summary>Nothing to be in range of: forget any walk that was under way.</summary>
    public void Reset()
    {
        this.Walking = false;
        this.Excess = 0f;
    }
}
