// Occult Crescent — North Horn. Boss 'Demi-Medusa' 0x4C6A (verified from recording: single instance, 29M HP;
// a lighter ~70s NM). Duty CFC 1093 — coexists with the other North Horn modules. No BossmodReborn reference;
// shapes are recording-only. WIP.
//
// Boss OID verified by HP: 0x4C6A is the boss; 0x4C6C is a 12-copy 'Demi-Medusa' add (~330K HP each) and
// 0x4EC1/0x4EC2/0x4CAE are zero-HP helper objects. 'Defective Lamia' (0x4DD5-8) are the adds that cast Cursed Sight.
//
// Cleanup / needs validation:
//   - Dropped both auto-voidzones (Crescent Adamantoise / Woolback) — ambient wildlife, not puddles.
//   - CursedSight / CursedSight2: MEDUSA THEME — "Cursed Sight" is very likely a GAZE (look away), not a cone.
//     Kept as cones so they draw danger, but confirm: if it's a gaze, swap to the Gaze component (dodging
//     sideways vs. looking away are different, so a cone here can mislead).
//   - Dark / Summon shapes unknown; LamianLesion cone unconfirmed.
//   - Arena is a guessed 21y circle at (-660.5, -55.8); refine.
using System;
using Minerva;

namespace Minerva.Dawntrail.Foray.DemiMedusa;

public enum OID : uint
{
    Boss = 0x4C6A,          // 'Demi-Medusa' R3.0 — the real boss (single instance, 29M HP; verified)
    DemiMedusaAdd = 0x4C6C, // 'Demi-Medusa' R1 — 12-copy add
    DefectiveLamia = 0x4DD6, // 'Defective Lamia' R2.5 — add (casts Cursed Sight); also 0x4DD5/7/8
    // ambient Crescent wildlife (Adamantoise / Woolback) captured by the open-field recording dropped here.
}

public enum AID : uint
{
    CursedSight = 48253, // Defective Lamia->Self, 4.7s, x12
    CursedSight2 = 48252, // Demi-Medusa->Self, 4.7s, x1
    LamianLesion = 48254, // Demi-Medusa->Self, 4.7s, x2
    Dark = 48255, // Demi-Medusa->Self, 2.7s, x1
    Dark2 = 48256, // Demi-Medusa->Self, 2.7s, x21
    Summon = 48300, // Demi-Medusa->Self, 2.7s, x1
}

// --- likely-correct / reasonable-guess AOEs (verify against the recording) ---
sealed class Dark2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Dark2, new AOEShapeCircle(6f));
sealed class LamianLesion(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LamianLesion, new AOEShapeCone(25f, 45f.Degrees())); // TODO: confirm shape
// Cursed Sight: likely a Medusa GAZE (look away), NOT a cone — drawn as cone for now; confirm and swap to Gaze.
sealed class CursedSight(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CursedSight, new AOEShapeCone(60f, 45f.Degrees())); // TODO: gaze?
sealed class CursedSight2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CursedSight2, new AOEShapeCone(60f, 45f.Degrees())); // TODO: gaze?

// --- shape unknown: hint only until the recording shows the real shape ---
sealed class Dark(ModuleBase module) : Components.CastHint(module, (uint)AID.Dark, "Dark (shape TBD)");
sealed class Summon(ModuleBase module) : Components.CastHint(module, (uint)AID.Summon, "Summon (adds)");

sealed class DemiMedusaStates : StateMachineBuilder
{
    public DemiMedusaStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<CursedSight>()
            .ActivateOnEnter<CursedSight2>()
            .ActivateOnEnter<LamianLesion>()
            .ActivateOnEnter<Dark>()
            .ActivateOnEnter<Dark2>()
            .ActivateOnEnter<Summon>();
    }
}

[ModuleInfo(CFCID = 1093u, NameID = 0u, Maturity = ModuleMaturity.WIP, Contributors = "Minerva extractor (North Horn; no BMR reference)")]
public sealed class DemiMedusa(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(-660.5f, -55.8f), new ArenaBoundsCircle(21f)); // arena guessed — refine
