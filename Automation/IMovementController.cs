using System.Collections.Generic;
using Minerva;

namespace Minerva.Automation;

/// <summary>
/// Something that can walk the local character. The AI decides where; this decides how -- raw input
/// override, or a navmesh follower when one is loaded.
/// </summary>
public interface IMovementController
{
    void MoveTo(WPos target);
    void MoveTo(WPos target, IReadOnlyList<WPos>? route);
    void Stop();
    void Face(Angle direction);

    /// <summary>Which mover the last <see cref="MoveTo"/> went through; <see cref="Mover.None"/> after <see cref="Stop"/>.</summary>
    Mover Mode { get; }

    /// <summary>The mover reports it is actually driving: a path running, or input written this frame. A
    /// steer that never becomes motion is the difference between this and the AI's own "steering".</summary>
    bool Busy { get; }
}

public sealed class NullMovementController : IMovementController
{
    public void MoveTo(WPos target) { }
    public void MoveTo(WPos target, IReadOnlyList<WPos>? route) { }
    public void Stop() { }
    public void Face(Angle direction) { }
    public Mover Mode => Mover.None;
    public bool Busy => false;
}
