// Occult Crescent — North Horn. Boss 'Claret Dragon' 0x4C46 (verified from recording: single instance, 122.9M
// HP; NameID 14787). Duty CFC 1093 — coexists with the other North Horn modules. No BossmodReborn reference; WIP.
//
// Boss OID: the extractor mis-picked 'Aetherial Ward' 0x4C48 (a 14.5M ward mechanic actor spawned by the boss's
// AetherialWard cast) as the boss — every mechanic is actually cast by 'Claret Dragon' 0x4C46, verified as the
// 122.9M-HP single instance. (Same class of mis-pick as Elm Gigas' swarm; caught by max HP.)
//
// Cleanup / needs validation:
//   - Pruned 5 of 6 auto-voidzones (Crescent Big Horn / Adamantoise / Woolback / Ratel wildlife + the 188K
//     Claret Dragon add); kept the Necrohaze puddle (0x4C47), which pairs with the Necrohaze cast.
//   - Necro/breath cones (Snaking Necrobreath, Breath in Threes 1/2) and Cauterize rect need confirming;
//     Howling Darkness / Grave Mold 2 / Soar / Cauterize 2 / Aetherial Ward shapes unknown.
//   - Arena is a guessed 22y square at (-687.6, 150.1); possible arena change on 0x1EC095.
using System;
using Minerva;

namespace Minerva.Dawntrail.Foray.ClaretDragon;

public enum OID : uint
{
    Boss = 0x4C46,          // 'Claret Dragon' R5.0 — the real boss (single instance, 122.9M HP; verified)
    AetherialWard = 0x4C48, // 'Aetherial Ward' — ward mechanic actor (extractor mis-picked this as boss)
    ClaretDragonAdd = 0x4D25, // 'Claret Dragon' R1.0 — 188K add
    Helper = 0x233C,        // 'Claret Dragon' R0.5 — invisible helper
    Necrohaze = 0x4C47,     // 'Necrohaze' R1.5 — lingering puddle (pairs with the Necrohaze cast)
    _1EA1A1 = 0x1EA1A1,     // '' R2 — event object
    // ambient Crescent wildlife (Big Horn / Adamantoise / Woolback / Ratel) captured by the recording dropped here.
}

public enum AID : uint
{
    HowlingDarkness = 48277, // Claret Dragon->Self, 4.7s, x5
    SnakingNecrobreath = 48260, // Claret Dragon->Self, 5.7s, x4
    GraveMold = 48262, // Claret Dragon->Self, 5.7s, x42
    GraveMold2 = 48261, // Claret Dragon->Self, 4.7s, x5
    Soar = 50488, // Claret Dragon->Self, 3.7s, x3
    Cauterize = 48265, // Claret Dragon->Self, 6.7s, x3
    Cauterize2 = 48264, // Claret Dragon->Self, 5.7s, x3
    AetherialWard = 48271, // Claret Dragon->Self, 4.2s, x1
    Necrohaze = 50484, // Claret Dragon->Self, 3.7s, x1
    BreathInThrees = 48270, // Claret Dragon->Self, 4.7s, x3
    BreathInThrees2 = 48248, // Claret Dragon->Self, 2.2s, x5
}

// --- likely-correct / reasonable-guess AOEs (verify against the recording) ---
sealed class GraveMold(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GraveMold, new AOEShapeCircle(8f));
sealed class Necrohaze(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Necrohaze, new AOEShapeCircle(5f));
sealed class Cauterize(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Cauterize, new AOEShapeRect(40f, 5f)); // dragon dive-line; confirm
sealed class SnakingNecrobreath(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SnakingNecrobreath, new AOEShapeCone(60f, 45f.Degrees())); // TODO: confirm shape
sealed class BreathInThrees(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BreathInThrees, new AOEShapeCone(60f, 45f.Degrees())); // TODO: confirm shape
sealed class BreathInThrees2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BreathInThrees2, new AOEShapeCone(60f, 45f.Degrees())); // TODO: confirm shape

// --- shape unknown: hint only until the recording shows the real shape ---
sealed class HowlingDarkness(ModuleBase module) : Components.CastHint(module, (uint)AID.HowlingDarkness, "Howling Darkness (shape TBD)");
sealed class GraveMold2(ModuleBase module) : Components.CastHint(module, (uint)AID.GraveMold2, "Grave Mold (shape TBD)");
sealed class Soar(ModuleBase module) : Components.CastHint(module, (uint)AID.Soar, "Soar (dive)");
sealed class Cauterize2(ModuleBase module) : Components.CastHint(module, (uint)AID.Cauterize2, "Cauterize (shape TBD)");
sealed class AetherialWard(ModuleBase module) : Components.CastHint(module, (uint)AID.AetherialWard, "Aetherial Ward (shape TBD)");

sealed class NecrohazeVoidzone(ModuleBase module) : Components.Voidzone(module, 1.5f, (uint)OID.Necrohaze); // TODO: confirm radius

sealed class ClaretDragonStates : StateMachineBuilder
{
    public ClaretDragonStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<GraveMold>()
            .ActivateOnEnter<GraveMold2>()
            .ActivateOnEnter<Necrohaze>()
            .ActivateOnEnter<NecrohazeVoidzone>()
            .ActivateOnEnter<Cauterize>()
            .ActivateOnEnter<Cauterize2>()
            .ActivateOnEnter<SnakingNecrobreath>()
            .ActivateOnEnter<BreathInThrees>()
            .ActivateOnEnter<BreathInThrees2>()
            .ActivateOnEnter<HowlingDarkness>()
            .ActivateOnEnter<Soar>()
            .ActivateOnEnter<AetherialWard>();
    }
}

[ModuleInfo(CFCID = 1093u, NameID = 14787u, Maturity = ModuleMaturity.WIP, Contributors = "Minerva extractor (North Horn; no BMR reference)")]
public sealed class ClaretDragon(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(-687.6f, 150.1f), new ArenaBoundsSquare(22f)); // arena guessed — refine
