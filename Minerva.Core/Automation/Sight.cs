using System;
using System.Collections.Generic;
using System.Numerics;

namespace Minerva;

/// <summary>
/// Walls. A few casts are stopped by one (deep dungeon trash that takes 16 seconds over a 46-yalm circle), and only the
/// running game knows where the walls are: the plugin sets <see cref="Blocked"/>. Offline nothing sets it, and nothing
/// counts as hidden.
/// </summary>
public static class Sight
{
    /// <summary>Is there a wall between these two places? World positions at the feet.</summary>
    public static Func<Vector3, Vector3, bool>? Blocked;

    /// <summary>Where a cast's verdict on each party member is written as it ends, to hold against who was hit.</summary>
    public static Action<string>? Log;
}

/// <summary>
/// What one caster cannot see. Each yalm of floor is asked once and kept: the caster stands still for its cast, and the
/// solver measures a zone at every cell of its grid, several grids a solve.
/// </summary>
public sealed class SightShadow
{
    private readonly Func<Vector3, Vector3, bool> blocked;
    private readonly Vector3 eye;
    private readonly Dictionary<(int X, int Z), bool> cells = [];

    public SightShadow(Func<Vector3, Vector3, bool> blocked, Vector3 eye)
    {
        this.blocked = blocked;
        this.eye = eye;
        // A caster walled in a yalm away on every side is a wall check that answers yes to everything, and believing it
        // would call the whole floor hidden. Not trusted, the cast is a plain circle.
        this.Trusted = !(blocked(eye, eye + new Vector3(1f, 0f, 0f)) && blocked(eye, eye - new Vector3(1f, 0f, 0f))
            && blocked(eye, eye + new Vector3(0f, 0f, 1f)) && blocked(eye, eye - new Vector3(0f, 0f, 1f)));
    }

    public bool Trusted { get; }

    /// <summary>How many yalms of floor have been asked about.</summary>
    public int Asked => this.cells.Count;

    /// <summary>
    /// Hidden here, and for a yalm every way round: where the shadow of a wall ends is only known to the yalm, and a
    /// toon is not a point.
    /// </summary>
    public bool Hides(WPos p, float y)
    {
        var x = (int)MathF.Round(p.X);
        var z = (int)MathF.Round(p.Z);
        for (var dx = -1; dx <= 1; ++dx)
            for (var dz = -1; dz <= 1; ++dz)
                if (!this.CellHidden(x + dx, z + dz, y))
                    return false;
        return true;
    }

    private bool CellHidden(int x, int z, float y)
    {
        if (!this.cells.TryGetValue((x, z), out var hidden))
            this.cells[(x, z)] = hidden = this.blocked(this.eye, new Vector3(x, y, z));
        return hidden;
    }
}

/// <summary>A circle that hits only what its caster can see: ground behind a wall is clear of it.</summary>
public sealed class SDCircleInSight(WPos origin, float radius, SightShadow shadow, float y) : ShapeDistance
{
    /// <summary>The distance given for hidden ground: clear by more than any margin asked for.</summary>
    private const float Clear = 1000f;

    public override float Distance(WPos p)
    {
        var d = (p - origin).Length() - radius;
        return d < Clear && shadow.Hides(p, y) ? Clear : d;
    }

    public override (WPos Center, float Radius)? Bounds() => (origin, radius);
}

/// <summary>
/// The casts of a zone that a wall stops: long casts over a circle too wide to walk out of, which the cast-bar guess
/// leaves alone as raidwides. Each is drawn as its circle less the ground a wall hides from the caster, so the dodge
/// walks behind a wall or out of reach, whichever is nearer. Where walls cannot be asked about it is the plain circle.
/// </summary>
public sealed class SightCasts : IDisposable
{
    /// <summary>
    /// One cast: how far it reaches from the caster's edge, and whether it is aimed at one player (only that player is
    /// in danger, and its reach runs to that player's edge) rather than at everyone round the caster.
    /// </summary>
    public readonly record struct Entry(float Reach, bool OnTarget = false);

    private readonly WorldState world;
    private readonly IReadOnlyDictionary<uint, Entry> radii;
    private readonly Dictionary<ulong, SightShadow?> casters = [];
    private readonly EventSubscriptions subscriptions;

    public SightCasts(WorldState world, IReadOnlyDictionary<uint, Entry> radii)
    {
        this.world = world;
        this.radii = radii;
        this.subscriptions = new(
            world.Actors.CastStarted.Subscribe(actor =>
            {
                if (actor.CastInfo is { } cast && radii.ContainsKey(cast.Action.ID))
                    this.casters[actor.InstanceID] = Sight.Blocked is { } blocked ? new SightShadow(blocked, new(actor.PosRot.X, actor.PosRot.Y, actor.PosRot.Z)) : null;
            }),
            world.Actors.CastFinished.Subscribe((actor, cast) =>
            {
                if (this.casters.Remove(actor.InstanceID, out var shadow) && Sight.Log is { } log)
                    log(this.Verdict(actor, cast, shadow));
            }),
            world.Actors.Removed.Subscribe(actor => this.casters.Remove(actor.InstanceID)));
    }

    public bool Replaces(uint action) => this.radii.ContainsKey(action);

    public void AddForbiddenZones(Actor player, AIHints hints)
    {
        foreach (var (id, shadow) in this.casters)
        {
            if (this.world.Actors.Find(id) is not { IsDeadOrDestroyed: false, CastInfo: { } cast } caster || !this.radii.TryGetValue(cast.Action.ID, out var entry))
                continue;
            if (entry.OnTarget && cast.TargetID != player.InstanceID)
                continue;
            var radius = entry.Reach + caster.HitboxRadius + (entry.OnTarget ? player.HitboxRadius : 0f);
            var lands = this.world.FutureTime(cast.RemainingTime);
            if (shadow is { Trusted: true })
                hints.AddForbiddenZone(new SDCircleInSight(caster.Position, radius, shadow, player.PosRot.Y), lands, id);
            else
                hints.AddForbiddenZone(new AOEShapeCircle(radius), caster.Position, default, lands, id);
        }
    }

    /// <summary>
    /// What the wall check made of each party member as the cast ended. The check cannot be tried offline, so this is
    /// how a live cast is held against the recording's list of who was hit.
    /// </summary>
    private string Verdict(Actor caster, ActorCastInfo cast, SightShadow? shadow)
    {
        var walls = shadow == null ? "walls not asked" : shadow.Trusted ? $"{shadow.Asked} yalms asked" : "wall check not trusted";
        var line = $"Minerva: {caster.Name}'s cast {cast.Action.ID} ended ({walls}).";
        foreach (var member in this.world.Party.WithoutSlot(includeDead: true))
        {
            var where = shadow is { Trusted: true } && shadow.Hides(member.Position, member.PosRot.Y) ? "behind a wall" : "in sight";
            line += $" {member.Name}: {(member.Position - caster.Position).Length():0.0}y, {where}.";
        }
        return line;
    }

    public void Dispose() => this.subscriptions.Dispose();
}
