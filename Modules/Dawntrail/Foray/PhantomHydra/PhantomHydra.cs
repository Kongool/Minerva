// Occult Crescent — North Horn. Boss 'Phantom Hydra' 0x4BC5 (identity confirmed: it self-casts the fight's
// signature mechanics — ElementalCascade, Many-Headed Breath, Discordance). Duty CFC 1093 (zone 1346); this
// coexists with the Arbatel module under the same CFC (registry activates whichever boss OID is present).
// No BossmodReborn reference exists for this fight (novel North Horn content); shapes are recording-only.
// WIP: validate against a recording (/mine -> Replay -> Validate) before trusting.
//
// This is an OPEN-FIELD boss, so the recording captured a lot of ambient noise. Cleaned up from the raw
// draft:
//   - Boss set to Phantom Hydra 0x4BC5 (was fine, kept).
//   - LevinRing / LevinRing2 were Rect(_, 0) = zero-width (draws nothing). "Ring" => almost certainly a
//     donut; demoted to CastHint until the recording gives inner/outer radii.
//   - IceBurst was a guessed cone; "burst" from an orb is more likely a circle — demoted to CastHint
//     pending radius (it fires x56, so a wrong shape would be very noisy).
//   - Pruned the auto-voidzones that were ambient field objects / wildlife (Striking Dummy, Occult
//     Aetheryte, Survey Point, Crescent fauna) so we don't paint fake danger on non-mechanics. Kept only
//     the numeric event-object 0x1EA1A1 as a plausible puddle — verify in Validate, add real ones back.
//   - Arena is a guessed 18y square at (-82, 483.5); open-field arena is fuzzy — refine from the recording.
//
// Still needs validation, highest value first:
//   - LevinRing donut radii; IceBurst / NighDrawnEruption / FarFlungEruption / Discordance shapes.
//   - Which lingering actor (if any) is a REAL voidzone puddle vs. ambient wildlife.
//   - Phase transitions are guessed map-effects (TODO); the draft put every mechanic in P1 (P2/P3 empty).
using System;
using Minerva;

namespace Minerva.Dawntrail.Foray.PhantomHydra;

public enum OID : uint
{
    Boss = 0x4BC5,          // 'Phantom Hydra' — the boss (self-casts the signature mechanics)
    Helper = 0x233C,        // 'Phantom Hydra' R0.5 — invisible helper
    CrescentSandSerpent = 0x4E20, // 'Crescent Sand Serpent' R3.45 — ambient add
    SwirlingOrb = 0x4BC8,   // 'Swirling Orb' R0.5 — casts IceBurst
    BallOfFire = 0x4BC7,    // 'Ball of Fire' R1.5 — casts ScarletThread
    CrescentMedusa = 0x4E1D, // 'Crescent Medusa' R1.35 — ambient add
    BallOfLevin = 0x4BC9,   // 'Ball of Levin' R2.3 — casts LevinRing/Shock
    _1EA1A1 = 0x1EA1A1,     // '' R2 — event object, plausible puddle
    HolySphere = 0x4BC6,    // 'Holy Sphere' R1.2 — casts StunningSheen
    OccultAetheryte = 0x1EC0C5, // 'Occult Aetheryte' — field object (not a mechanic)
    SurveyPoint = 0x1EC0BD, // 'Survey Point' — field object
    StrikingDummy = 0x478A, // 'Striking Dummy' — field object
    AetheryteShard = 0x1EC0C7, // 'Aetheryte Shard' — field object
    CrescentCliffkite = 0x4E08, // 'Crescent Cliffkite' R3 — ambient add
    CrescentCoeurl = 0x4E21, // 'Crescent Coeurl' R4.2 — ambient add
}

