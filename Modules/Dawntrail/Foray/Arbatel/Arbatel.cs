// Occult Crescent — North Horn. Boss 'Arbatel' 0x4BD3 (confirmed in-game; the extractor mis-picked the
// 'Page 16' add as boss because the fight is add/helper-driven). Duty CFC 1093 (zone 1346), 3 phases.
// There is NO BossmodReborn module for this fight (novel North Horn content), so every shape/timing here
// comes from the recording alone — MANY are extractor placeholders. WIP: validate against a recording
// (/mine -> Replay -> Validate) before trusting, and Preview the draft to catch obvious misplacements.
//
// Needs validation, highest value first:
//   - Knowledge-level mechanic: ~15 Arbatel casts (KnowledgeLevel3Flare* / PrimeKnowledgeLevelDeath* /
//     KnowledgeLevel4Holy*), all 10.7s, all defaulted to AOEShapeCone(25, 45deg). Same name + same timing
//     = almost certainly a tower / knowledge-soak set, NOT a pile of identical cones. Rework as a custom
//     GenericAOEs (or tower component) once the recording shows the real shape/positions.
//   - Tethers: the fact sheet has only ONE tether, but TetherAOEs(245) got applied to ~6 AIDs. Keep the
//     real one; the rest simply never fire, but confirm and demote them to CastHint/SimpleAOEs.
//   - Phase transitions are guessed map-effects (TODO). Confirm P1->P2->P3 triggers via the Validate run;
//     if a phase never enters, its mechanics show up as uncovered.
//   - Voidzone radii (Urolith 4.5, Weapon 2) are hitbox estimates.
//   - Arena is a guessed 27y square at (658.9, 659.2); refine the centre/shape, and there may be an arena
//     change when 0x1EBFCD appears (see the commented ArenaChange below).
using System;
using Minerva;

namespace Minerva.Dawntrail.Foray.Arbatel;

public enum OID : uint
{
    Boss = 0x4BD3,          // 'Arbatel' R3.06 — the real boss (confirmed in North Horn)
    Page16 = 0x4BD5,        // 'Page 16' R1.95 — add (extractor mis-picked this as boss)
    _0 = 0x0,               // '' R1.94
    Helper = 0x233C,        // 'Arbatel' R0.5 — invisible helper; casts most of the AOEs
    CrescentUrolith = 0x4E09, // 'Crescent Urolith' R4.5 [voidzone?]
    CrescentDhara = 0x4E0A, // 'Crescent Dhara' R1.69
    CrescentBibliotaph = 0x4E0B, // 'Crescent Bibliotaph' R2.28
    _1EA1A1 = 0x1EA1A1,     // '' R2
    TreasureCoffer = 0x7E0, // 'Treasure Coffer' R0.5
    _6DF = 0x6DF,           // '' R0.5
    Page512 = 0x4BD7,       // 'Page 512' R1.95 — add
    _1EBFCD = 0x1EBFCD,     // '' R0.5 — possible arena-change marker
    CrescentWeapon = 0x4E0C, // 'Crescent Weapon' R2 [voidzone?]
    _2B3 = 0x2B3,           // '' R0.5
    _2CA = 0x2CA,           // '' R0.5
    Page8 = 0x4BD6,         // 'Page 8' R1.5 — add
    _4BD8 = 0x4BD8,         // '' R2.4
    _6EB = 0x6EB,           // '' R0.5
    Page64 = 0x4BD4,        // 'Page 64' R2.4 — add
    _17C = 0x17C,           // '' R0.5
}

