using System;
using System.Collections.Generic;
using System.Reflection;
using Minerva;
using Minerva.GameSync;

namespace Minerva.Modules;

/// <summary>
/// Decides which encounter <see cref="ModuleBase"/> is active. Each frame it checks the current
/// duty (CFC id) against the <see cref="ModuleRegistry"/> and, if a registered boss actor is
/// present, spins up that module; it tears the module down when the boss despawns or the zone
/// changes. The radar renders whatever module this exposes.
/// </summary>
public sealed class ModuleManager : IDisposable
{
    private readonly WorldState world;
    private readonly ModuleRegistry registry;
    private readonly ZoneModuleRegistry zoneRegistry;
    private uint zoneModuleCFC;   // the CFC the current zone module was built for

    public ModuleBase? ActiveModule { get; private set; }
    public ModuleRegistry.Info? ActiveModuleInfo { get; private set; }
    public int RegisteredCount => this.registry.Count;

    /// <summary>
    /// The module for the zone itself, or null. Independent of <see cref="ActiveModule"/>: it lives for as
    /// long as you are in the zone, so a Foray field hazard keeps warning you between pulls, and keeps
    /// contributing while a boss module is up.
    /// </summary>
    public ZoneModule? ActiveZoneModule { get; private set; }

    public int RegisteredZoneCount => this.zoneRegistry.Count;

    /// <summary>All registered modules, grouped by duty (CFC id) — for the in-game module list.</summary>
    public IReadOnlyDictionary<uint, List<ModuleRegistry.Info>> ModulesByCFC => this.registry.ByCFC;

    public ModuleManager(WorldState world)
    {
        this.world = world;
        // scan both the plugin assembly (content modules) and the core assembly; quest modules are keyed on
        // their quest id and need the game to say which duty that is (see ModuleRegistry.Build)
        var questDuties = GameData.QuestBattleDuties();
        this.registry = ModuleRegistry.Build(quest => questDuties.TryGetValue(quest, out var cfc) ? cfc : 0u, Assembly.GetExecutingAssembly(), typeof(ModuleRegistry).Assembly);
        this.zoneRegistry = ZoneModuleRegistry.Build(Assembly.GetExecutingAssembly(), typeof(ModuleRegistry).Assembly);
        Service.Log.Information($"Minerva: {this.registry.Count} module(s) and {this.zoneRegistry.Count} zone module(s) registered; {questDuties.Count} quest battle(s) mapped to duties.");
        foreach (var c in this.zoneRegistry.Collisions)
            Service.Log.Warning($"Minerva: zone module collision — {c}");
    }

    public void Update()
    {
        this.UpdateZoneModule();

        // Drop the active module once it's no longer valid. A killed boss is not *destroyed* — the corpse
        // lingers for a while — so death has to be checked separately or the module keeps running over a
        // dead encounter and its components keep painting whatever AOEs they were left holding.
        if (this.ActiveModule != null && (this.ActiveModule.PrimaryActor.IsDestroyed || this.world.CurrentCFCID == 0 || this.EncounterOver()))
        {
            this.ActiveModule.Dispose();
            this.ActiveModule = null;
            this.ActiveModuleInfo = null;
        }

        // try to activate a module for the current duty + a present boss actor
        if (this.ActiveModule == null && this.world.CurrentCFCID != 0)
            this.TryActivate();

        this.ActiveModule?.Update();
    }

    /// <summary>
    /// Keep the zone module in step with the zone. Rebuilt only when the CFC changes, so it survives the
    /// whole visit rather than being torn down between pulls the way a boss module is.
    /// </summary>
    private void UpdateZoneModule()
    {
        var cfc = this.world.CurrentCFCID;
        if (cfc == this.zoneModuleCFC)
        {
            this.ActiveZoneModule?.Update();
            return;
        }

        this.ActiveZoneModule?.Dispose();
        this.ActiveZoneModule = null;
        this.zoneModuleCFC = cfc;

        if (cfc == 0)
            return;

        try
        {
            this.ActiveZoneModule = this.zoneRegistry.Create(this.world, cfc);
            if (this.ActiveZoneModule != null)
                Service.Log.Information($"Minerva: activated zone module {this.ActiveZoneModule.GetType().Name} for CFC {cfc}.");
        }
        catch (Exception ex)
        {
            // a zone module that throws on construction must not take the zone change with it
            Service.Log.Error(ex, $"Minerva: zone module for CFC {cfc} failed to construct.");
            this.ActiveZoneModule = null;
        }
    }

    private void TryActivate()
    {
        var candidates = this.registry.ForCFC(this.world.CurrentCFCID);
        for (var i = 0; i < candidates.Count; ++i)
        {
            var info = candidates[i];
            foreach (var actor in this.world.Actors)
            {
                // ... and don't immediately re-activate on that same corpse
                if (actor.OID == info.PrimaryActorOID && !actor.IsDestroyed && !(info.Attr.PrimaryActorDeathEndsEncounter && actor.IsDead))
                {
                    this.ActiveModule = info.Create(this.world, actor);
                    this.ActiveModuleInfo = info;
                    Service.Log.Information($"Minerva: activated module {info.ModuleType.Name} for boss {actor.Name}.");
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Has the fight finished? Only true for modules whose primary actor dying really is the end — a
    /// multi-form boss can die and be replaced, so those keep running until the actor goes away.
    /// </summary>
    private bool EncounterOver()
        => this.ActiveModuleInfo?.Attr.PrimaryActorDeathEndsEncounter == true && this.ActiveModule!.PrimaryActor.IsDead;

    /// <summary>The local player's actor in the world state, if resolvable (object-table slot 0 is the POV).</summary>
    public Actor? LocalPlayer()
    {
        var id = Service.ObjectTable[0]?.GameObjectId ?? 0;
        return id != 0 ? this.world.Actors.Find(id) : null;
    }

    public void Dispose()
    {
        this.ActiveModule?.Dispose();
        this.ActiveModule = null;
        this.ActiveModuleInfo = null;
        this.ActiveZoneModule?.Dispose();
        this.ActiveZoneModule = null;
    }
}
