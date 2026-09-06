// Occult Crescent — North Horn. Boss 'Metamorph' 0x4C77 (verified from recording: single instance, 120.8M HP;
// NameID 14801). A shapeshifter with a large, mostly self-cast kit. Duty CFC 1093 — coexists with the other
// North Horn modules. No BossmodReborn reference; recording-only. WIP.
//
// Cleanup / needs validation:
//   - Five zero-size shape bugs demoted to CastHint (drew nothing): BlackenedRain Circle(0), CyclonicRing /
//     ShapeshiftingSupercell4/5 Rect(_,0), HellwardBound Donut(0,0).
//   - Dropped all 5 auto-voidzones — 4 are ambient Crescent wildlife (Salt Swallow / Opken / Mimic / Soblyn)
//     and 0x4DFD 'Metamorph' is a 188K add, not a puddle.
//   - Many "Hellish Breath" / A483xx casts defaulted to 60y cones (plausible for breath); CycloneCrossing2 a
//     cross. Confirm shapes. Several Change / Shapeshifting / Cyclone / Dark-Dealing casts are shape-unknown.
//   - Arena is a guessed 23y square at (500, -312.1); possible arena changes on 0x1EC09A/B/C.
using System;
using Minerva;

namespace Minerva.Dawntrail.Foray.Metamorph;

public enum OID : uint
{
    Boss = 0x4C77,          // 'Metamorph' — the boss (single instance, 120.8M HP; verified)
    Helper = 0x233C,        // 'Metamorph' R0.5 — invisible helper; casts most AOEs
    MetamorphAdd = 0x4DFD,  // 'Metamorph' R1.0 — 188K add
    _1EA1A1 = 0x1EA1A1,     // '' R2 — event object
    // ambient Crescent wildlife (Salt Swallow / Opken / Mimic / Soblyn) captured by the recording dropped here.
}

public enum AID : uint
{
    BlackenedRain = 48336, BlackenedRain2 = 48335, Change = 48339, CyclonicRing = 48354,
    ShapeshiftingSupercell = 48355, ShapeshiftingSupercell2 = 48357, ShapeshiftingSupercell3 = 50767,
    ShapeshiftingSupercell4 = 48361, ShapeshiftingSupercell5 = 48362, ShapeshiftingSupercell6 = 48359,
    ShapeshiftingSupercell7 = 48360, MadeMagic = 48363, CycloneCrossing = 48365, CycloneCrossing2 = 48366,
    DarkDealing = 48337, Change2 = 48338, TongueOfFlame = 48341, HellfireFetch = 48345, HellwardBound = 48343,
    A48348 = 48348, HellishBreath = 48346, A48349 = 48349, A48347 = 48347,
    HellishBreath2 = 48662, HellishBreath3 = 50677, HellishBreath4 = 48663,
}

// --- likely-correct / reasonable-guess AOEs (verify against the recording) ---
sealed class ShapeshiftingSupercell3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ShapeshiftingSupercell3, new AOEShapeCircle(8f));
sealed class ShapeshiftingSupercell7(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ShapeshiftingSupercell7, new AOEShapeCircle(8f));
sealed class TongueOfFlame(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TongueOfFlame, new AOEShapeCircle(15f));
sealed class HellfireFetch(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HellfireFetch, new AOEShapeCircle(6f));
sealed class CycloneCrossing2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CycloneCrossing2, new AOEShapeCross(60f, 8f)); // TODO: confirm
// pre-filled estimates so auto-move can route around them (shape type confident, size unconfirmed):
sealed class CyclonicRing(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CyclonicRing, new AOEShapeDonut(8f, 30f)); // ESTIMATE: "Ring" => donut, radii unconfirmed
sealed class CycloneCrossing(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CycloneCrossing, new AOEShapeCross(60f, 8f)); // ESTIMATE: matches CycloneCrossing2
sealed class HellishBreath(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HellishBreath, new AOEShapeCone(60f, 45f.Degrees())); // ESTIMATE: matches HellishBreath2/3/4
sealed class ShapeshiftingSupercell2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ShapeshiftingSupercell2, new AOEShapeCone(60f, 45f.Degrees())); // TODO: confirm
sealed class ShapeshiftingSupercell6(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ShapeshiftingSupercell6, new AOEShapeCone(60f, 45f.Degrees())); // TODO: confirm
sealed class A48348(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.A48348, new AOEShapeCone(60f, 45f.Degrees())); // TODO: confirm
sealed class A48349(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.A48349, new AOEShapeCone(60f, 45f.Degrees())); // TODO: confirm
sealed class A48347(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.A48347, new AOEShapeCone(60f, 45f.Degrees())); // TODO: confirm
sealed class HellishBreath2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HellishBreath2, new AOEShapeCone(60f, 45f.Degrees())); // TODO: confirm
sealed class HellishBreath3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HellishBreath3, new AOEShapeCone(60f, 45f.Degrees())); // TODO: confirm
sealed class HellishBreath4(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HellishBreath4, new AOEShapeCone(60f, 45f.Degrees())); // TODO: confirm

