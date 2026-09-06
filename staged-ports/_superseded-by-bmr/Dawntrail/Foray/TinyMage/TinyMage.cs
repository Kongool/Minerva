// Occult Crescent — North Horn. Boss 'Tiny Mage' 0x4C6D (verified from recording: single instance, 180M HP;
// NameID 14795). A caster boss with Tiny Apprentice adds (0x4C6E, a 37-instance swarm) and Flare/Holy/Arcane
// Spheres. Duty CFC 1093 — coexists with the other North Horn modules. No BossmodReborn reference; WIP.
//
// Cleanup / needs validation:
//   - Zero-width bugs: TinyQuakeIII3/4 were Rect(_, 0) (draw nothing) -> CastHint pending real shape.
//   - Pruned 5 of 6 auto-voidzones: Crescent Elftoad/Worm are ambient wildlife, 0x4D55 'Tiny Mage' is a
//     6.8M add, Flare/Holy Spheres are caster orbs (not puddles). Kept the event-object 0x4EBB only.
//   - Many casts are CastHint placeholders (the Arcane Aggregation / Recharge / Small-For-One / All-For-One
//     / Meteor set); confirm shapes. Meteor is a 129.7s cast (likely the big/enrage). TinyBlizzardIII cone
//     unconfirmed. Fact sheet shows 2 tethers — not yet classified; identify and add TetherAOEs.
//   - Arena is a guessed 19y square at (151.5, 715.6); refine.
using System;
using Minerva;

namespace Minerva.Dawntrail.Foray.TinyMage;

public enum OID : uint
{
    Boss = 0x4C6D,          // 'Tiny Mage' — the boss (single instance, 180M HP; verified)
    Helper = 0x233C,        // 'Tiny Mage' R0.5 — invisible helper; casts most AOEs
    TinyMageAdd = 0x4D55,   // 'Tiny Mage' R1.0 — 6.8M add
    TinyApprentice = 0x4C6E, // 'Tiny Apprentice' R1.0 — 37-instance add swarm (Arcane Aggregation / Recharge)
    ArcaneSphere = 0x4C74,  // 'Arcane Sphere' — casts Meteor / Comet
    _4EBB = 0x4EBB,         // '' R1.75 — event object, plausible puddle
    _1EA1A1 = 0x1EA1A1,     // '' R2 — event object
    // ambient Crescent wildlife (Elftoad / Worm) and caster spheres captured by the open-field recording dropped here.
}

public enum AID : uint
{
    SmallForOne = 48306, ArcaneAggregation = 48307, Recharge = 48309, TinyFlare = 48311,
    TinyThunderIII = 48329, ArcaneAggregation2 = 49719, ArcaneAggregation3 = 49718, TinyHoly = 48312,
    TinyQuakeIII = 48322, TinyQuakeIII2 = 48323, TinyQuakeIII3 = 48324, TinyQuakeIII4 = 48325,
    AllForOne = 50762, Meteor = 48326, Comet = 48327, ArcaneAggregation4 = 48308,
    DiminutiveDualcast = 48317, TinyBlizzardIII = 48319, TinyFireIII = 48318, Recharge2 = 48310,
    TinyMeteor = 48320, TinyMeteor2 = 48321,
}

// --- likely-correct / reasonable-guess AOEs (verify against the recording) ---
sealed class TinyFlare(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TinyFlare, new AOEShapeCircle(18f));
sealed class TinyFireIII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TinyFireIII, new AOEShapeCircle(14f));
sealed class TinyQuakeIII2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TinyQuakeIII2, new AOEShapeCircle(10f));
sealed class TinyMeteor2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TinyMeteor2, new AOEShapeCircle(6f));
sealed class TinyBlizzardIII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TinyBlizzardIII, new AOEShapeCone(40f, 45f.Degrees())); // TODO: confirm shape
sealed class TinyHoly(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.TinyHoly);
sealed class Comet(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Comet);

