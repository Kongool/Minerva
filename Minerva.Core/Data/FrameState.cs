namespace Minerva;

/// <summary>
/// Per-frame timing sampled at the start of each game tick. <see cref="Duration"/> is the
/// delta used for cooldown/status/cast advancement; replays can scale it via
/// <see cref="TickSpeedMultiplier"/>. All timing derives from QueryPerformanceCounter, whose
/// frequency (QPF) is assumed constant for the session.
/// </summary>
public struct FrameState(DateTime timestamp, ulong qpc, uint index, float durationRaw, float duration, float tickSpeedMultiplier)
{
    public DateTime Timestamp = timestamp;
    public ulong QPC = qpc;
    public uint Index = index;
    public float DurationRaw = durationRaw;
    public float Duration = duration;
    public float TickSpeedMultiplier = tickSpeedMultiplier;
}
