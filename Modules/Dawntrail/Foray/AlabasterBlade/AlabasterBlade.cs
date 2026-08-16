// Occult Crescent — North Horn. Boss 'Alabaster Blade' 0x4BBE (identity confirmed: R4.0, the largest actor,
// and it self-casts its Occult spell kit — Occult Aero/Aero III/Tornado/Stone II, Right/Left Combination).
// 'Alabaster Golem' 0x4BBF is an add (casts Acclaim). Duty CFC 1093 — coexists with the Arbatel + Phantom
// Hydra modules (registry activates whichever boss OID is present). No BossmodReborn reference; shapes are
// recording-only. WIP — validate against a recording before trusting.
//
// Cleanup from the raw draft:
//   - Pruned the 4 Crescent-wildlife voidzones (Banemite, Moss Fungus, Arioch, Rot-eyes) — ambient
//     open-field mobs, not puddles. Kept Light Aether (a thematic light puddle) + the two event objects.
//   - Dropped ~40 ambient OID entries (wildlife, expedition NPCs, field objects) for legibility.
//
// Needs validation, highest value first:
//   - Right/Left Combination + Acclaim shapes: the extractor defaulted them to 40y cones. "Left/Right
//     Combination" is a directional side-cleave sequence — confirm the real shape/sequence.
//   - Occult Stone II shape (cone guess); Occult Aero/Aero III rect extents; Occult Tornado radius.
//   - FalseSpellbladeHoly is a 31.7s cast — likely a big raidwide/enrage; confirm.
//   - Which lingering actor is a real voidzone puddle (Light Aether / 0x4EBD / 0x1EA1A1) + radii.
//   - Arena is a guessed 27y circle at (-519, -641.7) — refine centre/shape (a 'Confluence' object bounds it?).
using System;
using Minerva;

namespace Minerva.Dawntrail.Foray.AlabasterBlade;

public enum OID : uint
{
    Boss = 0x4BBE,          // 'Alabaster Blade' R4.0 — the boss (self-casts the Occult kit)
    Helper = 0x233C,        // 'Alabaster Blade' R0.5 — invisible helper
    AlabasterGolem = 0x4BBF, // 'Alabaster Golem' R1.65 — add (casts Acclaim)
    Confluence = 0x1EC0D8,  // 'Confluence' R0.5 — event object (arena marker?)
    LightAether = 0x4BC0,   // 'Light Aether' R1.6 — plausible light puddle
    _1EA1A1 = 0x1EA1A1,     // '' R2 — event object, plausible puddle
    _4EBD = 0x4EBD,         // '' R1 — event object, plausible puddle
    // ~40 ambient wildlife (Crescent *), expedition NPCs and field objects were captured by the open-field
    // recording and dropped here.
}

public enum AID : uint
{
    EmbrittlingBlade = 47171, // Alabaster Blade->Self, 4.7s, x1
    Summon = 47154, // Alabaster Blade->Self, 2.7s, x5
    FourfoldAttackOrder = 47155, // Alabaster Blade->Self, 9.7s, x5
    Acclaim = 47157, // Alabaster Golem->Self, 11.7s, x18
    Acclaim2 = 47158, // Alabaster Golem->Self, 2.7s, x54
    RightLeftCombination = 47166, // Alabaster Blade->Self, 4.7s, x4
    OccultAeroIII = 47170, // Alabaster Blade->Self, 4.7s, x22
    LightPrayer = 47159, // Alabaster Blade->Self, 2.7s, x1
    FalseSpellbladeHoly = 47757, // Alabaster Blade->Self, 31.7s, x1
    OccultAero = 47163, // Alabaster Blade->Self, 4.7s, x10
    OccultTornado = 47165, // Alabaster Blade->Self, 4.7s, x10
    OccultStoneII = 47164, // Alabaster Blade->Self, 4.7s, x6
    LeftRightCombination = 47167, // Alabaster Blade->Self, 4.7s, x1
}

