namespace Minerva;

/// <summary>
/// What Minerva costs the game per frame over one fight, stage by stage, for a line in the log when the fight ends.
/// <para>The offline probes time the solver over a recording, but only the plugin can time itself inside the game, next
/// to the game's own frame. Elm Gigas, 2026-10-19: the game ran 64ms frames through the six seconds the dodge mattered
/// most, against 30 the rest of the pull, and nothing could say how much of that was Minerva's.</para>
/// <para>It also profiles frame drops: each frame far longer than those around it is handed back with what it held
/// (<see cref="LastDrop"/>), and every five seconds the steady state (<see cref="LastWindow"/>), for the recording.</para>
/// </summary>
public sealed class FrameCost
{
    /// <summary>The stages of one update tick, in the order the plugin runs them.</summary>
    public enum Stage { Sync, Modules, Dodge, Rest }

    private readonly List<float>[] stages = [[], [], [], []];
    private readonly List<float> totals = [];
    private readonly List<float> draws = [];
    private readonly List<float> gameFrames = [];
    private readonly float[] current = new float[4];
    private float slowest;
    private float[] slowestStages = new float[4];
    private double slowestAt;
    private double elapsed;

    // What the game frame just measured held. A frame's length is known one tick late: it is the time from the last
    // update to this one, which is the last update's own work, the drawing that followed it, and the game's.
    private readonly float[] previous = new float[4];
    private float previousAllocKb;
    private bool hasPrevious;
    private float drawSince;

    // Frame drops: a frame at least twice the median of the 120 before it and at least 25ms over it.
    private const int BaselineFrames = 120;
    private const int BaselineMinimum = 30;
    private const float DropFactor = 2f;
    private const float DropExcessMs = 25f;
    private readonly float[] recent = new float[BaselineFrames];
    private readonly float[] sorted = new float[BaselineFrames];
    private int recentCount;
    private int recentNext;
    private int drops;
    private int dropsMostlyOurs;
    private int dropsMostlyGc;
    private float dropExcessMs;
    private float dropOursMs;
    private float dropGcMs;
    private float worstDropMs;
    private float worstDropOursMs;
    private float worstDropGcMs;
    private double worstDropAt;

    // The steady state between drops, in windows of five seconds.
    private const double WindowSeconds = 5d;
    private double windowElapsed;
    private int windowFrames;
    private float windowFrameMs;
    private readonly float[] windowStages = new float[4];
    private float windowDrawMs;
    private float windowGcMs;
    private int windowGen0;
    private int windowGen1;
    private int windowGen2;
    private float windowAllocKb;

    public int Frames => this.totals.Count;

    /// <summary>The frame the last <see cref="EndTick"/> measured, when it was a drop; otherwise null.</summary>
    public FramePerf? LastDrop { get; private set; }

    /// <summary>The five seconds the last <see cref="EndTick"/> closed, averaged per frame; otherwise null.</summary>
    public FramePerf? LastWindow { get; private set; }

    /// <summary>Time one stage of the tick under way.</summary>
    public void Add(Stage stage, double ms) => this.current[(int)stage] += (float)ms;

    /// <summary>
    /// Close the tick: what it cost in all, and how long the game's frame was. That frame is the one that ended as this
    /// tick began, so the garbage collector's figures are those since the tick before began, for the whole process (every
    /// plugin shares one collector). <paramref name="allocKb"/> is what this tick's own update allocated.
    /// </summary>
    public void EndTick(double gameFrameSeconds, double gcPauseMs = 0d, int gen0 = 0, int gen1 = 0, int gen2 = 0, double allocKb = 0d)
    {
        this.LastDrop = null;
        this.LastWindow = null;
        if (this.hasPrevious)
            this.CloseFrame((float)(gameFrameSeconds * 1000d), (float)gcPauseMs, gen0, gen1, gen2);
        Array.Copy(this.current, this.previous, this.current.Length);
        this.previousAllocKb = (float)allocKb;
        this.hasPrevious = true;
        this.drawSince = 0f;

        var total = 0f;
        for (var i = 0; i < this.current.Length; ++i)
        {
            this.stages[i].Add(this.current[i]);
            total += this.current[i];
        }

        this.totals.Add(total);
        this.gameFrames.Add((float)(gameFrameSeconds * 1000d));
        if (total > this.slowest)
        {
            this.slowest = total;
            this.slowestStages = (float[])this.current.Clone();
            this.slowestAt = this.elapsed;
        }

        this.elapsed += gameFrameSeconds;
        Array.Clear(this.current);
    }

