namespace Minerva.Pathfinding;

/// <summary>
/// A pair of points a fight lets you cross between — the wolf platforms in M08S, and anything else that
/// moves you somewhere the floor does not connect to.
///
/// <para><b>Recorded, not routed through.</b> Minerva's pathfinder solves over a grid of arena cells and
/// has no notion of an edge that skips space, so a module collecting these is stating a fact about the
/// fight that the solver cannot yet use. That is the same bargain as the tank-AI fields on
/// <see cref="AIHints.Enemy"/>: the knowledge is real, deleting it from every port would lose it, and the
/// day the solver grows teleporter edges the modules will already be telling it where they are.</para>
///
/// <para>Shaped exactly like BossmodReborn's so ported modules construct it unchanged. <c>useTime</c> is
/// how long taking it costs, and <c>notBeforeG</c> the earliest path-cost at which it may be used — both
/// meaningful only to a solver that routes through them.</para>
/// </summary>
public readonly struct Teleporter(WPos entrance, WPos exit, float radius, bool bidirectional, float useTime = 0f, float notBeforeG = 0f)
{
    public readonly WPos Entrance = entrance;
    public readonly WPos Exit = exit;
    public readonly float Radius = radius;
    public readonly bool Bidirectional = bidirectional;
    public readonly float UseTime = useTime;
    public readonly float NotBeforeG = notBeforeG;
}
