namespace Minerva.Components;

/// <summary>
/// Draws the adds of a given OID, exposes them via <see cref="ActiveActors"/>, and ranks them for targeting. Ported from
/// BossmodReborn's Adds (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// <para>The ranking was left out when the auto-dodge picked no targets, and stayed out after Minerva began publishing
/// <c>Minerva.Hints.PriorityTargets</c> for the rotation plugin: every module's "kill these first" written as
/// <c>Adds(…, priority)</c> or <c>AddsMulti(…, priority)</c> did nothing. Ogre's Wisps, 2026-10-25: two or three a wave
/// reached the boss, a Vulnerability Up stack each, while the Great Wisp was being hit.</para>
/// </summary>
public class Adds(ModuleBase module, uint oid, int priority = 0, bool forbidDots = false) : ModuleComponent(module)
{
    /// <summary>Which add to track. Named <c>AddOID</c>, not <c>OID</c>: every module declares a namespace-level
    /// <c>OID</c> enum, and an inherited member of that name shadows it inside every derived component, so
    /// <c>Components.Adds(module, (uint)OID.Whatever)</c> stops compiling in 37 files.</summary>
    public readonly uint AddOID = oid;
    public readonly int Priority = priority;
    public readonly bool ForbidDots = forbidDots;

    /// <summary>Every tracked add, alive or not — BossmodReborn's name for the list, which its modules
    /// iterate directly to draw or measure. Distinct from <see cref="ActiveActors"/>, which filters to
    /// the ones a mechanic can still involve.</summary>
    public List<Actor> Actors => this.Module.Enemies(this.AddOID);

    public List<Actor> ActiveActors
    {
        get
        {
            var result = new List<Actor>();
            foreach (var a in this.Module.Enemies(this.AddOID))
                if (a.IsTargetable && !a.IsDeadOrDestroyed)
                    result.Add(a);
            return result;
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
        => hints.PrioritizeTargetsByOIDAndForbidDOTs(this.AddOID, this.Priority, this.ForbidDots);

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var a in this.Module.Enemies(this.AddOID))
            if (!a.IsDeadOrDestroyed)
                this.Arena.ActorMarker(a.Position, a.Rotation, a.HitboxRadius, Colors.Enemy);
    }
}

/// <summary>Adds that shouldn't be targeted but should still be drawn.</summary>
public class AddsPointless(ModuleBase module, uint oid) : Adds(module, oid)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        foreach (var a in this.Actors)
            hints.SetPriority(a, AIHints.Enemy.PriorityPointless);
    }
}

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

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (this.Priority != 0)
            hints.PrioritizeTargetsByOID(this.OIDs, this.Priority);
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var id in this.OIDs)
            foreach (var a in this.Module.Enemies(id))
                if (!a.IsDeadOrDestroyed)
                    this.Arena.ActorMarker(a.Position, a.Rotation, a.HitboxRadius, Colors.Enemy);
    }
}
