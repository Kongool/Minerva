namespace Minerva;

/// <summary>
/// When a running module is over, and when it gives way to another.
///
/// <para>A module that says when its fight ends -- its last phase has an end condition, the <c>Raw.Update</c> or
/// <c>TransitionOn</c> 269 ports carry -- runs until that condition holds, as BossmodReborn runs it, not until its
/// first actor dies. Hope on the Waves, 2026-09-28: the module is keyed on the Imperial Centurion and is meant to last
/// the whole duty (its end is "left the duty"), so it holds the Magitek Hexadrone's mechanics too; it was dropped when
/// the Centurion died, and the Hexadrone was fought with nothing drawn. A trash-pack module ending on "all of these
/// dead" is the same shape.</para>
///
/// <para>Such a module can run past its boss, so it gives way when another module's boss turns up alive: BossmodReborn
/// loads several at once, Minerva runs one.</para>
/// </summary>
public static class ModuleLifetime
{
    /// <summary>
    /// The module is over: its own end has come, the duty was left, or -- for a module that sets no end of its own --
    /// its boss despawned, or died where that death ends the encounter.
    /// </summary>
    public static bool Ended(bool finished, bool leftDuty, bool hasOwnEnd, bool primaryDestroyed, bool primaryDeathEndsIt)
        => finished || leftDuty || (!hasOwnEnd && (primaryDestroyed || primaryDeathEndsIt));

    /// <summary>A module still running past its boss may give way to another module whose boss turns up.</summary>
    public static bool MayGiveWay(bool hasOwnEnd, bool primaryDead, bool primaryDestroyed)
        => hasOwnEnd && (primaryDead || primaryDestroyed);
}
