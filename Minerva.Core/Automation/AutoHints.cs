using System;
using System.Collections.Generic;
using Minerva.Generation;

namespace Minerva.Automation;

/// <summary>
/// Avoidance for content nobody has written a module for — trash packs, open world, anything unscripted.
/// It watches enemy cast bars and derives each one's shape from the game's own action data, then feeds the
/// result into the same <see cref="AIHints"/> a boss module would write to, so the pathfinder cannot tell
/// the difference.
/// <para>An authored module always wins: it knows the fight, this only knows the sheet. The caller is
/// expected to use this only while no module is active.</para>
/// <para>This is a guess, and it is deliberately a narrow one. A cast the sheet cannot describe is skipped
/// rather than approximated, and a raidwide-sized circle is skipped too — there is nowhere to stand, so
/// marking the world forbidden would only make the dodge sprint pointlessly.</para>
/// </summary>
public sealed class AutoHints
{
    /// <summary>A point-blank circle at least this wide is a raidwide: no amount of running clears it.</summary>
    public const float RaidwideRadius = 30f;

    /// <summary>One cast being tracked: what was drawn for it, where, and when it lands.</summary>
    public readonly record struct Zone(AOEShape Shape, WPos Origin, Angle Rotation, DateTime Activation, uint Action);

    /// <summary>One cast we cannot dodge but can look away from: the caster, and when it resolves.</summary>
    public readonly record struct Gaze(ulong Caster, DateTime Activation, uint Action);

    private readonly WorldState world;
    private readonly IShapeResolver shapes;
    private readonly INameResolver? names;
    private readonly Dictionary<ulong, Zone> active = [];
    private readonly Dictionary<ulong, Gaze> gazes = [];

    public AutoHints(WorldState world, IShapeResolver shapes)
    {
        this.world = world;
        this.shapes = shapes;
        this.names = shapes as INameResolver;
        world.Actors.CastStarted.Subscribe(this.OnCastStarted);
        world.Actors.CastFinished.Subscribe((a, _) =>
        {
            this.active.Remove(a.InstanceID);
            this.gazes.Remove(a.InstanceID);
        });
        world.Actors.Removed.Subscribe(a =>
        {
            this.active.Remove(a.InstanceID);
            this.gazes.Remove(a.InstanceID);
        });
    }

    /// <summary>Casts currently being tracked — lets a caller skip the pathfinder entirely when idle.</summary>
    public int Count => this.active.Count;

    /// <summary>The zones being tracked right now, for a replay or a radar to show what the guess was.</summary>
    public IEnumerable<Zone> Active => this.active.Values;

    /// <summary>Casts being watched as gazes: nothing to walk out of, something to look away from.</summary>
    public IEnumerable<Gaze> Gazes => this.gazes.Values;

    /// <summary>How many gazes are being watched; a caller with neither zones nor gazes can skip the solve.</summary>
    public int GazeCount => this.gazes.Count;

    /// <summary>
    /// Words that name a gaze, from the 71 actions the ported modules' gaze components watch. A name is
    /// weak evidence on its own, which is why it is only consulted for a cast that is <i>not</i> being
    /// drawn: the character is not dodging that cast either way, so turning away costs a fraction of a
    /// second of facing and can only help. A drawn zone (Eye of the Storm and its kind) never reaches here.
    /// </summary>
    private static readonly string[] GazeWords = ["gaze", "glare", "stare", "eye", "petrif", "hypnot", "mesmer", "sight", "nightmare", "look"];

    /// <summary>Does this action's name read like a gaze?</summary>
    public static bool LooksLikeGaze(string? actionName)
    {
        if (string.IsNullOrEmpty(actionName))
            return false;
        for (var i = 0; i < GazeWords.Length; ++i)
            if (actionName.Contains(GazeWords[i], StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    /// <summary>
    /// The one rule for what gets drawn: a ground shape the sheet describes, smaller than a raidwide.
    /// Shared with the replay validator so an offline verdict uses the same test as the live guess.
    /// </summary>
    public static bool Draws(ShapeHint hint)
        => hint.Kind is not (ShapeKind.Unknown or ShapeKind.SingleTarget)
            && !(hint.Kind == ShapeKind.Circle && hint.Radius >= RaidwideRadius);

    private void OnCastStarted(Actor caster)
    {
        if (caster.CastInfo is not { } cast || cast.Action.ID == 0)
            return;
        if (caster.Type is not (ActorType.Enemy or ActorType.Helper) || caster.IsAlly)
            return;

        var hint = this.shapes.Resolve(cast.Action.ID);
        if (!Draws(hint))
        {
            // Nothing to walk out of -- but a gaze is answered by facing, not by feet, and in unscripted
            // content nothing else will ever say so. Orthos, 2026-09-06: Double Hex Eye is a 40-yalm circle
            // (dropped as a raidwide, correctly) that stuns everyone looking at it; the stun then held the
            // character still through the next AOE and killed it.
            if (LooksLikeGaze(this.names?.ActionName(cast.Action.ID)))
                this.gazes[caster.InstanceID] = new Gaze(caster.InstanceID, this.world.FutureTime(cast.RemainingTime), cast.Action.ID);
            return;
        }

        // a large enemy's cone or line starts at its edge, not its centre
        var reach = hint.Kind is ShapeKind.Cone or ShapeKind.Rect ? caster.HitboxRadius : 0f;
        if (hint with { Radius = hint.Radius + reach } is var sized && sized.ToShape() is { } shape)
        {
            var origin = cast.LocXZ != default ? cast.LocXZ : caster.Position;
            this.active[caster.InstanceID] = new Zone(shape, origin, cast.Rotation, this.world.FutureTime(cast.RemainingTime), cast.Action.ID);
        }
    }

    /// <summary>Add every tracked cast to the hints as a forbidden zone.</summary>
    public void AddForbiddenZones(AIHints hints)
    {
        foreach (var p in this.active.Values)
            hints.AddForbiddenZone(p.Shape, p.Origin, p.Rotation, p.Activation);
    }

    /// <summary>Forbid facing each suspected gaze's caster, the same arc a module's gaze component uses.</summary>
    public void AddForbiddenDirections(AIHints hints, WPos playerPos)
    {
        foreach (var g in this.gazes.Values)
        {
            if (this.world.Actors.Find(g.Caster) is not { IsDeadOrDestroyed: false } caster)
                continue;
            var away = caster.Position - playerPos;
            if (away.LengthSq() < 0.01f)
                continue;
            hints.ForbiddenDirections.Add((Angle.FromDirection(away), 45f.Degrees(), g.Activation));
        }
    }
}