public enum AID : uint
{
    KnowledgeLevelCorrection = 47296, // Arbatel->Self, 4.7s cast, x5, P1
    Summon = 49055, // Arbatel->Self, 2.7s cast, x5, P1
    Summon2 = 47307, // Arbatel->Self, 2.7s cast, x14, P1
    PrimeKnowledgeLevelDeath = 50561, // Arbatel->Self, 10.7s cast, x2, P1
    KnowledgeLevel3Flare = 47309, // Arbatel->Self, 10.7s cast, x1, P1
    KnowledgeLevel3Flare2 = 50555, // Arbatel->Self, 10.7s cast, x2, P1
    PrimeKnowledgeLevelDeath2 = 49879, // Arbatel->Self, 10.7s cast, x1, P1
    KnowledgeLevel3Flare3 = 47316, // Page 16->Self, 10.7s cast, x4, P1
    PrimeKnowledgeLevelDeath3 = 47318, // Page 512->Self, 10.7s cast, x5, P1
    Marginalia = 47327, // Arbatel->Self, 4.7s cast, x24, P1
    Marginalia2 = 47328, // Arbatel->Self, 4.7s cast, x8, P1
    PrimeKnowledgeLevelDeath4 = 50560, // Arbatel->Self, 10.7s cast, x8, P1
    KnowledgeLevel4Holy = 50559, // Arbatel->Self, 10.7s cast, x4, P1
    KnowledgeLevel3Flare4 = 47312, // Arbatel->Self, 10.7s cast, x3, P1
    KnowledgeLevel3Flare5 = 50558, // Arbatel->Self, 10.7s cast, x6, P1
    KnowledgeLevel4Holy2 = 47313, // Arbatel->Self, 10.7s cast, x2, P1
    PrimeKnowledgeLevelDeath5 = 47314, // Arbatel->Self, 10.7s cast, x4, P1
    KnowledgeLevel4Holy3 = 47317, // Page 8->Self, 10.7s cast, x2, P1
    CoverToCover = 47302, // Arbatel->Self, 3.7s cast, x2, P2
    CoverToCover2 = 47303, // Arbatel->Self, 0.7s cast, x2, P2
    UnboundInk = 49492, // Arbatel->Self, 3.7s cast, x2, P2
    BookDrop = 47319, // Arbatel->Self, 2.7s cast, x2, P2
    BookDrop2 = 47322, // OID4BD8->Self, 7.7s cast, x12, P2
    ThunderII = 47324, // Arbatel->Self, 3.7s cast, x40, P2
    FireII = 47325, // Arbatel->Self, 4.7s cast, x8, P2
    FireII2 = 47326, // Arbatel->Self, 4.7s cast, x2, P2
    KnowledgeLevel5Death = 47311, // Arbatel->Self, 10.7s cast, x3, P3
    KnowledgeLevel5Death2 = 50557, // Arbatel->Self, 10.7s cast, x6, P3
    KnowledgeLevel5Death3 = 47315, // Page 64->Self, 10.7s cast, x3, P3
    ArcaneRule = 47304, // Arbatel->Self, 5.7s cast, x2, P3
    QuadRule = 47305, // Arbatel->Self, 5.7s cast, x2, P3
    HorizontalRule = 47306, // Arbatel->Self, 1.7s cast, x32, P3
    Blot = 47301, // Arbatel->Self, 7.7s cast, x12, P3
    Blot2 = 47300, // Arbatel->Self, 2.7s cast, x2, P3
}

// --- classified / likely-correct (still verify against the recording) ---
sealed class Summon2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Summon2, new AOEShapeCircle(4f));
sealed class Marginalia(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Marginalia);
sealed class UnboundInk(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.UnboundInk, new AOEShapeCircle(9f));
sealed class BookDrop2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BookDrop2, new AOEShapeCircle(3f));
sealed class ThunderII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThunderII, new AOEShapeRect(50f, 2.5f));
sealed class HorizontalRule(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HorizontalRule, new AOEShapeRect(50f, 3f));
sealed class Blot(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Blot, new AOEShapeCircle(15f));
sealed class CrescentUrolithVoidzone(ModuleBase module) : Components.Voidzone(module, 4.5f, (uint)OID.CrescentUrolith); // TODO: confirm voidzone radius
sealed class CrescentWeaponVoidzone(ModuleBase module) : Components.Voidzone(module, 2f, (uint)OID.CrescentWeapon); // TODO: confirm voidzone radius