    /// <summary>Drop the tick under way: outside a fight it is not counted.</summary>
    public void DiscardTick()
    {
        Array.Clear(this.current);
        this.hasPrevious = false;
        this.drawSince = 0f;
        this.LastDrop = null;
        this.LastWindow = null;
    }

    /// <summary>The radar and the windows, drawn once a frame apart from the update.</summary>
    public void AddDraw(double ms)
    {
        this.draws.Add((float)ms);
        this.drawSince += (float)ms;
    }

    private void CloseFrame(float frameMs, float gcMs, int gen0, int gen1, int gen2)
    {
        var update = 0f;
        foreach (var ms in this.previous)
            update += ms;
        var ours = update + this.drawSince;

        if (this.recentCount >= BaselineMinimum)
        {
            Array.Copy(this.recent, this.sorted, this.recentCount);
            Array.Sort(this.sorted, 0, this.recentCount);
            var baseline = this.sorted[this.recentCount / 2];
            var excess = frameMs - baseline;
            if (frameMs >= DropFactor * baseline && excess >= DropExcessMs)
            {
                this.LastDrop = new FramePerf(true, 1, frameMs, baseline, this.previous[0], this.previous[1], this.previous[2], this.previous[3],
                    this.drawSince, gcMs, gen0, gen1, gen2, this.previousAllocKb);
                ++this.drops;
                this.dropExcessMs += excess;
                this.dropOursMs += ours;
                this.dropGcMs += gcMs;
                if (ours >= excess / 2f)
                    ++this.dropsMostlyOurs;
                else if (gcMs >= excess / 2f)
                    ++this.dropsMostlyGc;
                if (frameMs > this.worstDropMs)
                {
                    this.worstDropMs = frameMs;
                    this.worstDropOursMs = ours;
                    this.worstDropGcMs = gcMs;
                    this.worstDropAt = this.elapsed;
                }
            }
        }

        this.recent[this.recentNext] = frameMs;
        this.recentNext = (this.recentNext + 1) % BaselineFrames;
        this.recentCount = Math.Min(this.recentCount + 1, BaselineFrames);

        ++this.windowFrames;
        this.windowFrameMs += frameMs;
        for (var i = 0; i < this.previous.Length; ++i)
            this.windowStages[i] += this.previous[i];
        this.windowDrawMs += this.drawSince;
        this.windowGcMs += gcMs;
        this.windowGen0 += gen0;
        this.windowGen1 += gen1;
        this.windowGen2 += gen2;
        this.windowAllocKb += this.previousAllocKb;
        this.windowElapsed += frameMs / 1000d;
        if (this.windowElapsed >= WindowSeconds)
        {
            var n = (float)this.windowFrames;
            this.LastWindow = new FramePerf(false, this.windowFrames, this.windowFrameMs / n, 0f, this.windowStages[0] / n, this.windowStages[1] / n,
                this.windowStages[2] / n, this.windowStages[3] / n, this.windowDrawMs / n, this.windowGcMs, this.windowGen0, this.windowGen1, this.windowGen2,
                this.windowAllocKb / n);
            this.ClearWindow();
        }
    }

    private void ClearWindow()
    {
        this.windowElapsed = 0d;
        this.windowFrames = 0;
        this.windowFrameMs = 0f;
        Array.Clear(this.windowStages);
        this.windowDrawMs = 0f;
        this.windowGcMs = 0f;
        this.windowGen0 = this.windowGen1 = this.windowGen2 = 0;
        this.windowAllocKb = 0f;
    }

