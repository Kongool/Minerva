using System;
using System.Reflection;
using Minerva;

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

    public ModuleBase? ActiveModule { get; private set; }
    public ModuleRegistry.Info? ActiveModuleInfo { get; private set; }
    public int RegisteredCount => this.registry.Count;

    public ModuleManager(WorldState world)
    {
        this.world = world;
        // scan both the plugin assembly (content modules) and the core assembly
        this.registry = ModuleRegistry.Build(Assembly.GetExecutingAssembly(), typeof(ModuleRegistry).Assembly);
        Service.Log.Information($"Minerva: {this.registry.Count} module(s) registered.");
    }

    public void Update()
    {
        // drop the active module if it's no longer valid
        if (this.ActiveModule != null && (this.ActiveModule.PrimaryActor.IsDestroyed || this.world.CurrentCFCID == 0))
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

    private void TryActivate()
    {
        var candidates = this.registry.ForCFC(this.world.CurrentCFCID);
        for (var i = 0; i < candidates.Count; ++i)
        {
            var info = candidates[i];
            foreach (var actor in this.world.Actors)
            {
                if (actor.OID == info.PrimaryActorOID && !actor.IsDestroyed)
                {
                    this.ActiveModule = info.Create(this.world, actor);
                    this.ActiveModuleInfo = info;
                    Service.Log.Information($"Minerva: activated module {info.ModuleType.Name} for boss {actor.Name}.");
                    return;
                }
            }
        }
    }

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
    }
}
