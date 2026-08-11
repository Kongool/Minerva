using System;
using System.Numerics;
using Minerva.Radar;

namespace Minerva.Replay;

/// <summary>
/// Drives interactive playback of a recorded fight. It steps a <see cref="ReplayTimeline"/> through a
/// private <see cref="WorldState"/> on a real-time cursor (play/pause/speed/seek), activating the
/// matching boss module against that world so real AOE shapes and boundaries replay exactly as they
/// did live. Seeking rebuilds from the start to the target (ops are cheap and forward-only). The
/// <see cref="ReplayWindow"/> renders it; this class owns the world, module, cursor, and arena.
/// </summary>
public sealed class ReplayPlayer : IDisposable
{
    private readonly ReplayTimeline timeline;
    private readonly ModuleRegistry registry;
    private readonly ImGuiArena arena = new();

    private WorldState world;
    private ModuleBase? module;
    private int opIndex;
    private long cursor;

    public bool Playing { get; private set; }
    public float Speed = 1f;

    public ReplayPlayer(ReplayTimeline timeline, ModuleRegistry registry)
    {
        this.timeline = timeline;
        this.registry = registry;
        this.world = new WorldState(timeline.QPF, timeline.GameVersion);
        this.cursor = timeline.StartTicks;
        this.ApplyUpTo(this.cursor); // apply the opening snapshot so the first frame is visible paused
    }

    public double DurationSeconds => this.timeline.DurationTicks / (double)TimeSpan.TicksPerSecond;
    public double PositionSeconds => Math.Max(0d, (this.cursor - this.timeline.StartTicks) / (double)TimeSpan.TicksPerSecond);
    public float Progress => this.timeline.DurationTicks > 0 ? (float)Math.Clamp((this.cursor - this.timeline.StartTicks) / (double)this.timeline.DurationTicks, 0d, 1d) : 0f;
    public bool AtEnd => this.opIndex >= this.timeline.Ops.Count;
    public int OpCount => this.timeline.Ops.Count;
    public ushort ReplayCFC => this.world.CurrentCFCID;
    public string ModuleName => this.module != null ? this.module.GetType().Name : $"no module for cfc {this.world.CurrentCFCID} — actors + casts";

    public void Play()
    {
        if (this.AtEnd)
            this.Rebuild(this.timeline.StartTicks); // replaying from the end restarts
        this.Playing = true;
    }

    public void Pause() => this.Playing = false;
    public void TogglePlay() { if (this.Playing) this.Pause(); else this.Play(); }
    public void Restart() => this.Rebuild(this.timeline.StartTicks);

    public void Update(TimeSpan realDt)
    {
        if (!this.Playing || this.timeline.Ops.Count == 0)
            return;
        this.cursor += (long)(realDt.Ticks * Math.Max(0.05f, this.Speed));
        this.ApplyUpTo(this.cursor);
        this.module?.Update();
        if (this.AtEnd)
        {
            this.cursor = this.timeline.EndTicks;
            this.Playing = false;
        }
    }

    /// <summary>Jump to <paramref name="fraction"/> (0..1) of the fight; rebuilds the world to that moment.</summary>
    public void Seek(float fraction)
    {
        var target = this.timeline.StartTicks + (long)(Math.Clamp(fraction, 0f, 1f) * this.timeline.DurationTicks);
        this.Rebuild(target);
    }

    private void Rebuild(long target)
    {
        this.module?.Dispose();
        this.module = null;
        this.world = new WorldState(this.timeline.QPF, this.timeline.GameVersion);
        this.opIndex = 0;
        this.cursor = target;
        this.ApplyUpTo(target);
        this.module?.Update();
    }

    private void ApplyUpTo(long ticks)
    {
        var ops = this.timeline.Ops;
        while (this.opIndex < ops.Count && ops[this.opIndex].Ticks <= ticks)
        {
            this.world.Execute(ops[this.opIndex].Op);
            this.opIndex++;
            this.TryActivateModule(); // must be active before its casts replay so components catch them
        }
    }