// --- placeholder shape unknown: hint only until the recording shows the real shape ---
sealed class KnowledgeLevelCorrection(ModuleBase module) : Components.CastHint(module, (uint)AID.KnowledgeLevelCorrection, "Knowledge Level Correction (shape TBD)");
sealed class Summon(ModuleBase module) : Components.CastHint(module, (uint)AID.Summon, "Summon (shape TBD)");
sealed class Marginalia2(ModuleBase module) : Components.CastHint(module, (uint)AID.Marginalia2, "Marginalia (shape TBD)");
sealed class BookDrop(ModuleBase module) : Components.CastHint(module, (uint)AID.BookDrop, "Book Drop (shape TBD)");
sealed class FireII2(ModuleBase module) : Components.CastHint(module, (uint)AID.FireII2, "Fire II (shape TBD)");
sealed class ArcaneRule(ModuleBase module) : Components.CastHint(module, (uint)AID.ArcaneRule, "Arcane Rule (shape TBD)");
sealed class Blot2(ModuleBase module) : Components.CastHint(module, (uint)AID.Blot2, "Blot (shape TBD)");

// --- KNOWLEDGE-LEVEL set: defaulted to cones by the extractor; almost certainly a tower/soak mechanic.
//     Left as SimpleAOEs cones so they draw *something*, but treat every shape here as unconfirmed. ---
sealed class KnowledgeLevel3Flare3(ModuleBase module) : Components.CastHint(module, (uint)AID.KnowledgeLevel3Flare3, "Knowledge Level 3: Flare (shape TBD)");
sealed class PrimeKnowledgeLevelDeath3(ModuleBase module) : Components.CastHint(module, (uint)AID.PrimeKnowledgeLevelDeath3, "Prime Knowledge Level: Death (shape TBD)");
sealed class KnowledgeLevel4Holy3(ModuleBase module) : Components.CastHint(module, (uint)AID.KnowledgeLevel4Holy3, "Knowledge Level 4: Holy (shape TBD)");
sealed class KnowledgeLevel5Death3(ModuleBase module) : Components.CastHint(module, (uint)AID.KnowledgeLevel5Death3, "Knowledge Level 5: Death (shape TBD)");
sealed class PrimeKnowledgeLevelDeath4(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PrimeKnowledgeLevelDeath4, new AOEShapeCone(25f, 45f.Degrees())); // TODO: confirm shape (likely tower)
sealed class KnowledgeLevel4Holy(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.KnowledgeLevel4Holy, new AOEShapeCone(25f, 45f.Degrees())); // TODO: confirm shape (likely tower)
sealed class KnowledgeLevel3Flare4(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.KnowledgeLevel3Flare4, new AOEShapeCone(25f, 45f.Degrees())); // TODO: confirm shape (likely tower)
sealed class KnowledgeLevel3Flare5(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.KnowledgeLevel3Flare5, new AOEShapeCone(25f, 45f.Degrees())); // TODO: confirm shape (likely tower)
sealed class KnowledgeLevel4Holy2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.KnowledgeLevel4Holy2, new AOEShapeCone(25f, 45f.Degrees())); // TODO: confirm shape (likely tower)
sealed class PrimeKnowledgeLevelDeath5(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PrimeKnowledgeLevelDeath5, new AOEShapeCone(25f, 45f.Degrees())); // TODO: confirm shape (likely tower)

// --- tether 245: fact sheet shows only ONE real tether — verify which AID actually tethers, demote the rest ---
sealed class PrimeKnowledgeLevelDeath(ModuleBase module) : Components.TetherAOEs(module, 245u, (uint)AID.PrimeKnowledgeLevelDeath, new AOEShapeCone(25f, 45f.Degrees())); // TODO: confirm tether target/shape
sealed class KnowledgeLevel3Flare(ModuleBase module) : Components.TetherAOEs(module, 245u, (uint)AID.KnowledgeLevel3Flare, new AOEShapeCone(25f, 45f.Degrees())); // TODO: confirm tether target/shape
sealed class KnowledgeLevel3Flare2(ModuleBase module) : Components.TetherAOEs(module, 245u, (uint)AID.KnowledgeLevel3Flare2, new AOEShapeCone(25f, 45f.Degrees())); // TODO: confirm tether target/shape
sealed class PrimeKnowledgeLevelDeath2(ModuleBase module) : Components.TetherAOEs(module, 245u, (uint)AID.PrimeKnowledgeLevelDeath2, new AOEShapeCone(25f, 45f.Degrees())); // TODO: confirm tether target/shape
sealed class KnowledgeLevel5Death(ModuleBase module) : Components.TetherAOEs(module, 245u, (uint)AID.KnowledgeLevel5Death, new AOEShapeCone(25f, 45f.Degrees())); // TODO: confirm tether target/shape
sealed class KnowledgeLevel5Death2(ModuleBase module) : Components.TetherAOEs(module, 245u, (uint)AID.KnowledgeLevel5Death2, new AOEShapeCone(25f, 45f.Degrees())); // TODO: confirm tether target/shape

