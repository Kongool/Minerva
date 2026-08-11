using System;
using Minerva;
using Minerva.Modules;

namespace Minerva.Automation;

/// <summary>
/// Drives the auto-dodge loop: each frame it builds <see cref="AIHints"/> from the active module's
/// components for the local player, runs <see cref="ArenaPathfinder"/>, and exposes the resulting
/// <see cref="SafeSpot"/> for the radar to display. Actual movement is only issued when auto-dodge
/// is explicitly enabled AND a real <see cref="IMovementController"/> is installed — by default it
/// is guidance only (draw the dodge target; the player moves).
/// </summary>
public sealed class AIManager
{
    private readonly WorldState world;
    private readonly ModuleManager modules;
    private readonly Configuration config;
    private readonly IMovementController movement;
    private readonly AIHints hints = new();

    public SafeSpot Current { get; private set; } = SafeSpot.Stay;
    public AIHints Hints => this.hints;
    public bool HasSolution { get; private set; }

    public AIManager(WorldState world, ModuleManager modules, Configuration config, IMovementController? movement = null)
    {
        this.world = world;
        this.modules = modules;
        this.config = config;
        this.movement = movement ?? new NullMovementController();
    }

    public void Update()
    {
        this.HasSolution = false;
        this.Current = SafeSpot.Stay;

        var module = this.modules.ActiveModule;
        var pc = this.modules.LocalPlayer();
        if (module == null || pc == null)
        {
            this.movement.Stop();
            return;
        }

        module.BuildAIHints(0, pc, this.hints);
        this.Current = ArenaPathfinder.Solve(this.hints, this.world.CurrentTime);
        this.HasSolution = true;

        // steering is opt-in and only meaningful with a real controller installed
        if (this.config.AutoDodgeEnabled && this.Current.NeedToMove && this.Current.Found)
            this.movement.MoveTo(this.Current.Target);
        else
            this.movement.Stop();
    }
}