    private void TryActivateModule()
    {
        if (this.module != null)
        {
            if (this.module.PrimaryActor.IsDestroyed)
            {
                this.module.Dispose();
                this.module = null;
            }
            else
            {
                return;
            }
        }
        if (this.world.CurrentCFCID == 0)
            return;
        foreach (var info in this.registry.ForCFC(this.world.CurrentCFCID))
            foreach (var actor in this.world.Actors)
                if (actor.OID == info.PrimaryActorOID && !actor.IsDestroyed)
                {
                    this.module = info.Create(this.world, actor);
                    return;
                }
    }

    /// <summary>Render the current playback frame into a square canvas.</summary>
    public void DrawArena(Vector2 canvasTopLeft, Vector2 canvasSize)
    {
        var pc = this.FindPlayer();
        if (this.module != null)
        {
            this.arena.Center = this.module.Center;
            this.arena.Bounds = this.module.Bounds;
            this.arena.Begin(canvasTopLeft, canvasSize);
            this.module.Arena = this.arena;
            this.module.DrawArena(0, pc ?? this.module.PrimaryActor); // boundary + enemies + live AOEs

            foreach (var a in this.world.Actors)
                if (a.Type == ActorType.Player && !a.IsDeadOrDestroyed)
                    this.arena.ActorMarker(a.Position, a.Rotation, MathF.Max(a.HitboxRadius, 0.5f), Colors.PC);
            return;
        }

        // no authored module: a personal-radar view centred on the fight at a usable zoom, clipping far
        // actors so nothing spills outside the circle (there's no real arena to draw here)
        var (center, radius) = this.EstimateArena();
        this.arena.Center = center;
        this.arena.Bounds = new ArenaBoundsCircle(radius);
        this.arena.Begin(canvasTopLeft, canvasSize);
        this.arena.DrawBoundary();

        this.DrawCasts(center, radius);
        foreach (var a in this.world.Actors)
        {
            if (a.IsDeadOrDestroyed || !a.Position.InCircle(center, radius))
                continue;
            if (a.Type == ActorType.Enemy)
                this.arena.ActorMarker(a.Position, a.Rotation, MathF.Max(a.HitboxRadius, 0.5f), Colors.Enemy);
            else if (a.Type == ActorType.Player)
                this.arena.ActorMarker(a.Position, a.Rotation, MathF.Max(a.HitboxRadius, 0.5f), Colors.PC);
        }
    }

    // without an authored module we don't know AOE shapes, but we can still show a cast is happening and
    // where it's aimed: a filled marker + ring at the aim point and a line from the caster. Within view only.
    private void DrawCasts(WPos center, float radius)
    {
        foreach (var a in this.world.Actors)
        {
            var cast = a.CastInfo;
            if (cast == null || a.IsDeadOrDestroyed)
                continue;
            var aim = cast.LocXZ != default ? cast.LocXZ : a.Position;
            if (!aim.InCircle(center, radius))
                continue;
            this.arena.AddCircleFilled(aim, 3f, Colors.AOE);
            this.arena.AddCircle(aim, 3f, Colors.Danger, 2f);
            if (aim != a.Position)
                this.arena.AddLine(a.Position, aim, Colors.Danger, 1.5f);
        }
    }

    private Actor? FindPlayer()
    {
        foreach (var a in this.world.Actors)
            if (a.Type == ActorType.Player && !a.IsDeadOrDestroyed)
                return a;
        return null;
    }

    // no authored module = no real arena. Centre on the fight (biggest enemy, else a player) at a fixed,
    // usable zoom — a personal-radar view. Far actors are clipped by the caller, not shrunk into it.
    private const float ViewRadius = 25f;

    private (WPos center, float radius) EstimateArena()
    {
        Actor? focus = null;
        foreach (var a in this.world.Actors)
            if (a.Type == ActorType.Enemy && !a.IsDeadOrDestroyed && (focus == null || a.HitboxRadius > focus.HitboxRadius))
                focus = a;
        focus ??= this.FindPlayer();
        return (focus?.Position ?? default, ViewRadius);
    }

    public void Dispose()
    {
        this.module?.Dispose();
        this.module = null;
    }
}
