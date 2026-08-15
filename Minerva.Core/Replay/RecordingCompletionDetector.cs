namespace Minerva;

/// <summary>
/// Reports a recording completion only after an explicit success signal and a short delay. Combat
/// loss is deliberately not an input: wipes, deaths, and temporary disengages must not stop a replay.
/// </summary>
public sealed class RecordingCompletionDetector
{
    private readonly TimeSpan gracePeriod;
    private TimeSpan pendingFor;
    private bool completionPending;

    public bool CompletionPending => this.completionPending;

    public RecordingCompletionDetector(TimeSpan gracePeriod)
    {
        if (gracePeriod < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(gracePeriod));
        this.gracePeriod = gracePeriod;
    }

    public void Reset()
    {
        this.completionPending = false;
        this.pendingFor = TimeSpan.Zero;
    }

    /// <summary>Mark the encounter as positively completed (duty success or confirmed final boss death).</summary>
    public void SignalCompletion()
    {
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
