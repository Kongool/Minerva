namespace Minerva;

/// <summary>
/// The parts of the local client's state that boss modules ask about.
///
/// <para>Deliberately small. BossmodReborn's equivalent is several hundred lines covering job gauges,
/// cooldowns and hotbars, all of it there to drive an autorotation — which Minerva does not have. What
/// modules here actually reach for is the duty action, so that is what this holds.</para>
/// </summary>
public sealed class ClientState
{
    /// <summary>The game gives a duty at most this many; only the first two carry charges.</summary>
    public const int NumDutyActions = 5;

    /// <summary>
    /// An action granted by the duty rather than by your job.
    ///
    /// <para>These are what a fight hands you when it changes what you are: Wuk Lamat's own kit in a
    /// Dawntrail solo duty, a Bozja lost action, the single button you are left with while transformed.
    /// A module names one because in that fight it is the answer — sometimes the only thing usable at
    /// all — and no amount of knowing your job tells a rotation that.</para>
    /// </summary>
    public readonly record struct DutyAction(ActionID Action, byte CurCharges, byte MaxCharges);

    /// <summary>
    /// The duty's actions, in slot order. Empty entries are <c>default</c>, so a module reading slot 0 of
    /// a duty that grants nothing gets an action id of 0 rather than an exception.
    /// </summary>
    public readonly DutyAction[] DutyActions = new DutyAction[NumDutyActions];

    /// <summary>Does this duty grant any action at all?</summary>
    public bool HasDutyActions
    {
        get
        {
            for (var i = 0; i < NumDutyActions; ++i)
                if (this.DutyActions[i].Action.ID != 0)
                    return true;
            return false;
        }
    }
}
