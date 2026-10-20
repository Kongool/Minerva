namespace Minerva;

/// <summary>
/// What Minerva costs the game per frame over one fight, stage by stage, for a line in the log when the fight ends.
/// <para>The offline probes time the solver over a recording, but only the plugin can time itself inside the game, next
/// to the game's own frame. Elm Gigas, 2026-10-19: the game ran 64ms frames through the six seconds the dodge mattered
/// most, against 30 the rest of the pull, and nothing could say how much of that was Minerva's.</para>
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

    public int Frames => this.totals.Count;

    /// <summary>Time one stage of the tick under way.</summary>
    public void Add(Stage stage, double ms) => this.current[(int)stage] += (float)ms;

    /// <summary>Close the tick: what it cost in all, and how long the game's frame was.</summary>
    public void EndTick(double gameFrameSeconds)
    {
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
    public void DiscardTick() => Array.Clear(this.current);

    /// <summary>The radar and the windows, drawn once a frame apart from the update.</summary>
    public void AddDraw(double ms) => this.draws.Add((float)ms);

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

        foreach (var s in this.stages)
            s.Clear();
        this.totals.Clear();
        this.draws.Clear();
        this.gameFrames.Clear();
        this.slowest = 0f;
        this.elapsed = 0d;
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
