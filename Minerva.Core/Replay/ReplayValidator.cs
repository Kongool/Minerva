using System;
using System.Collections.Generic;
using System.Text;
using Minerva.Generation;

namespace Minerva;

/// <summary>
/// Replays a recording through the matching boss module and reports how well the module covers the
/// fight — which enemy casts it actually drew an AOE for, which it merely hints/warns about, and which
/// it ignores entirely (candidates for a missing component). This is the automated round-trip check
/// BMR authors do by eye. Fully offline: it only needs the timeline + the module registry.
/// </summary>
public sealed class ReplayValidator
{
    /// <summary>
    /// Per-fight coverage: <see cref="Drawn"/> produced an AOE, <see cref="Hinted"/> is watched by a
    /// non-AOE component (raidwide/tankbuster/gaze/…), <see cref="Uncovered"/> was cast but the module
    /// did nothing (single-target, or a mechanic you haven't handled yet).
    /// </summary>
    public sealed record Result(
        string ModuleName, int EnemyActions,
        IReadOnlyList<uint> Drawn, IReadOnlyList<uint> Hinted,
        IReadOnlyList<uint> UncoveredMechanics, IReadOnlyList<uint> UncoveredVisuals,
        string? ArenaNote)
    {
        public int Uncovered => this.UncoveredMechanics.Count + this.UncoveredVisuals.Count;

        public string Render(INameResolver? names = null)
        {
            string Join(IReadOnlyList<uint> aids)
            {
                var parts = new List<string>(aids.Count);
                foreach (var a in aids)
                    parts.Add(names?.ActionName(a) is { Length: > 0 } n ? $"{n} ({a})" : a.ToString());
                return string.Join(", ", parts);
            }

            var b = new StringBuilder();
            b.AppendLine($"Validation vs recording — module: {this.ModuleName}");
            b.AppendLine($"Enemy actions cast: {this.EnemyActions}   drawn: {this.Drawn.Count}   hinted: {this.Hinted.Count}   uncovered: {this.Uncovered}");
            if (this.Drawn.Count > 0)
                b.AppendLine("  drawn AOE:  " + Join(this.Drawn));
            if (this.Hinted.Count > 0)
                b.AppendLine("  hint only:  " + Join(this.Hinted));
            // helper casts are only ever mechanics, so an uncovered one is a likely miss; boss self-casts are usually just visuals
            if (this.UncoveredMechanics.Count > 0)
                b.AppendLine("  UNCOVERED (helper-cast — likely a missed mechanic): " + Join(this.UncoveredMechanics));
            if (this.UncoveredVisuals.Count > 0)
                b.AppendLine("  uncovered (boss visual / single-target — usually fine): " + Join(this.UncoveredVisuals));
            if (this.ArenaNote != null)
                b.AppendLine("  " + this.ArenaNote);
            return b.ToString();
        }
    }

    public static Result Validate(ReplayTimeline timeline, ModuleRegistry registry)
    {
        var world = new WorldState(timeline.QPF, timeline.GameVersion);
        ModuleBase? module = null;
        var watched = new HashSet<uint>();
        var castCount = new SortedDictionary<uint, int>();
        var drawn = new HashSet<uint>();
        var helperCast = new HashSet<uint>(); // AIDs cast by Helpers (0x233C) — only ever mechanics
        var initialRadius = 0f;
        bool boundsChanged = false, arenaMarkerSpawned = false;

        foreach (var (_, op) in timeline.Ops)
        {
            // detect the start of an enemy/helper cast (players are ignored — their skills aren't mechanics)
            uint enemyCastAid = 0;
            var byHelper = false;
            if (op is ActorState.OpCastInfo ci && ci.Value is { } cast && cast.Action.ID != 0)
            {
                var caster = world.Actors.Find(ci.InstanceID);
                if (caster != null && caster.Type is not (ActorType.Player or ActorType.Pet or ActorType.Chocobo))
                {
                    enemyCastAid = cast.Action.ID;
                    byHelper = caster.Type == ActorType.Helper;
                }
            }

            var before = module != null ? AoeCount(module) : 0;
            world.Execute(op);
            if (module == null)
            {
                module = TryActivate(world, registry, watched);
                if (module != null)
                    initialRadius = module.Bounds.Radius;
            }
            module?.Update();
            var after = module != null ? AoeCount(module) : 0;

            if (module != null)
            {
                if (MathF.Abs(module.Bounds.Radius - initialRadius) > 0.5f)
                    boundsChanged = true;
                if (op is ActorState.OpCreate created && created.Type == ActorType.EventObj)
                    arenaMarkerSpawned = true; // an environment object appeared mid-fight (likely an arena change)
            }

            if (enemyCastAid != 0)
            {
                castCount[enemyCastAid] = castCount.GetValueOrDefault(enemyCastAid) + 1;
                if (byHelper)
                    helperCast.Add(enemyCastAid);
                if (after > before)
                    drawn.Add(enemyCastAid); // the module reacted with a new AOE for this cast
            }
        }

        var drawnList = new List<uint>();
        var hinted = new List<uint>();
        var uncoveredMechanics = new List<uint>();
        var uncoveredVisuals = new List<uint>();
        foreach (var aid in castCount.Keys)
        {
            if (drawn.Contains(aid))
                drawnList.Add(aid);
            else if (watched.Contains(aid))
                hinted.Add(aid);
            else if (helperCast.Contains(aid))
                uncoveredMechanics.Add(aid); // a Helper cast the module ignores is a likely missing mechanic
            else
                uncoveredVisuals.Add(aid);    // a boss self-cast is usually a visual / single-target
        }

        string? arenaNote = boundsChanged
            ? "arena: bounds change mid-fight (handled by the module)"
            : arenaMarkerSpawned
                ? "⚠ arena: environment objects spawned but the module never changed bounds — check for a dynamic arena (Components.ArenaChange)"
                : null;

        return new Result(module?.GetType().Name ?? "(no module activated)", castCount.Count, drawnList, hinted, uncoveredMechanics, uncoveredVisuals, arenaNote);
    }

    private static ModuleBase? TryActivate(WorldState world, ModuleRegistry registry, HashSet<uint> watched)
    {
        if (world.CurrentCFCID == 0)
            return null;
        foreach (var info in registry.ForCFC(world.CurrentCFCID))
            foreach (var actor in world.Actors)
                if (actor.OID == info.PrimaryActorOID && !actor.IsDestroyed)
                {
                    var module = info.Create(world, actor);
                    CollectWatchedActions(module, watched);
                    return module;
                }
        return null;
    }

    private static int AoeCount(ModuleBase module)
    {
        var n = 0;
        foreach (var c in module.Components)
            if (c is Components.GenericAOEs g)
                n += g.ActiveAOEs(0, module.PrimaryActor).Length;
        return n;
    }

    // collect the action ids components explicitly watch (WatchedAction uint / AIDs uint[]) so raidwides,
    // tankbusters, gazes etc. count as "handled" even though they draw no AOE zone
    private static void CollectWatchedActions(ModuleBase module, HashSet<uint> watched)
    {
        foreach (var c in module.Components)
            foreach (var f in c.GetType().GetFields())
            {
                if (f.FieldType == typeof(uint) && f.Name == "WatchedAction")
                    watched.Add((uint)f.GetValue(c)!);
                else if (f.FieldType == typeof(uint[]) && f.Name == "AIDs")
                    foreach (var a in (uint[])f.GetValue(c)!)
                        watched.Add(a);
            }
    }
}
