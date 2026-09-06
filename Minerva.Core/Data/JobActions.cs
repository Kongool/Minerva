namespace Minerva;

// Job actions boss modules name directly, in the same shape as ClassShared next door: a static class with a
// nested AID enum, so a ported module's `WAR.AID.Vengeance` compiles unchanged.
//
// A module naming one of these is stating a fact about the fight -- "this tankbuster wants Vengeance",
// "answer this with Hallowed Ground" -- and for a plugin assisting a boxed party that is among the most
// useful things a module can say, since no rotation can infer it from the job alone. Minerva still presses
// nothing; it carries the claim so the rotation plugin can act on it.
//
// Deliberately not BossmodReborn's action database, which exists to drive an autorotation and runs to
// thousands of ids. These are the ones ported modules actually reference. Extend as more land.

/// <summary>Warrior. Shared actions are aliased rather than renumbered, the way BossmodReborn does it, so
/// a module may reach the same action through either name.</summary>
public static class WAR
{
    public enum AID : uint
    {
        None = 0,
        ThrillOfBattle = 40,
        Holmgang = 43,
        Vengeance = 44,
        Rampart = (uint)ClassShared.AID.Rampart,
        Reprisal = (uint)ClassShared.AID.Reprisal,
    }
}

/// <summary>Paladin.</summary>
public static class PLD
{
    public enum AID : uint
    {
        None = 0,
        Sentinel = 17,
        HallowedGround = 30,
        Sheltron = 3542,
    }
}

/// <summary>White Mage.</summary>
public static class WHM
{
    public enum AID : uint
    {
        None = 0,
        Repose = (uint)ClassShared.AID.Repose,
    }
}

/// <summary>Dragoon.</summary>
public static class DRG
{
    public enum AID : uint
    {
        None = 0,
        ElusiveJump = 94,
    }
}

/// <summary>Dancer.</summary>
public static class DNC
{
    public enum AID : uint
    {
        None = 0,
        ClosedPosition = 16006,
    }

    /// <summary>Statuses a module checks for by name.</summary>
    public enum SID : uint
    {
        None = 0,
        ClosedPosition = 1823,
    }
}

/// <summary>Bard.</summary>
public static class BRD
{
    public enum AID : uint
    {
        None = 0,
        WardensPaean = 3561,
    }
}
