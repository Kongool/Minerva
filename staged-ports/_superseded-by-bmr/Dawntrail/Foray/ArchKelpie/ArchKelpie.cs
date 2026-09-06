// Arch Kelpie — Occult Crescent (North Horn) FATE boss. Built from Minerva recordings 2026-08-24 (two
// pulls), with every shape taken from the game's own Action sheet.
//
// BossmodReborn DOES have this fight, as `WavedAway` — named after the FATE, not the boss, which is why a
// search by boss name missed it. That port is in staged-ports/ and does not compile yet; when it does it
// supersedes this file, and this one must be deleted rather than left alongside it. Two modules claiming
// one boss OID means the registry picks whichever it reaches first.
namespace Minerva.Modules.Dawntrail.Foray.ArchKelpie;

public enum OID : uint
{
    Boss = 0x4B1F,      // 'Arch Kelpie', R5.4, 15.2M HP
    Helper = 0x4B5B,    // same name, R0.5 — the invisible actor that places the puddles
}

public enum AID : uint
{
    WaveWhistle = 47383,        // Boss->location, 4.7s cast, 25 forward x 50 wide rect
    BloodyPuddleVisual = 47384, // Boss->self, 3.7s cast, single-target: the wind-up for the puddles
    BloodyPuddle = 47385,       // Helper->location, 2.7s cast, range 8 circle, scattered
    WaterIV = 47386,            // Boss->location, 5.2s cast, range 60 circle — larger than the arena
    StormWave = 47387,          // Boss->location, 4.7s cast, 50 x 10 rect
}

sealed class WaveWhistle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WaveWhistle, new AOEShapeRect(25f, 25f));
sealed class BloodyPuddle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BloodyPuddle, new AOEShapeCircle(8f));
sealed class StormWave(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.StormWave, new AOEShapeRect(50f, 5f));

// A 60 yalm circle in an arena 46 across cannot be dodged, so it is damage to survive rather than ground
// to leave. Drawing it would forbid every inch of the floor.
sealed class WaterIV(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.WaterIV);

sealed class BloodyPuddleVisual(ModuleBase module) : Components.IgnoredCasts(module,
    [(uint)AID.BloodyPuddleVisual], "single-target wind-up; the puddles themselves are BloodyPuddle");

sealed class ArchKelpieStates : StateMachineBuilder
{
    public ArchKelpieStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<WaveWhistle>()
            .ActivateOnEnter<BloodyPuddle>()
            .ActivateOnEnter<StormWave>()
            .ActivateOnEnter<WaterIV>()
            .ActivateOnEnter<BloodyPuddleVisual>();
    }
}

// Centre and extent are the bounding box of 25 mechanic casts across both recordings (46x43 yalms),
// rounded out to BossmodReborn's own 30y radius for open-world FATEs.
[ModuleInfo(CFCID = 1093u, PrimaryActorOID = (uint)OID.Boss, PrimaryActorDeathEndsEncounter = true,
    Maturity = ModuleMaturity.WIP, Contributors = "Minerva (extracted from recordings)")]
public sealed class ArchKelpie(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(332.1f, -249.4f), new ArenaBoundsCircle(30f));