// --- shape unknown / bugged in the draft: hint only until the recording shows the real shape ---
sealed class TinyQuakeIII3(ModuleBase module) : Components.CastHint(module, (uint)AID.TinyQuakeIII3, "Tiny Quake III (shape TBD)"); // was Rect(20,0)
sealed class TinyQuakeIII4(ModuleBase module) : Components.CastHint(module, (uint)AID.TinyQuakeIII4, "Tiny Quake III (shape TBD)"); // was Rect(30,0)
sealed class TinyQuakeIII(ModuleBase module) : Components.CastHint(module, (uint)AID.TinyQuakeIII, "Tiny Quake III (shape TBD)");
sealed class TinyThunderIII(ModuleBase module) : Components.CastHint(module, (uint)AID.TinyThunderIII, "Tiny Thunder III (shape TBD)");
sealed class TinyMeteor(ModuleBase module) : Components.CastHint(module, (uint)AID.TinyMeteor, "Tiny Meteor (shape TBD)");
sealed class Meteor(ModuleBase module) : Components.CastHint(module, (uint)AID.Meteor, "Meteor (129.7s — big cast)");
sealed class SmallForOne(ModuleBase module) : Components.CastHint(module, (uint)AID.SmallForOne, "Small for One (shape TBD)");
sealed class AllForOne(ModuleBase module) : Components.CastHint(module, (uint)AID.AllForOne, "All for One (shape TBD)");
sealed class DiminutiveDualcast(ModuleBase module) : Components.CastHint(module, (uint)AID.DiminutiveDualcast, "Diminutive Dualcast (shape TBD)");
sealed class ArcaneAggregation(ModuleBase module) : Components.CastHint(module, (uint)AID.ArcaneAggregation, "Arcane Aggregation (shape TBD)");
sealed class ArcaneAggregation2(ModuleBase module) : Components.CastHint(module, (uint)AID.ArcaneAggregation2, "Arcane Aggregation (shape TBD)");
sealed class ArcaneAggregation3(ModuleBase module) : Components.CastHint(module, (uint)AID.ArcaneAggregation3, "Arcane Aggregation (shape TBD)");
sealed class ArcaneAggregation4(ModuleBase module) : Components.CastHint(module, (uint)AID.ArcaneAggregation4, "Arcane Aggregation (shape TBD)");
sealed class Recharge(ModuleBase module) : Components.CastHint(module, (uint)AID.Recharge, "Recharge");
sealed class Recharge2(ModuleBase module) : Components.CastHint(module, (uint)AID.Recharge2, "Recharge");

// --- voidzone: only the event-object puddle kept; wildlife/adds/caster-spheres pruned ---
sealed class EventVoidzone(ModuleBase module) : Components.Voidzone(module, 1.75f, (uint)OID._4EBB); // TODO: confirm puddle + radius

sealed class TinyMageStates : StateMachineBuilder
{
    public TinyMageStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<TinyFlare>()
            .ActivateOnEnter<TinyFireIII>()
            .ActivateOnEnter<TinyQuakeIII2>()
            .ActivateOnEnter<TinyMeteor2>()
            .ActivateOnEnter<TinyBlizzardIII>()
            .ActivateOnEnter<TinyHoly>()
            .ActivateOnEnter<Comet>()
            .ActivateOnEnter<TinyQuakeIII>()
            .ActivateOnEnter<TinyQuakeIII3>()
            .ActivateOnEnter<TinyQuakeIII4>()
            .ActivateOnEnter<TinyThunderIII>()
            .ActivateOnEnter<TinyMeteor>()
            .ActivateOnEnter<Meteor>()
            .ActivateOnEnter<SmallForOne>()
            .ActivateOnEnter<AllForOne>()
            .ActivateOnEnter<DiminutiveDualcast>()
            .ActivateOnEnter<ArcaneAggregation>()
            .ActivateOnEnter<ArcaneAggregation2>()
            .ActivateOnEnter<ArcaneAggregation3>()
            .ActivateOnEnter<ArcaneAggregation4>()
            .ActivateOnEnter<Recharge>()
            .ActivateOnEnter<Recharge2>()
            .ActivateOnEnter<EventVoidzone>();
    }
}

[ModuleInfo(CFCID = 1093u, NameID = 14795u, Maturity = ModuleMaturity.WIP, Contributors = "Minerva extractor (North Horn; no BMR reference)")]
public sealed class TinyMage(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(151.5f, 715.6f), new ArenaBoundsSquare(19f)); // arena guessed — refine
