// Occult Crescent — North Horn. Boss 'Pallmagia' 0x4D8F (R3.5, confirmed in-game — a Blue-Mage-style caster).
// Duty CFC 1093 — coexists with the other North Horn modules (registry activates whichever boss OID is present).
// No BossmodReborn reference; shapes are recording-only. WIP, but this one has the correct boss and should
// activate/draw in-game (the earlier 'Arch Kelpie' mis-pick for this fight was deleted).
//
// Design choice: the draft split into 3 guessed phases (with guessed map-effect transitions). Collapsed to a
// SINGLE always-active phase so a wrong transition guess can't leave later mechanics dark — every component is
// AID-keyed, so nothing draws until its own cast fires, and having them all active is harmless.
//
// Cleanup / needs validation:
//   - Dropped all 7 auto-voidzones: 5 were Crescent wildlife (ambient), and Pallkeeper/Pallmagia(0x4D91) are
//     adds, not puddles. Re-add a real voidzone if Validate shows a lingering puddle.
//   - Plaincracker was classified as a tether (207); demoted to a plain self-circle (its usual BLU form). The
//     fact sheet shows 2 tethers — identify which mechanics actually tether and restore TetherAOEs there.
//   - Confirm shapes for the CastHint set (Great Whirlwind, Occult Missile, Lilliputian Lyric, Roulette,
//     Esoteric Instruction, Reverse Polarity, Magic Hammer) and the guessed cones (Bad Breath, Lyric 2).
//   - Arena is a guessed 28y square at (806.1, -569.9); refine, and there may be an arena change (0x1EC02B/C).
using System;
using Minerva;

namespace Minerva.Dawntrail.Foray.Pallmagia;

public enum OID : uint
{
    Boss = 0x4D8F,          // 'Pallmagia' R3.5 — the boss
    Helper = 0x233C,        // 'Pallmagia' R0.5 — invisible helper; casts most AOEs
    Pallkeeper = 0x4D90,    // 'Pallkeeper' R2.3 — add
    PallmagiaAdd = 0x4D91,  // 'Pallmagia' R1 — add
    _1EA1A1 = 0x1EA1A1,     // '' R2 — event object
    _1EC02B = 0x1EC02B,     // '' R0.5 — possible arena-change marker
    _1EC02C = 0x1EC02C,     // '' R0.5 — possible arena-change marker
    // ambient Crescent wildlife (Glutton / Nanka / Huwasi / Bile) captured by the open-field recording dropped here.
}

public enum AID : uint
{
    GreatWhirlwind = 49798, // Pallmagia->Self, 4.7s
    A49799 = 49799, // Pallmagia->Self, 4.7s
    GreatWhirlwind2 = 50450, // Pallmagia->Self, 4.7s, x15
    OccultMissile = 49795, // Pallmagia->Self, 3.7s
    OccultMissile2 = 49797, // Pallmagia->Self, 3.7s, x36
    LilliputianLyric = 49791, // Pallmagia->Self, 4.7s
    LilliputianLyric2 = 49792, // Pallmagia->Self, 4.7s
    Roulette = 49787, // Pallmagia->Self, 3.7s
    Summon = 49772, // Pallmagia->Self, 2.7s
    EsotericInstruction = 49774, // Pallmagia->Self, 12.7s
    ReversePolarity = 49775, // Pallmagia->Self, 4.7s
    Plaincracker = 49779, // Pallmagia->Self, 2.7s
    BadBreath = 49777, // Pallmagia->Self, 2.7s
    MagicHammer = 49793, // Pallmagia->Self, 2.7s
    MagicHammer2 = 49794, // Pallmagia->Self, 5.2s, x24
}

// --- likely-correct / reasonable-guess AOEs (verify against the recording) ---
sealed class GreatWhirlwind2(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.GreatWhirlwind2);
sealed class OccultMissile2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.OccultMissile2, new AOEShapeCircle(6f));
sealed class MagicHammer2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MagicHammer2, new AOEShapeCircle(8f));
sealed class BadBreath(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BadBreath, new AOEShapeCone(50f, 45f.Degrees())); // Bad Breath = cone (BLU); confirm size
sealed class Plaincracker(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Plaincracker, new AOEShapeCircle(30f)); // self-circle; confirm radius (was mis-classified as a tether)
sealed class LilliputianLyric2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LilliputianLyric2, new AOEShapeCone(40f, 45f.Degrees())); // TODO: confirm shape

// --- shape unknown: hint only until the recording shows the real shape ---
sealed class GreatWhirlwind(ModuleBase module) : Components.CastHint(module, (uint)AID.GreatWhirlwind, "Great Whirlwind (shape TBD)");
sealed class A49799(ModuleBase module) : Components.CastHint(module, (uint)AID.A49799, "Great Whirlwind? (shape TBD)");
sealed class OccultMissile(ModuleBase module) : Components.CastHint(module, (uint)AID.OccultMissile, "Occult Missile (shape TBD)");
sealed class LilliputianLyric(ModuleBase module) : Components.CastHint(module, (uint)AID.LilliputianLyric, "Lilliputian Lyric (shape TBD)");
sealed class Roulette(ModuleBase module) : Components.CastHint(module, (uint)AID.Roulette, "Roulette (shape TBD)");
sealed class Summon(ModuleBase module) : Components.CastHint(module, (uint)AID.Summon, "Summon (adds)");
sealed class EsotericInstruction(ModuleBase module) : Components.CastHint(module, (uint)AID.EsotericInstruction, "Esoteric Instruction (12.7s — big cast)");
sealed class ReversePolarity(ModuleBase module) : Components.CastHint(module, (uint)AID.ReversePolarity, "Reverse Polarity (shape TBD)");
sealed class MagicHammer(ModuleBase module) : Components.CastHint(module, (uint)AID.MagicHammer, "Magic Hammer (shape TBD)");

sealed class PallmagiaStates : StateMachineBuilder
{
    public PallmagiaStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<GreatWhirlwind>()
            .ActivateOnEnter<GreatWhirlwind2>()
            .ActivateOnEnter<A49799>()
            .ActivateOnEnter<OccultMissile>()
            .ActivateOnEnter<OccultMissile2>()
            .ActivateOnEnter<LilliputianLyric>()
            .ActivateOnEnter<LilliputianLyric2>()
            .ActivateOnEnter<Roulette>()
            .ActivateOnEnter<Summon>()
            .ActivateOnEnter<EsotericInstruction>()
            .ActivateOnEnter<ReversePolarity>()
            .ActivateOnEnter<Plaincracker>()
            .ActivateOnEnter<BadBreath>()
            .ActivateOnEnter<MagicHammer>()
            .ActivateOnEnter<MagicHammer2>();
    }
}

[ModuleInfo(CFCID = 1093u, NameID = 0u, Maturity = ModuleMaturity.WIP, Contributors = "Minerva extractor (North Horn; no BMR reference)")]
public sealed class Pallmagia(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(806.1f, -569.9f), new ArenaBoundsSquare(28f)); // arena guessed — refine
