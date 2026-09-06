namespace Minerva;

/// <summary>
/// Actions every job shares — role actions, Sprint, and the handful of duty-specific ones the game hands
/// out. Ported from BossmodReborn's <c>ClassShared</c> (BSD-3; see THIRD-PARTY-NOTICES.txt).
///
/// <para>Modules reference these when a mechanic is answered by a role action rather than by movement: a
/// raidwide that wants Reprisal, a doom that wants Esuna. Minerva does not press buttons, so these are
/// carried so ported hints compile and read correctly — and so a rotation plugin asking Minerva what is
/// coming can be told which mitigation the fight expects.</para>
///
/// <para>Only the actions ported modules actually name are listed; BossmodReborn's full table runs to
/// several hundred. Extend it as more modules land rather than transcribing the lot.</para>
/// </summary>
public static class ClassShared
{
    public enum AID : uint
    {
        None = 0,
        Sprint = 3,

        // Tank
        Rampart = 7531,
        Provoke = 7533,
        Reprisal = 7535,
        ArmsLength = 7548,
        LowBlow = 7540,

        // Melee
        Feint = 7549,
        LegSweep = 7863,

        // Physical ranged
        LegGraze = 7554,

        // Caster / healer
        Addle = 7560,
        Surecast = 7559,
        Esuna = 7568,
        Repose = 16560,

        // Duty actions
        MagitekPulse = 7962,
        Vril = 9345,
        Shatterstone = 9823,
    }
}