// --- likely-correct / reasonable-guess AOEs (verify against the recording) ---
sealed class OccultAero(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.OccultAero, new AOEShapeRect(50f, 5f));
sealed class OccultAeroIII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.OccultAeroIII, new AOEShapeRect(50f, 5f));
sealed class OccultTornado(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.OccultTornado, new AOEShapeCircle(5f));
// directional cleaves / cones — extractor defaulted to 40y cones; confirm shape + sequence
sealed class Acclaim(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Acclaim, new AOEShapeCone(40f, 45f.Degrees())); // TODO: confirm shape
sealed class Acclaim2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Acclaim2, new AOEShapeCone(40f, 45f.Degrees())); // TODO: confirm shape
sealed class RightLeftCombination(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RightLeftCombination, new AOEShapeCone(40f, 45f.Degrees())); // TODO: side-cleave sequence?
sealed class LeftRightCombination(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LeftRightCombination, new AOEShapeCone(40f, 45f.Degrees())); // TODO: side-cleave sequence?
sealed class OccultStoneII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.OccultStoneII, new AOEShapeCone(40f, 45f.Degrees())); // TODO: confirm shape

// --- shape unknown: hint only until the recording shows the real shape ---
sealed class EmbrittlingBlade(ModuleBase module) : Components.CastHint(module, (uint)AID.EmbrittlingBlade, "Embrittling Blade (shape TBD)");
sealed class Summon(ModuleBase module) : Components.CastHint(module, (uint)AID.Summon, "Summon (adds)");
sealed class FourfoldAttackOrder(ModuleBase module) : Components.CastHint(module, (uint)AID.FourfoldAttackOrder, "Fourfold Attack Order (shape TBD)");
sealed class LightPrayer(ModuleBase module) : Components.CastHint(module, (uint)AID.LightPrayer, "Light Prayer (shape TBD)");
sealed class FalseSpellbladeHoly(ModuleBase module) : Components.CastHint(module, (uint)AID.FalseSpellbladeHoly, "False Spellblade: Holy (31.7s — big cast)");

// --- voidzones: thematic light puddle + event objects kept; ambient wildlife pruned ---
sealed class LightAetherVoidzone(ModuleBase module) : Components.Voidzone(module, 1.6f, (uint)OID.LightAether); // TODO: confirm puddle + radius
sealed class EventVoidzone1(ModuleBase module) : Components.Voidzone(module, 2f, (uint)OID._1EA1A1); // TODO: confirm puddle + radius
sealed class EventVoidzone2(ModuleBase module) : Components.Voidzone(module, 1f, (uint)OID._4EBD); // TODO: confirm puddle + radius

sealed class AlabasterBladeStates : StateMachineBuilder
{
    public AlabasterBladeStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<EmbrittlingBlade>()
            .ActivateOnEnter<Summon>()
            .ActivateOnEnter<FourfoldAttackOrder>()
            .ActivateOnEnter<Acclaim>()
            .ActivateOnEnter<Acclaim2>()
            .ActivateOnEnter<RightLeftCombination>()
            .ActivateOnEnter<LeftRightCombination>()
            .ActivateOnEnter<OccultAeroIII>()
            .ActivateOnEnter<OccultAero>()
            .ActivateOnEnter<OccultTornado>()
            .ActivateOnEnter<OccultStoneII>()
            .ActivateOnEnter<LightPrayer>()
            .ActivateOnEnter<FalseSpellbladeHoly>()
            .ActivateOnEnter<LightAetherVoidzone>()
            .ActivateOnEnter<EventVoidzone1>()
            .ActivateOnEnter<EventVoidzone2>();
    }
}

[ModuleInfo(CFCID = 1093u, NameID = 14509u, Maturity = ModuleMaturity.WIP, Contributors = "Minerva extractor (North Horn; no BMR reference)")]
public sealed class AlabasterBlade(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(-519f, -641.7f), new ArenaBoundsCircle(27f)); // arena guessed — refine
