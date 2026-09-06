// Occult Crescent — North Horn. Boss 'Phantom Necromancer' 0x4BC1 (verified from recording: single instance,
// 154M HP; NameID 14512). It raises Long-dead adds (Explorer/Pirate) that explode. Duty CFC 1093 — coexists
// with the other North Horn modules. No BossmodReborn reference; shapes are recording-only. WIP.
//
// Cleanup / needs validation:
//   - Dropped all 3 auto-voidzones: 0x4C75 'Phantom Necromancer' is a 1073-HP puppet add (not a puddle),
//     Crescent Wraith / Belladonna are ambient wildlife.
//   - DarkII drawn as a big 50x25 rect; DarkFlare / ArcaneRevelation / RiseOfTheFallen shapes unknown
//     (RiseOfTheFallen raises the adds). Explosion2 (Long-dead Pirate) is a guessed cross.
//   - Arena is a guessed 19y square at (224.8, -859.7); refine (possible arena change on 0x1EBFF5).
using System;
using Minerva;

namespace Minerva.Dawntrail.Foray.PhantomNecromancer;

public enum OID : uint
{
    Boss = 0x4BC1,          // 'Phantom Necromancer' R4.0 — the boss (single instance, 154M HP; verified)
    NecromancerPuppet = 0x4C75, // 'Phantom Necromancer' R1.0 — 1073-HP puppet add
    Helper = 0x233C,        // 'Phantom Necromancer' R0.5 — invisible helper
    LongDeadExplorer = 0x4BC2, // 'Long-dead Explorer' — raised add, casts Explosion
    LongDeadPirate = 0x4BC3, // 'Long-dead Pirate' — raised add, casts Explosion2
    _1EA1A1 = 0x1EA1A1,     // '' R2 — event object
    // ambient Crescent wildlife (Wraith / Belladonna / Melia) captured by the open-field recording dropped here.
}

public enum AID : uint
{
    DarkII = 47181, // Phantom Necromancer->Self, 4.7s, x1
    RiseOfTheFallen = 47174, // Phantom Necromancer->Self, 2.7s, x4 (raises adds)
    Explosion = 47175, // Long-dead Explorer->Self, 1.7s, x120
    Explosion2 = 47176, // Long-dead Pirate->Self, 3.7s, x36
    DarkFlare = 47182, // Phantom Necromancer->Self, 4.7s, x6
    ArcaneRevelation = 47179, // Phantom Necromancer->Self, 2.7s, x3
    Necrosurge = 47180, // Phantom Necromancer->Self, 6.7s, x9
}

// --- likely-correct / reasonable-guess AOEs (verify against the recording) ---
sealed class Explosion(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Explosion, new AOEShapeCircle(8f));
sealed class Necrosurge(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Necrosurge, new AOEShapeRect(70f, 6f));
sealed class DarkII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DarkII, new AOEShapeRect(50f, 25f)); // TODO: confirm (large rect)
sealed class Explosion2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Explosion2, new AOEShapeCross(80f, 3.5f)); // TODO: confirm shape

// --- shape unknown: hint only until the recording shows the real shape ---
sealed class RiseOfTheFallen(ModuleBase module) : Components.CastHint(module, (uint)AID.RiseOfTheFallen, "Rise of the Fallen (raises adds)");
sealed class DarkFlare(ModuleBase module) : Components.CastHint(module, (uint)AID.DarkFlare, "Dark Flare (shape TBD)");
sealed class ArcaneRevelation(ModuleBase module) : Components.CastHint(module, (uint)AID.ArcaneRevelation, "Arcane Revelation (shape TBD)");

sealed class PhantomNecromancerStates : StateMachineBuilder
{
    public PhantomNecromancerStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<DarkII>()
            .ActivateOnEnter<RiseOfTheFallen>()
            .ActivateOnEnter<Explosion>()
            .ActivateOnEnter<Explosion2>()
            .ActivateOnEnter<DarkFlare>()
            .ActivateOnEnter<ArcaneRevelation>()
            .ActivateOnEnter<Necrosurge>();
    }
}

[ModuleInfo(CFCID = 1093u, NameID = 14512u, Maturity = ModuleMaturity.WIP, Contributors = "Minerva extractor (North Horn; no BMR reference)")]
public sealed class PhantomNecromancer(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(224.8f, -859.7f), new ArenaBoundsSquare(19f)); // arena guessed — refine
