namespace Minerva;

/// <summary>
/// The hard-cast budget Minerva publishes (<c>Minerva.MaxCastTime</c>), reconciled with the dodge that can
/// cancel the cast.
///
/// <para>
/// <see cref="AIHints.MaxCastTime"/> prices the ground: how long until a zone lands here, less the walk out
/// of it. That is the right answer on its own, and the wrong one to publish while the dodge is live, because
/// the steering branch in AIManager asks a different question. It cancels a hardcast the moment this spot is
/// <see cref="AIHints.InImminentDanger(WPos, System.DateTime, float)"/> within the dodge's look-ahead --
/// horizon plus clearance lead, six seconds on the defaults. Everything that falls between the two answers
/// was offered as castable and then cancelled a frame after it began.
/// </para>
///
/// <para>
/// Field report 2026-09-14: a Pictomancer stutter-stepping through a dense dodge phase. Minerva reported
/// "Danger" every step; Daedalus started a cast against a ~4.7s budget for a zone landing five seconds out;
/// Minerva cancelled it and stepped; the next cast started the moment it stopped. A cast that is going to
/// be cancelled is worth nothing, so refusing it up front loses nothing and removes the stutter.
/// </para>
/// </summary>
public static class CastBudget
{
    /// <summary>
    /// The budget to publish when the dodge may cancel a cast: the smaller of the geometric budget and the
    /// time left before this spot enters the cancel's look-ahead.
    /// </summary>
    /// <param name="geometricBudget"><see cref="AIHints.MaxCastTime"/> for the player's spot.</param>
    /// <param name="secondsUntilDangerAtSpot">
    /// <see cref="AIHints.SecondsUntilDangerAt(WPos, System.DateTime, float)"/> for the player's spot, at the
    /// same margin the cancel uses. It applies the same test as the cancel's InImminentDanger -- signed
    /// distance within the margin, the ring probe for shapes that cannot measure, obstacles as immediate --
    /// so "the cancel fires" is exactly "this is at or below the look-ahead".
    /// </param>
    /// <param name="dodgeLookaheadSeconds">How far ahead the cancel looks: horizon plus clearance lead.</param>
    public static float Reconcile(float geometricBudget, float secondsUntilDangerAtSpot, float dodgeLookaheadSeconds)
    {
        // Nothing coming here at all. Spelled out rather than left to float arithmetic, where MaxValue less a
        // few seconds happens to round back to MaxValue.
        if (secondsUntilDangerAtSpot >= float.MaxValue)
            return geometricBudget;

        var untilCancel = System.MathF.Max(secondsUntilDangerAtSpot - dodgeLookaheadSeconds, 0f);
        return System.MathF.Min(geometricBudget, untilCancel);
    }
}
