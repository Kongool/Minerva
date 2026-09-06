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

    private readonly WorldState world;
    private readonly IShapeResolver shapes;
    private readonly Dictionary<ulong, Pending> active = [];

    private readonly record struct Pending(AOEShape Shape, WPos Origin, Angle Rotation, DateTime Activation);

    public AutoHints(WorldState world, IShapeResolver shapes)
    {
        this.world = world;
        this.shapes = shapes;
        world.Actors.CastStarted.Subscribe(this.OnCastStarted);
        world.Actors.CastFinished.Subscribe((a, _) => this.active.Remove(a.InstanceID));
        world.Actors.Removed.Subscribe(a => this.active.Remove(a.InstanceID));
    }

    /// <summary>Casts currently being tracked — lets a caller skip the pathfinder entirely when idle.</summary>
    public int Count => this.active.Count;

    private void OnCastStarted(Actor caster)
    {
        if (caster.CastInfo is not { } cast || cast.Action.ID == 0)
            return;
        if (caster.Type is not (ActorType.Enemy or ActorType.Helper) || caster.IsAlly)
            return;

        var hint = this.shapes.Resolve(cast.Action.ID);
        if (hint.Kind is ShapeKind.Unknown or ShapeKind.SingleTarget)
            return; // nothing aimed at the ground: a tankbuster or a heal, not something to walk out of
        if (hint.Kind == ShapeKind.Circle && hint.Radius >= RaidwideRadius)
            return;

        // a large enemy's cone or line starts at its edge, not its centre
        var reach = hint.Kind is ShapeKind.Cone or ShapeKind.Rect ? caster.HitboxRadius : 0f;
        if (hint with { Radius = hint.Radius + reach } is var sized && sized.ToShape() is { } shape)
        {
            var origin = cast.LocXZ != default ? cast.LocXZ : caster.Position;
            this.active[caster.InstanceID] = new Pending(shape, origin, cast.Rotation, this.world.FutureTime(cast.RemainingTime));
        }
    }

    /// <summary>Add every tracked cast to the hints as a forbidden zone.</summary>
    public void AddForbiddenZones(AIHints hints)
    {
        foreach (var p in this.active.Values)
            hints.AddForbiddenZone(p.Shape, p.Origin, p.Rotation, p.Activation);
    }
}