public enum AID : uint
{
    NighDrawnEruption = 47197, // Phantom Hydra->Self, 6.7s cast, x2, P1
    ElementalCascade = 47201, // Phantom Hydra->Self, 6.7s cast, x8, P1
    ElementalCascade2 = 47199, // Phantom Hydra->Self, 6.7s cast, x5, P1
    ElementalCascade3 = 47202, // Phantom Hydra->Self, 6.7s cast, x8, P1
    ElementalCascade4 = 47203, // Phantom Hydra->Self, 6.7s cast, x8, P1
    ElementalCascade5 = 47200, // Phantom Hydra->Self, 6.7s cast, x5, P1
    FarFlungEruption = 47198, // Phantom Hydra->Self, 6.7s cast, x3, P1
    ElementalCascade6 = 47184, // Phantom Hydra->Self, 2.7s cast, x3, P1
    ElementalCascade7 = 47187, // Phantom Hydra->Self, 2.7s cast, x4, P1
    ElementalCascade8 = 47189, // Phantom Hydra->Self, 2.7s cast, x5, P1
    ElementalCascade9 = 47185, // Phantom Hydra->Self, 2.7s cast, x8, P1
    LevinRing = 47196, // Ball of Levin->Self, 9.7s cast, x4, P1
    LevinRing2 = 47195, // Ball of Levin->Self, 6.7s cast, x4, P1
    Shock = 47194, // Ball of Levin->Self, 3.7s cast, x4, P1
    StunningSheen = 47191, // Holy Sphere->Self, 4.7s cast, x5, P1
    ElementalCascade10 = 47186, // Phantom Hydra->Self, 2.7s cast, x7, P1
    ElementalCascade11 = 47188, // Phantom Hydra->Self, 2.7s cast, x30, P1
    IceBurst = 47192, // Swirling Orb->Self, 2.7s cast, x56, P1
    ScarletThread = 47190, // Ball of Fire->Self, 2.7s cast, x30, P1
    Discordance = 47209, // Phantom Hydra->Self, 4.7s cast, x5, P1
    ManyHeadedBreath = 47213, // Phantom Hydra->Self, 7.7s cast, x2, P1 (wind-up)
    ManyHeadedBreath2 = 47212, // Phantom Hydra->Self, 0.7s cast, x12, P1
    ManyHeadedBreath3 = 50674, // Phantom Hydra->Self, 0.5s cast, x4, P1
    ManyHeadedBreath4 = 50675, // Phantom Hydra->Self, 0.5s cast, x4, P1
    ManyHeadedBreath5 = 50673, // Phantom Hydra->Self, 0.5s cast, x4, P1
}

// --- classified / likely-correct (still verify against the recording) ---
sealed class ElementalCascade(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ElementalCascade, new AOEShapeCircle(8f));
sealed class ElementalCascade2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ElementalCascade2, new AOEShapeCircle(8f));
sealed class ElementalCascade3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ElementalCascade3, new AOEShapeCircle(8f));
sealed class ElementalCascade4(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ElementalCascade4, new AOEShapeCircle(8f));
sealed class ElementalCascade5(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ElementalCascade5, new AOEShapeCircle(8f));
sealed class ElementalCascade7(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ElementalCascade7, new AOEShapeCircle(6f));
sealed class ElementalCascade8(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ElementalCascade8, new AOEShapeCircle(6f));
sealed class ElementalCascade9(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ElementalCascade9, new AOEShapeCircle(6f));
sealed class ElementalCascade10(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ElementalCascade10, new AOEShapeCircle(6f));
sealed class ElementalCascade11(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ElementalCascade11, new AOEShapeCircle(6f));
sealed class Shock(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Shock, new AOEShapeCircle(10f));
sealed class StunningSheen(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.StunningSheen);
sealed class ScarletThread(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ScarletThread, new AOEShapeRect(70f, 2f));
// Many-Headed Breath: the short 0.5-0.7s casts are the actual head cones (wind-up is the 7.7s cast below).
sealed class ManyHeadedBreath2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ManyHeadedBreath2, new AOEShapeCone(30f, 45f.Degrees())); // TODO: confirm cone radius/angle
sealed class ManyHeadedBreath3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ManyHeadedBreath3, new AOEShapeCone(30f, 45f.Degrees())); // TODO: confirm cone radius/angle
sealed class ManyHeadedBreath4(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ManyHeadedBreath4, new AOEShapeCone(30f, 45f.Degrees())); // TODO: confirm cone radius/angle
sealed class ManyHeadedBreath5(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ManyHeadedBreath5, new AOEShapeCone(30f, 45f.Degrees())); // TODO: confirm cone radius/angle

