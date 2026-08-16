// Occult Crescent — North Horn. Boss 'Elm Gigas' 0x4BDA — a wind-themed giant. Duty CFC 1093 (coexists with
// the other North Horn modules; registry activates whichever boss OID is present). No BossmodReborn reference;
// shapes are recording-only. WIP — validate against a recording before trusting.
//
// Boss OID note: TWO actors are named 'Elm Gigas' — 0x4BDA (extractor's pick, the primary caster) and
// 0x4BD9 (R3.5). Unlike the earlier mis-picks these are the SAME boss in two forms, so keying on the
// extractor's pick is safe; if it fails to activate in-game, swap Boss to 0x4BD9.
//
// Cleanup from the raw draft:
//   - AncientAeroIII was AOEShapeCircle(0f) = draws nothing; demoted to CastHint (radius unknown).
//   - Dropped all 4 auto-voidzones (Crescent Anila / Coeurl / Kaluk / Kargas) — ambient open-field wildlife,
//     not puddles. This fight surfaced no plausible real voidzone in the draft.
//
// Needs validation, highest value first:
//   - SpinningSweep (cone?), InspiritedHurricane2 (cross?), AncientAeroIII / AncientAeroIII2 shapes.
//   - The Inspirited* wind set (Crosswinds/Impact/Hurricane/Cyclone) — confirm the cross vs circle split.
//   - Arena is a guessed 31y square at (-390.2, 700.2); refine.
using System;
using Minerva;

namespace Minerva.Dawntrail.Foray.ElmGigas;

public enum OID : uint
{
    Boss = 0x4BDA,          // 'Elm Gigas' — the boss (primary combat form / caster)
    ElmGigasAlt = 0x4BD9,   // 'Elm Gigas' R3.5 — the other form (swap Boss to this if activation fails)
    Helper = 0x233C,        // 'Elm Gigas' R0.5 — invisible helper
    CrescentBomb = 0x4E88,  // 'Crescent Bomb' R2.4 — add (possible bomb mechanic; unconfirmed)
    _1EA1A1 = 0x1EA1A1,     // '' R2 — event object
    // ambient wildlife (Crescent Anila / Coeurl / Kaluk / Kargas) captured by the open-field recording dropped here.
}

public enum AID : uint
{
    AncientAeroIII = 48041, // Elm Gigas->Self, 4.7s, x21
    AncientAeroIII2 = 47544, // Elm Gigas->Self, 4.7s, x7
    UnbowedSpirit = 47530, // Elm Gigas->Self, 3.7s, x3
    SpinningSweep = 47541, // Elm Gigas->Self, 5.7s, x9
    InspiritedCrosswinds = 47533, // Elm Gigas->Self, 6.5s, x3
    InspiritedCrosswinds2 = 47535, // Elm Gigas->Self, 5.7s, x24
    InspiritedImpact = 47542, // Elm Gigas->Self, 2.7s, x4
    InspiritedImpact2 = 47543, // Elm Gigas->Self, 9.3s, x16
    InspiritedHurricane = 47537, // Elm Gigas->Self, 4.7s, x3
    InspiritedHurricane2 = 47538, // Elm Gigas->Self, 4.7s, x3
    InspiritedHurricane3 = 47536, // Elm Gigas->Self, 4.7s, x3
    AncientAero = 47540, // Elm Gigas->Self, 2.7s, x100
    InspiritedCyclone = 47534, // Elm Gigas->Self, 5.7s, x24
    InspiritedCyclone2 = 47532, // Elm Gigas->Self, 5.7s, x3
}

// --- likely-correct / reasonable-guess AOEs (verify against the recording) ---
sealed class AncientAero(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AncientAero, new AOEShapeRect(70f, 3f));
sealed class InspiritedCrosswinds2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.InspiritedCrosswinds2, new AOEShapeCross(60f, 4f)); // "Crosswinds" = cross; confirm extents
sealed class InspiritedImpact2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.InspiritedImpact2, new AOEShapeCircle(25f));
sealed class InspiritedHurricane(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.InspiritedHurricane, new AOEShapeCircle(12f));
sealed class InspiritedCyclone(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.InspiritedCyclone, new AOEShapeCircle(12f));
sealed class SpinningSweep(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SpinningSweep, new AOEShapeCone(40f, 45f.Degrees())); // TODO: confirm shape
sealed class InspiritedHurricane2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.InspiritedHurricane2, new AOEShapeCross(60f, 5f)); // TODO: confirm shape

// --- shape unknown / bugged in the draft: hint only until the recording shows the real shape ---
sealed class AncientAeroIII(ModuleBase module) : Components.CastHint(module, (uint)AID.AncientAeroIII, "Ancient Aero III (shape TBD)"); // was Circle(0)
sealed class AncientAeroIII2(ModuleBase module) : Components.CastHint(module, (uint)AID.AncientAeroIII2, "Ancient Aero III (shape TBD)");
sealed class UnbowedSpirit(ModuleBase module) : Components.CastHint(module, (uint)AID.UnbowedSpirit, "Unbowed Spirit (shape TBD)");
sealed class InspiritedCrosswinds(ModuleBase module) : Components.CastHint(module, (uint)AID.InspiritedCrosswinds, "Inspirited Crosswinds (shape TBD)");
sealed class InspiritedImpact(ModuleBase module) : Components.CastHint(module, (uint)AID.InspiritedImpact, "Inspirited Impact (shape TBD)");
sealed class InspiritedHurricane3(ModuleBase module) : Components.CastHint(module, (uint)AID.InspiritedHurricane3, "Inspirited Hurricane (shape TBD)");
sealed class InspiritedCyclone2(ModuleBase module) : Components.CastHint(module, (uint)AID.InspiritedCyclone2, "Inspirited Cyclone (shape TBD)");

sealed class ElmGigasStates : StateMachineBuilder
{
    public ElmGigasStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<AncientAero>()
            .ActivateOnEnter<AncientAeroIII>()
            .ActivateOnEnter<AncientAeroIII2>()
            .ActivateOnEnter<UnbowedSpirit>()
            .ActivateOnEnter<SpinningSweep>()
            .ActivateOnEnter<InspiritedCrosswinds>()
            .ActivateOnEnter<InspiritedCrosswinds2>()
            .ActivateOnEnter<InspiritedImpact>()
            .ActivateOnEnter<InspiritedImpact2>()
            .ActivateOnEnter<InspiritedHurricane>()
            .ActivateOnEnter<InspiritedHurricane2>()
            .ActivateOnEnter<InspiritedHurricane3>()
            .ActivateOnEnter<InspiritedCyclone>()
            .ActivateOnEnter<InspiritedCyclone2>();
    }
}

[ModuleInfo(CFCID = 1093u, NameID = 0u, Maturity = ModuleMaturity.WIP, Contributors = "Minerva extractor (North Horn; no BMR reference)")]
public sealed class ElmGigas(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(-390.2f, 700.2f), new ArenaBoundsSquare(31f)); // arena guessed — refine
