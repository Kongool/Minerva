using System;
using System.Collections.Generic;
using System.Numerics;
using Minerva.Radar;

namespace Minerva.Replay;

/// <summary>How a previewed cast places its shape — mirrors the generator's component choice.</summary>
public enum PreviewKind
{
    Simple,   // fixed shape at the cast location/caster (SimpleAOEs)
    OnTarget, // circle on the marked player (spread/stack/bait)
    Tether,   // shape on the tethered target (TetherAOEs), else on the caster
}

/// <summary>A previewed cast: the shape to draw and how to place it.</summary>
public readonly record struct PreviewCast(AOEShape Shape, PreviewKind Kind, uint TetherID = 0);

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

    private readonly AIHints aiHints = new();
    private WorldState world;
    private ModuleBase? module;
    private int opIndex;
    private long cursor;

    // draft-preview state: a per-AID classified-cast map to draw instead of a compiled module (see SetPreview)
    private Dictionary<uint, PreviewCast>? previewShapes;
    private WPos previewCenter;
    private ArenaBounds? previewBounds;
    public bool PreviewActive => this.previewShapes != null;

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

    /// <summary>Show a generated draft's AOEs without compiling it: map each cast to its classified shape.</summary>
    public void SetPreview(Dictionary<uint, PreviewCast> shapes, WPos center, ArenaBounds bounds)
    {
        this.previewShapes = shapes;
        this.previewCenter = center;
        this.previewBounds = bounds;
    }

    public void ClearPreview() => this.previewShapes = null;

    /// <summary>Render the current playback frame into a square canvas.</summary>
    public void DrawArena(Vector2 canvasTopLeft, Vector2 canvasSize)
    {
        if (this.previewShapes != null)
        {
            this.DrawPreview(canvasTopLeft, canvasSize);
            return;
        }

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

            if (pc != null)
                this.DrawSafeSpot(pc); // green "stand here" guidance from the module's active AOEs
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

    // preview: draw each active cast's classified shape (what the generated SimpleAOEs/etc. would draw)
    private void DrawPreview(Vector2 topLeft, Vector2 size)
    {
        this.arena.Center = this.previewCenter;
        this.arena.Bounds = this.previewBounds!;
        this.arena.Begin(topLeft, size);
        this.arena.DrawBoundary();

        foreach (var a in this.world.Actors)
        {
            var cast = a.CastInfo;
            if (cast == null || a.IsDeadOrDestroyed)
                continue;
            if (!this.previewShapes!.TryGetValue(cast.Action.ID, out var pc))
                continue;
            switch (pc.Kind)
            {
                case PreviewKind.Simple:
                    var origin = cast.LocXZ != default ? cast.LocXZ : a.Position;
                    this.arena.ZoneShape(pc.Shape, origin, cast.Rotation, Colors.AOE);
                    break;
                case PreviewKind.OnTarget:
                    var target = this.world.Actors.Find(cast.TargetID); // spread/stack follows the marked player
                    if (target != null)
                        this.arena.ZoneShape(pc.Shape, target.Position, default, Colors.AOE);
                    break;
                case PreviewKind.Tether:
                    this.DrawTetherPreview(a, pc.Shape, pc.TetherID);
                    break;
            }
        }

        foreach (var a in this.world.Actors)
        {
            if (a.IsDeadOrDestroyed)
                continue;
            if (a.Type == ActorType.Enemy)
                this.arena.ActorMarker(a.Position, a.Rotation, MathF.Max(a.HitboxRadius, 0.5f), Colors.Enemy);
            else if (a.Type == ActorType.Player)
                this.arena.ActorMarker(a.Position, a.Rotation, MathF.Max(a.HitboxRadius, 0.5f), Colors.PC);
        }
    }

    // a tether-driven AOE lands on the tethered target(s); if the tether already resolved, it erupts on the caster
    private void DrawTetherPreview(Actor caster, AOEShape shape, uint tetherID)
    {
        var drew = false;
        foreach (var src in this.world.Actors)
        {
            if (src.Tether.ID != tetherID)
                continue;
            var tgt = this.world.Actors.Find(src.Tether.Target);
            if (tgt != null)
            {
                this.arena.ZoneShape(shape, tgt.Position, tgt.Rotation, Colors.AOE);
                drew = true;
            }
        }
        if (!drew)
            this.arena.ZoneShape(shape, caster.Position, caster.Rotation, Colors.AOE);
    }

    // run the auto-dodge solver on the module's active AOEs and mark the nearest safe cell in green
    private void DrawSafeSpot(Actor pc)
    {
        if (this.module == null)
            return;
        this.module.BuildAIHints(0, pc, this.aiHints);
        var solve = ArenaPathfinder.Solve(this.aiHints, this.world.CurrentTime);
        if (!solve.NeedToMove || !solve.Found)
            return;
        this.arena.AddLine(pc.Position, solve.Target, Colors.Safe, 3f);
        this.arena.AddCircleFilled(solve.Target, 0.8f, Colors.Safe);
        this.arena.AddCircle(solve.Target, 0.8f, Colors.PC, 2f);
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