// --- shape unknown / bugged in the draft: hint only until the recording shows the real shape ---
sealed class NighDrawnEruption(ModuleBase module) : Components.CastHint(module, (uint)AID.NighDrawnEruption, "Nigh-Drawn Eruption (shape TBD)");
sealed class FarFlungEruption(ModuleBase module) : Components.CastHint(module, (uint)AID.FarFlungEruption, "Far-Flung Eruption (shape TBD)");
sealed class ElementalCascade6(ModuleBase module) : Components.CastHint(module, (uint)AID.ElementalCascade6, "Elemental Cascade (shape TBD)");
sealed class Discordance(ModuleBase module) : Components.CastHint(module, (uint)AID.Discordance, "Discordance (shape TBD)");
sealed class ManyHeadedBreath(ModuleBase module) : Components.CastHint(module, (uint)AID.ManyHeadedBreath, "Many-Headed Breath incoming");
sealed class LevinRing(ModuleBase module) : Components.CastHint(module, (uint)AID.LevinRing, "Levin Ring (donut? radii TBD)"); // was Rect(30,0) = draws nothing
sealed class LevinRing2(ModuleBase module) : Components.CastHint(module, (uint)AID.LevinRing2, "Levin Ring (donut? radii TBD)"); // was Rect(20,0) = draws nothing
sealed class IceBurst(ModuleBase module) : Components.CastHint(module, (uint)AID.IceBurst, "Ice Burst (circle? radius TBD)"); // was a guessed cone, fires x56

// --- voidzones: only the plausible event-object puddle kept; ambient wildlife/field objects pruned ---
sealed class EventVoidzone(ModuleBase module) : Components.Voidzone(module, 2f, (uint)OID._1EA1A1); // TODO: confirm this is a real puddle + radius
// Pruned as ambient (add back if Validate shows a real puddle): CrescentSandSerpent, CrescentMedusa,
// BallOfLevin, OccultAetheryte, StrikingDummy, CrescentCliffkite, CrescentCoeurl.

sealed class PhantomHydraStates : StateMachineBuilder
{
    public PhantomHydraStates(ModuleBase module) : base(module)
    {
        // The draft assigned every mechanic to P1 (P2/P3 came up empty). Kept single-phase until the
        // recording shows real phase boundaries; add P2/P3 mechanics + real transitions after Validate.
        this.TrivialPhase()
            .ActivateOnEnter<ElementalCascade>()
            .ActivateOnEnter<ElementalCascade2>()
            .ActivateOnEnter<ElementalCascade3>()
            .ActivateOnEnter<ElementalCascade4>()
            .ActivateOnEnter<ElementalCascade5>()
            .ActivateOnEnter<ElementalCascade6>()
            .ActivateOnEnter<ElementalCascade7>()
            .ActivateOnEnter<ElementalCascade8>()
            .ActivateOnEnter<ElementalCascade9>()
            .ActivateOnEnter<ElementalCascade10>()
            .ActivateOnEnter<ElementalCascade11>()
            .ActivateOnEnter<NighDrawnEruption>()
            .ActivateOnEnter<FarFlungEruption>()
            .ActivateOnEnter<Discordance>()
            .ActivateOnEnter<ManyHeadedBreath>()
            .ActivateOnEnter<ManyHeadedBreath2>()
            .ActivateOnEnter<ManyHeadedBreath3>()
            .ActivateOnEnter<ManyHeadedBreath4>()
            .ActivateOnEnter<ManyHeadedBreath5>()
            .ActivateOnEnter<LevinRing>()
            .ActivateOnEnter<LevinRing2>()
            .ActivateOnEnter<Shock>()
            .ActivateOnEnter<StunningSheen>()
            .ActivateOnEnter<IceBurst>()
            .ActivateOnEnter<ScarletThread>()
            .ActivateOnEnter<EventVoidzone>();
    }
}

[ModuleInfo(CFCID = 1093u, NameID = 0u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Minerva extractor (North Horn; no BMR reference)")]
public sealed class PhantomHydra(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(-82f, 483.5f), new ArenaBoundsSquare(18f)); // arena guessed — refine
