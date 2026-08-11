using Minerva;

namespace Minerva.Automation;

/// <summary>
/// Executes movement toward a computed dodge target. Kept behind an interface so the tested
/// decision engine (Core) is fully decoupled from the act of steering the character — which needs
/// game-input/movement hooks that can't be verified headless. The default
/// <see cref="NullMovementController"/> is draw-only (the player moves themselves); a real hooked
/// controller is an opt-in future addition, gated behind config.
/// </summary>
public interface IMovementController
{
    void MoveTo(WPos target);
    void Stop();
}

/// <summary>No-op controller: computes and displays the dodge, but never moves the character.</summary>
public sealed class NullMovementController : IMovementController
{
    public void MoveTo(WPos target) { }
    public void Stop() { }
}
