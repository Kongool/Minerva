namespace Minerva;

/// <summary>
/// Which recordings to delete, and why. Recordings are the evidence every diagnosis in this project rests on, so
/// the rules are deliberately timid: the newest one is never deleted, nor is the one being written, and both limits
/// are off until somebody sets them.
/// <para>Kept apart from the file system so the policy can be tested without one, and so "what would this remove"
/// can be answered before anything is removed.</para>
/// </summary>
public static class ReplayRetention
{
    /// <summary>One recording on disk. <paramref name="Revision"/> is the recorder revision that wrote it.</summary>
    public readonly record struct Entry(string Path, long Bytes, DateTime Modified, int Revision = 0);

    /// <summary>A recording marked for deletion, with the rule that marked it.</summary>
    public readonly record struct Doomed(string Path, long Bytes, string Reason);

    public const long BytesPerMB = 1024L * 1024L;

    /// <summary>
    /// What to delete from <paramref name="files"/>, oldest first.
    /// </summary>
    /// <param name="keepDays">Delete recordings older than this many days. Zero or less: never by age.</param>
    /// <param name="maxTotalMB">Keep the folder under this size, deleting oldest first. Zero or less: no cap.</param>
    /// <param name="now">The clock, so age is testable.</param>
    /// <param name="inUse">The recording being written right now, which is never deleted.</param>
    /// <param name="currentRevision">
    /// Retire anything written before this recorder revision. This is the rule that matches how the pile actually goes
    /// stale: as more of the fight is exposed to recording, an older log cannot answer what a new one can, whatever
    /// its age or size. Zero or less: keep every revision.
    /// </param>
    public static List<Doomed> Plan(IReadOnlyList<Entry> files, int keepDays, int maxTotalMB, DateTime now, string? inUse = null, int currentRevision = 0)
    {
        var doomed = new List<Doomed>();
        if (files.Count == 0 || (keepDays <= 0 && maxTotalMB <= 0 && currentRevision <= 0))
            return doomed;

        // oldest first: both rules delete from that end
        var ordered = new List<Entry>(files);
        ordered.Sort(static (a, b) => a.Modified.CompareTo(b.Modified));

        // The newest recording is the one somebody is about to ask about, and the one the plugin reloads into the
        // playback tab. Never spend it, whatever the limits say.
        var newest = ordered[^1].Path;
        bool Untouchable(Entry e) => e.Path == newest || (inUse != null && e.Path == inUse);

        var remaining = new List<Entry>();
        var cutoff = keepDays > 0 ? now.AddDays(-keepDays) : DateTime.MinValue;
        foreach (var e in ordered)
        {
            if (Untouchable(e))
            {
                remaining.Add(e);
            }
            else if (currentRevision > 0 && e.Revision < currentRevision)
            {
                doomed.Add(new Doomed(e.Path, e.Bytes, e.Revision == 0
                    ? "recorded before the recorder was stamped"
                    : $"recorded by revision {e.Revision}, now {currentRevision}"));
            }
            else if (keepDays > 0 && e.Modified < cutoff)
            {
                doomed.Add(new Doomed(e.Path, e.Bytes, $"older than {keepDays} day{(keepDays == 1 ? "" : "s")}"));
            }
            else
            {
                remaining.Add(e);
            }
        }

        if (maxTotalMB > 0)
        {
            var cap = maxTotalMB * BytesPerMB;
            var total = 0L;
            foreach (var e in remaining)
                total += e.Bytes;
            foreach (var e in remaining)
            {
                if (total <= cap)
                    break;
                if (Untouchable(e))
                    continue;
                doomed.Add(new Doomed(e.Path, e.Bytes, $"over {maxTotalMB} MB"));
                total -= e.Bytes;
            }
        }

        return doomed;
    }

    /// <summary>"12 recordings, 563 MB" — what the folder holds, for a window that is about to offer to shrink it.</summary>
    public static string Describe(int count, long bytes)
        => $"{count} recording{(count == 1 ? "" : "s")}, {bytes / (double)BytesPerMB:0.#} MB";

    /// <summary>How many of these were written before <paramref name="currentRevision"/>: the ones that cannot answer
    /// what a recording made today can.</summary>
    public static int CountObsolete(IReadOnlyList<Entry> files, int currentRevision)
    {
        var n = 0;
        foreach (var e in files)
            if (e.Revision < currentRevision)
                ++n;
        return n;
    }
}
