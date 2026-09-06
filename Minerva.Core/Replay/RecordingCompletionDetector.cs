namespace Minerva;

/// <summary>
/// Reports a recording completion only after an explicit success signal and a short delay. Combat
/// loss is deliberately not an input: wipes, deaths, and temporary disengages must not stop a replay.
/// </summary>
public sealed class RecordingCompletionDetector
{
    private readonly TimeSpan gracePeriod;
    private readonly TimeSpan idleTimeout;
    private TimeSpan pendingFor;
    private TimeSpan outOfCombatFor;
    private bool completionPending;
    private bool sawCombat;

    public bool CompletionPending => this.completionPending;

    /// <param name="gracePeriod">How long to keep recording after the fight is known to be over.</param>
    /// <param name="idleTimeout">How long out of combat ends the recording by itself. Zero disables it.</param>
    public RecordingCompletionDetector(TimeSpan gracePeriod, TimeSpan idleTimeout = default)
    {
        if (gracePeriod < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(gracePeriod));
        if (idleTimeout < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(idleTimeout));
        this.gracePeriod = gracePeriod;
        this.idleTimeout = idleTimeout;
    }

    public void Reset()
    {
        this.completionPending = false;
        this.pendingFor = TimeSpan.Zero;
        this.outOfCombatFor = TimeSpan.Zero;
        this.sawCombat = false;
    }

    /// <summary>
    /// Feed the player's combat state. Once combat has been seen, staying out of it for the whole idle
    /// timeout ends the recording.
    /// <para>Sustained rather than immediate, because losing combat is not the same as the fight being
    /// over: a wipe, a death, a moment between adds and a boss all drop it briefly. But something has to
    /// end a recording of a fight that has no death to detect — a FATE boss that despawns, a pull that is
    /// abandoned — or it runs until the player notices, and every one of those is a file of mostly nothing.</para>
    /// <para>Only after combat has been seen: a recording started while walking to the boss must not end
    /// before the pull.</para>
    /// </summary>
    public void NoteCombat(bool inCombat, TimeSpan elapsed)
    {
        if (this.idleTimeout <= TimeSpan.Zero)
            return;

        if (inCombat)
        {
            this.sawCombat = true;
            this.outOfCombatFor = TimeSpan.Zero;
            return;
        }

        if (!this.sawCombat || this.completionPending)
            return;

        if (elapsed > TimeSpan.Zero)
            this.outOfCombatFor += elapsed;
        if (this.outOfCombatFor >= this.idleTimeout)
            this.SignalCompletion();
    }

    /// <summary>Mark the encounter as positively completed (duty success or confirmed final boss death).</summary>
    public void SignalCompletion()
    {
        // Idempotent. The boss's death is polled every frame rather than caught as an event, so this
        // arrives repeatedly for as long as the boss stays dead; restarting the countdown each time would
        // hold the grace period open forever and the recording would never stop at all.
        if (this.completionPending)
            return;

        this.completionPending = true;
        this.pendingFor = TimeSpan.Zero;
    }

    /// <summary>Advance the post-completion delay; true is returned exactly once.</summary>
    public bool Update(TimeSpan elapsed)
    {
        if (!this.completionPending)
            return false;

        if (elapsed > TimeSpan.Zero)
            this.pendingFor += elapsed;
        if (this.pendingFor < this.gracePeriod)
            return false;

        this.Reset();
        return true;
    }
}