    /// <summary>One line for the log, and a fresh start for the next fight. Null when nothing was timed.</summary>
    public string? Summarise()
    {
        if (this.totals.Count == 0)
            return null;

        var line = $"Minerva frame cost over the fight, {this.totals.Count} frames: update median {Median(this.totals):0.00}ms, p95 {P95(this.totals):0.00}ms, max {this.slowest:0.0}ms "
            + $"(sync {P95(this.stages[0]):0.00}, modules {P95(this.stages[1]):0.00}, dodge {P95(this.stages[2]):0.00}, rest {P95(this.stages[3]):0.00} at p95); "
            + $"draw median {Median(this.draws):0.00}ms, p95 {P95(this.draws):0.00}ms, max {Max(this.draws):0.0}ms; "
            + $"the game's frame median {Median(this.gameFrames):0}ms, p95 {P95(this.gameFrames):0}ms. "
            + $"Slowest update {TimeSpan.FromSeconds(this.slowestAt):m\\:ss} in: sync {this.slowestStages[0]:0.0}, modules {this.slowestStages[1]:0.0}, dodge {this.slowestStages[2]:0.0}, rest {this.slowestStages[3]:0.0}ms.";
        line += this.drops == 0
            ? " No frame drops."
            : $" Frame drops (twice the running median and 25ms over it): {this.drops}, {this.dropExcessMs / 1000f:0.0}s lost; of that Minerva's update and drawing "
                + $"{this.dropOursMs / 1000f:0.00}s and the garbage collector {this.dropGcMs / 1000f:0.00}s; mostly Minerva's in {this.dropsMostlyOurs}, mostly the collector's in "
                + $"{this.dropsMostlyGc}, neither in {this.drops - this.dropsMostlyOurs - this.dropsMostlyGc}. Worst {this.worstDropMs:0}ms at {TimeSpan.FromSeconds(this.worstDropAt):m\\:ss} "
                + $"(Minerva {this.worstDropOursMs:0.0}ms, collector {this.worstDropGcMs:0.0}ms).";

        foreach (var s in this.stages)
            s.Clear();
        this.totals.Clear();
        this.draws.Clear();
        this.gameFrames.Clear();
        this.slowest = 0f;
        this.elapsed = 0d;
        this.recentCount = this.recentNext = 0;
        this.drops = this.dropsMostlyOurs = this.dropsMostlyGc = 0;
        this.dropExcessMs = this.dropOursMs = this.dropGcMs = 0f;
        this.worstDropMs = this.worstDropOursMs = this.worstDropGcMs = 0f;
        this.worstDropAt = 0d;
        this.ClearWindow();
        return line;
    }

    private static float Median(List<float> xs) => Quantile(xs, 0.5);

    private static float P95(List<float> xs) => Quantile(xs, 0.95);

    private static float Max(List<float> xs) => xs.Count == 0 ? 0f : xs.Max();

    private static float Quantile(List<float> xs, double q)
    {
        if (xs.Count == 0)
            return 0f;
        var sorted = xs.ToArray();
        Array.Sort(sorted);
        return sorted[Math.Min(sorted.Length - 1, (int)(q * sorted.Length))];
    }
}

/// <summary>
/// One line of the frame profile kept in a recording: a single dropped frame (<paramref name="Drop"/>), or five seconds
/// averaged per frame. The stage times are those of the update that ran inside the frame. The collector's pause and
/// collection counts are the whole process's over the frame (over the window, summed). The allocation is the update's own.
/// </summary>
public readonly record struct FramePerf(bool Drop, int Frames, float FrameMs, float BaselineMs, float SyncMs, float ModulesMs, float DodgeMs, float RestMs,
    float DrawMs, float GcPauseMs, int Gen0, int Gen1, int Gen2, float AllocKb);

/// <summary>
/// <see cref="FramePerf"/> as a replay op, so a recording says what a slow frame was made of long after dalamud.log has
/// rolled over. Applying it changes nothing, and a parser that does not know the tag skips the line.
/// <para>Occult Crescent, 2026-11-19: both clients ran at 20 fps with frames of up to 2.2s, and by the time anyone looked,
/// that evening's end-of-fight cost lines were gone from the log.</para>
/// </summary>
public sealed class OpFramePerf(FramePerf value) : WorldState.Operation
{
    public readonly FramePerf Value = value;

    protected override void Exec(WorldState ws)
    {
    }

    public override void Write(OperationOutput o)
        => o.Tag("PERF").Emit(this.Value.Drop).Emit(this.Value.Frames).Emit(this.Value.FrameMs, "f1").Emit(this.Value.BaselineMs, "f1")
            .Emit(this.Value.SyncMs, "f2").Emit(this.Value.ModulesMs, "f2").Emit(this.Value.DodgeMs, "f2").Emit(this.Value.RestMs, "f2").Emit(this.Value.DrawMs, "f2")
            .Emit(this.Value.GcPauseMs, "f1").Emit(this.Value.Gen0).Emit(this.Value.Gen1).Emit(this.Value.Gen2).Emit(this.Value.AllocKb, "f0");
}
