// Dawntrail dungeon (CFC 1064, zone 1314). Boss 'Treno Catoblepas' 0x4841 (verified from recording: single
// instance, 12.2M HP; NameID 14270) — a lightning/petrify beast. No BossmodReborn reference; recording-only. WIP.
//
// IMPORTANT — Trust NPC casts stripped: the recording was made with Duty Support, so the raw draft included
// ~17 casts from Y'shtola / Alisaie / Thancred's Avatars (Jolt III, Verstone, Verthunder II, and all the
// *OfTheSeventhDawn spells). Those are ALLY casts aimed at the boss, NOT player dangers — "ignore other players"
// filters real players but not Trust NPCs. Only Treno's own self/target casts are kept here.
//
// Cleanup / needs validation:
//   - Dropped the 3 auto-voidzones (0x4851/4842/4843): they're add mobs (188K HP, multiple instances), not puddles.
//   - Petribreath is the catoblepas petrify — likely a GAZE (look away), drawn as a cone for now; confirm.
//   - Ray of Lightning is targeted (line/tankbuster) — left as a hint. Collapsed the draft's 4 guessed phases
//     into one always-active phase (components are AID-keyed).
//   - Arena is a guessed 18y circle at (83, 369.9); refine.
using System;
using Minerva;

namespace Minerva.Dawntrail.Dungeon.TrenoCatoblepas;

public enum OID : uint
{
    Boss = 0x4841,          // 'Treno Catoblepas' R4.5 — the boss (single instance, 12.2M HP; verified)
    Helper = 0x233C,        // 'Treno Catoblepas' R0.5 — invisible helper
    Add1 = 0x4851,          // add (188K HP, x4)
    Add2 = 0x4842,          // add (188K HP, x3)
    Add3 = 0x4843,          // add (188K HP, x6)
}

public enum AID : uint
{
    Earthquake = 43327, // Treno Catoblepas->Self, 4.7s, x4
    ThunderII = 43331, // Treno Catoblepas->Self, 4.7s, x4
    ThunderII2 = 43332, // Treno Catoblepas->Self, 4.7s, x35
    ThunderII3 = 43333, // Treno Catoblepas->Target, 4.7s, x16
    ThunderIII = 43329, // Treno Catoblepas->Target, 4.7s, x2
    BedevilingLight = 43330, // Treno Catoblepas->Self, 6.7s, x4
    RayOfLightning = 44825, // Treno Catoblepas->Target, 5.7s, x2
    Petribreath = 43335, // Treno Catoblepas->Self, 4.7s, x1
}

// --- likely-correct / reasonable-guess AOEs (verify against the recording) ---
sealed class Earthquake(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Earthquake, new AOEShapeCircle(30f));
sealed class BedevilingLight(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BedevilingLight, new AOEShapeCircle(30f));
sealed class ThunderII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThunderII, new AOEShapeCircle(5f));
sealed class ThunderII2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThunderII2, new AOEShapeCircle(5f));
sealed class ThunderII3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThunderII3, new AOEShapeCircle(5f));
sealed class ThunderIII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThunderIII, new AOEShapeCircle(4f));
sealed class Petribreath(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Petribreath, new AOEShapeCone(30f, 45f.Degrees())); // TODO: petrify GAZE? (look away, not a cone)

// --- targeted: hint only ---
sealed class RayOfLightning(ModuleBase module) : Components.CastHint(module, (uint)AID.RayOfLightning, "Ray of Lightning (targeted)");

sealed class TrenoCatoblepasStates : StateMachineBuilder
{
    public TrenoCatoblepasStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<Earthquake>()
            .ActivateOnEnter<BedevilingLight>()
            .ActivateOnEnter<ThunderII>()
            .ActivateOnEnter<ThunderII2>()
            .ActivateOnEnter<ThunderII3>()
            .ActivateOnEnter<ThunderIII>()
            .ActivateOnEnter<Petribreath>()
            .ActivateOnEnter<RayOfLightning>();
    }
}

[ModuleInfo(CFCID = 1064u, NameID = 14270u, Maturity = ModuleMaturity.WIP, Contributors = "Minerva extractor (Trust casts stripped)")]
public sealed class TrenoCatoblepas(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(83f, 369.9f), new ArenaBoundsCircle(18f)); // arena guessed — refine
