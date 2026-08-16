namespace Minerva;

/// <summary>A danger area the AI should keep out of, resolving at <see cref="Activation"/>.</summary>
public readonly struct ForbiddenZone(AOEShape shape, WPos origin, Angle rotation, DateTime activation)
{
    public readonly AOEShape Shape = shape;
    public readonly WPos Origin = origin;
    public readonly Angle Rotation = rotation;
    public readonly DateTime Activation = activation;

    public bool Contains(WPos p) => this.Shape.Check(p, this.Origin, this.Rotation);
}

/// <summary>
/// Per-frame decision input for the auto-dodge engine: where the player is, the arena, and the
/// danger zones the active module's components contributed. Rebuilt each frame (see
/// <see cref="Clear"/>) and consumed by <see cref="ArenaPathfinder"/>. Game-free and testable.
/// </summary>
public sealed class AIHints
{
    public WPos PlayerPosition;
    public WPos Center;
    public ArenaBounds Bounds = new ArenaBoundsCircle(20f);
    public readonly List<ForbiddenZone> ForbiddenZones = [];

    public void Clear()
    {
        this.ForbiddenZones.Clear();
    }

    public void AddForbiddenZone(AOEShape shape, WPos origin, Angle rotation, DateTime activation)
        => this.ForbiddenZones.Add(new ForbiddenZone(shape, origin, rotation, activation));

    public void AddForbiddenZone(in AOEInstance aoe)
        => this.ForbiddenZones.Add(new ForbiddenZone(aoe.Shape, aoe.Origin, aoe.Rotation, aoe.Activation));

    /// <summary>Is a point inside any zone that resolves at or before <paramref name="deadline"/>?</summary>
    public bool InImminentDanger(WPos p, DateTime deadline)
    {
        foreach (var z in this.ForbiddenZones)
            if (z.Activation <= deadline && z.Contains(p))
                return true;
        return false;
    }

    /// <summary>
    /// Like <see cref="InImminentDanger(WPos, DateTime)"/>, but also rejects a point within <paramref name="margin"/>
    /// of a danger zone (sampled on a ring around it). Used to pick a dodge target that keeps clearance from the
    /// AOE edge — accounting for hitbox, reaction, and stopping distance — instead of landing right against it.
    /// </summary>
    public bool InImminentDanger(WPos p, DateTime deadline, float margin)
    {
        if (this.InImminentDanger(p, deadline))
            return true;
        if (margin > 0f)
            for (var i = 0; i < 8; ++i)
            {
                var dir = new Angle(i * (Angle.TwoPI / 8f)).ToDirection();
                if (this.InImminentDanger(p + dir * margin, deadline))
                    return true;
            }
        return false;
    }
}
