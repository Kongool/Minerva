using Minerva;

namespace Minerva.Automation;

/// <summary>
/// Executes movement toward a computed dodge target. Kept behind an interface so the tested
/// decision engine (Core) is fully decoupled from the act of steering the character — which needs
/// game-input/movement hooks that can't be verified headless. <see cref="NullMovementController"/>
/// is draw-only (the player moves themselves) and is the headless/default fallback; the real hooked
/// implementation is <see cref="MovementController"/>, activated only when auto-move is enabled.
/// </summary>
public interface IMovementController
{
    void MoveTo(WPos target);
    void Stop();

    /// <summary>Turn the character to face a world direction. Used for gazes, where facing is the mechanic.</summary>
    void Face(Angle direction);
}

/// <summary>No-op controller: computes and displays the dodge, but never moves the character.</summary>
public sealed class NullMovementController : IMovementController
{
    public void MoveTo(WPos target) { }
    public void Stop() { }
    public void Face(Angle direction) { }
}
