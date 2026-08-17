namespace Minerva.Components;

/// <summary>
/// Draws the adds of a given OID and exposes them via <see cref="ActiveActors"/>. Ported from
/// BossmodReborn's Adds (BSD-3; see THIRD-PARTY-NOTICES.txt); Minerva omits BMR's AI target-priority
/// weighting (its auto-dodge does not pick targets), keeping just the radar drawing.
/// </summary>
public class Adds(ModuleBase module, uint oid, int priority = 0, bool forbidDots = false) : ModuleComponent(module)
{
    public readonly uint OID = oid;
    public readonly int Priority = priority;
    public readonly bool ForbidDots = forbidDots;

    public List<Actor> ActiveActors
    {
        get
        {
            var result = new List<Actor>();
            foreach (var a in this.Module.Enemies(this.OID))
                if (a.IsTargetable && !a.IsDeadOrDestroyed)
                    result.Add(a);
            return result;
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var a in this.Module.Enemies(this.OID))
            if (!a.IsDeadOrDestroyed)
                this.Arena.ActorMarker(a.Position, a.Rotation, a.HitboxRadius, Colors.Enemy);
    }
}

/// <summary>Adds that shouldn't be targeted but should still be drawn.</summary>
public class AddsPointless(ModuleBase module, uint oid) : Adds(module, oid);

/// <summary>Draws adds of several OIDs when distinguishing them isn't useful. Ported from BossmodReborn (BSD-3).</summary>
public class AddsMulti(ModuleBase module, uint[] oids, int priority = 0) : ModuleComponent(module)
{
    public readonly uint[] OIDs = oids;
    public readonly int Priority = priority;

    public List<Actor> ActiveActors
    {
        get
        {
            var result = new List<Actor>();
            foreach (var id in this.OIDs)
                foreach (var a in this.Module.Enemies(id))
                    if (a.IsTargetable && !a.IsDeadOrDestroyed)
                        result.Add(a);
            return result;
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var id in this.OIDs)
            foreach (var a in this.Module.Enemies(id))
                if (!a.IsDeadOrDestroyed)
                    this.Arena.ActorMarker(a.Position, a.Rotation, a.HitboxRadius, Colors.Enemy);
    }
}