// --- shape unknown / bugged in the draft: hint only until the recording shows the real shape ---
sealed class BlackenedRain(ModuleBase module) : Components.CastHint(module, (uint)AID.BlackenedRain, "Blackened Rain (shape TBD)"); // was Circle(0)
sealed class ShapeshiftingSupercell4(ModuleBase module) : Components.CastHint(module, (uint)AID.ShapeshiftingSupercell4, "Shapeshifting Supercell (shape TBD)"); // was Rect(16,0)
sealed class ShapeshiftingSupercell5(ModuleBase module) : Components.CastHint(module, (uint)AID.ShapeshiftingSupercell5, "Shapeshifting Supercell (shape TBD)"); // was Rect(30,0)
sealed class HellwardBound(ModuleBase module) : Components.CastHint(module, (uint)AID.HellwardBound, "Hellward Bound (donut? TBD)"); // was Donut(0,0)
sealed class BlackenedRain2(ModuleBase module) : Components.CastHint(module, (uint)AID.BlackenedRain2, "Blackened Rain (shape TBD)");
sealed class Change(ModuleBase module) : Components.CastHint(module, (uint)AID.Change, "Change (shapeshift)");
sealed class Change2(ModuleBase module) : Components.CastHint(module, (uint)AID.Change2, "Change (shapeshift)");
sealed class ShapeshiftingSupercell(ModuleBase module) : Components.CastHint(module, (uint)AID.ShapeshiftingSupercell, "Shapeshifting Supercell (shape TBD)");
sealed class MadeMagic(ModuleBase module) : Components.CastHint(module, (uint)AID.MadeMagic, "Made Magic (shape TBD)");
sealed class DarkDealing(ModuleBase module) : Components.CastHint(module, (uint)AID.DarkDealing, "Dark Dealing (targeted)");

sealed class MetamorphStates : StateMachineBuilder
{
    public MetamorphStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<ShapeshiftingSupercell3>()
            .ActivateOnEnter<ShapeshiftingSupercell7>()
            .ActivateOnEnter<TongueOfFlame>()
            .ActivateOnEnter<HellfireFetch>()
            .ActivateOnEnter<CycloneCrossing2>()
            .ActivateOnEnter<ShapeshiftingSupercell2>()
            .ActivateOnEnter<ShapeshiftingSupercell6>()
            .ActivateOnEnter<A48348>()
            .ActivateOnEnter<A48349>()
            .ActivateOnEnter<A48347>()
            .ActivateOnEnter<HellishBreath2>()
            .ActivateOnEnter<HellishBreath3>()
            .ActivateOnEnter<HellishBreath4>()
            .ActivateOnEnter<BlackenedRain>()
            .ActivateOnEnter<BlackenedRain2>()
            .ActivateOnEnter<CyclonicRing>()
            .ActivateOnEnter<ShapeshiftingSupercell>()
            .ActivateOnEnter<ShapeshiftingSupercell4>()
            .ActivateOnEnter<ShapeshiftingSupercell5>()
            .ActivateOnEnter<HellwardBound>()
            .ActivateOnEnter<Change>()
            .ActivateOnEnter<Change2>()
            .ActivateOnEnter<MadeMagic>()
            .ActivateOnEnter<CycloneCrossing>()
            .ActivateOnEnter<DarkDealing>()
            .ActivateOnEnter<HellishBreath>();
    }
}

[ModuleInfo(CFCID = 1093u, NameID = 14801u, Maturity = ModuleMaturity.WIP, Contributors = "Minerva extractor (North Horn; no BMR reference)")]
public sealed class Metamorph(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(500f, -312.1f), new ArenaBoundsSquare(23f)); // arena guessed — refine