// --- P2/P3 shapes to confirm ---
sealed class CoverToCover(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CoverToCover, new AOEShapeCone(30f, 45f.Degrees())); // TODO: confirm shape
sealed class CoverToCover2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CoverToCover2, new AOEShapeCone(30f, 45f.Degrees())); // TODO: confirm shape
sealed class FireII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FireII, new AOEShapeCone(60f, 45f.Degrees())); // TODO: confirm shape
sealed class QuadRule(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.QuadRule, new AOEShapeCross(25f, 5f)); // TODO: confirm shape

// arena may change when '' (OID 0x1EBFCD) appears — set the new bounds, then uncomment + ActivateOnEnter:
// sealed class Arena_1EBFCD(ModuleBase module) : Components.ArenaChange(module, new ArenaBoundsCircle(20f), triggerOID: (uint)OID._1EBFCD);

sealed class ArbatelStates : StateMachineBuilder
{
    public ArbatelStates(ModuleBase module) : base(module)
    {
        this.Phase("P1")
            .ActivateOnEnter<KnowledgeLevelCorrection>()
            .ActivateOnEnter<Summon>()
            .ActivateOnEnter<Summon2>()
            .ActivateOnEnter<PrimeKnowledgeLevelDeath>()
            .ActivateOnEnter<KnowledgeLevel3Flare>()
            .ActivateOnEnter<KnowledgeLevel3Flare2>()
            .ActivateOnEnter<PrimeKnowledgeLevelDeath2>()
            .ActivateOnEnter<KnowledgeLevel3Flare3>()
            .ActivateOnEnter<PrimeKnowledgeLevelDeath3>()
            .ActivateOnEnter<Marginalia>()
            .ActivateOnEnter<Marginalia2>()
            .ActivateOnEnter<PrimeKnowledgeLevelDeath4>()
            .ActivateOnEnter<KnowledgeLevel4Holy>()
            .ActivateOnEnter<KnowledgeLevel3Flare4>()
            .ActivateOnEnter<KnowledgeLevel3Flare5>()
            .ActivateOnEnter<KnowledgeLevel4Holy2>()
            .ActivateOnEnter<PrimeKnowledgeLevelDeath5>()
            .ActivateOnEnter<KnowledgeLevel4Holy3>()
            .ActivateOnEnter<CrescentUrolithVoidzone>()
            .ActivateOnEnter<CrescentWeaponVoidzone>()
            .TransitionOnMapEffect((byte)24, 131073u); // TODO: confirm phase transition (HP %? map effect?)
        this.Phase("P2")
            .ActivateOnEnter<CoverToCover>()
            .ActivateOnEnter<CoverToCover2>()
            .ActivateOnEnter<UnboundInk>()
            .ActivateOnEnter<BookDrop>()
            .ActivateOnEnter<BookDrop2>()
            .ActivateOnEnter<ThunderII>()
            .ActivateOnEnter<FireII>()
            .ActivateOnEnter<FireII2>()
            .TransitionOnMapEffect((byte)26, 2097168u); // TODO: confirm phase transition (HP %? map effect?)
        this.Phase("P3")
            .ActivateOnEnter<KnowledgeLevel5Death>()
            .ActivateOnEnter<KnowledgeLevel5Death2>()
            .ActivateOnEnter<KnowledgeLevel5Death3>()
            .ActivateOnEnter<ArcaneRule>()
            .ActivateOnEnter<QuadRule>()
            .ActivateOnEnter<HorizontalRule>()
            .ActivateOnEnter<Blot>()
            .ActivateOnEnter<Blot2>();
    }
}

[ModuleInfo(CFCID = 1093u, NameID = 0u, Maturity = ModuleMaturity.WIP, Contributors = "Minerva extractor (North Horn; no BMR reference)")]
public sealed class Arbatel(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(658.9f, 659.2f), new ArenaBoundsSquare(27f)); // arena guessed — refine
