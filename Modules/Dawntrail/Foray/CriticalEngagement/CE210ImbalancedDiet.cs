// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.CriticalEngagement.CE210ImbalancedDiet;

public enum OID : uint
{
    Algol = 0x4C4B, // R7.500, x1
    AlgolHelper = 0x4D87, // R6.000, x5
    CrescentTomato = 0x4C4C, // R0.900, x4
    CrescentOnion = 0x4C4D, // R0.900, x4
    UnknownActor = 0x4C4E, // R1.000, x0 (spawn during fight)
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 50644, // Algol->player, no cast, single-target
    Deathwall = 48118, // AlgolHelper->self, no cast, range 24-30 donut

    RottenOnion1 = 48112, // Helper->self, 2.0s cast, range 60 30-degree cone
    RottenOnion2 = 48110, // Helper->self, 4.0s cast, range 60 30-degree cone
    ShrillPeal = 50426, // Algol->self, 3.0s cast, ???
    ShrillPeal1 = 50427, // Helper->self, 4.0s cast, ???
    Inhale = 48101, // Algol->self, 2.0+1.0s cast, single-target
    Inhale1 = 48102, // Algol->self, no cast, single-target
    Inhale2 = 48104, // Algol2->self, 3.5s cast, range 60 30-degree cone
    Inhale3 = 48103, // Helper->CrescentTomato1/CrescentOnion, 0.7s cast, single-target
    Devour = 50469, // Helper->self, 6.8s cast, range 8 120-degree cone
    Regurgitomato = 48106, // Algol->location, no cast, single-target
    RottenTomato1 = 48109, // Helper->self, 4.0s cast, range 50 width 6 rect
    RottenTomato2 = 48111, // Helper->self, 2.0s cast, range 50 width 6 rect
    CursedScreech = 48100, // Algol->self, 5.0s cast, ???
    CursedScreech1 = 48971, // Helper->self, 6.0s cast, ???

    SpinningInhaleVisual = 48113, // Algol->self, 5.0s cast, range 30 30-degree cone // 3 casts, presumeably it does something different depending on the distance
    SpinningInhale1 = 50942, // AlgolHelper->self, no cast, range ?-30 donut
    SpinningInhale2 = 48114, // AlgolHelper->self, no cast, range ?-30 donut
    SpinningInhale3 = 48249, // Helper->self, no cast, range 7 30-degree cone

    UnknownWeaponskill2 = 48115, // Algol->self, no cast, single-target
    Devour1 = 48105, // Algol->self, no cast, range 12 ?-degree cone
    Devour2 = 50422, // Helper->self, 3.0s cast, range 12 120-degree cone
    Devour3 = 50467, // Helper->self, 3.0s cast, range 12 120-degree cone
    DigestedJuice = 48116, // Algol->self, 4.0s cast, range 40 width 50 rect
    DigestedJuice1 = 50423, // Algol->self, no cast, single-target
    DigestedJuice2 = 50424, // Helper->self, 4.0s cast, range 40 width 50 rect
    Malady = 48117, // Algol->self, no cast, range 12 circle
    Malady1 = 50425, // Helper->self, 3.0s cast, range 11 circle

    Regurgitonion = 48107, // Algol->location, no cast, single-target
}

[SkipLocalsInit]
// Both ids: the boss casts the telegraph and a helper casts the damage. Watching only the boss's left the
// helper unaccounted for, so nine hits in one pull read as "the module has no component for it" when they
// were the raidwide the module already knew about (2026-09-06 recording).
sealed class CursedScreech(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.CursedScreech, (uint)AID.CursedScreech1]);
[SkipLocalsInit]
sealed class ShrillPeal(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.ShrillPeal, (uint)AID.ShrillPeal1]);
[SkipLocalsInit]
sealed class Inhale(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Inhale2, new AOEShapeCone(60f, 15f.Degrees()));
[SkipLocalsInit]
sealed class DevourShort(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Devour, new AOEShapeCone(8f, 60f.Degrees()));
[SkipLocalsInit]
sealed class RottenOnion(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.RottenOnion1, (uint)AID.RottenOnion2], new AOEShapeCone(60f, 15f.Degrees()));
[SkipLocalsInit]
sealed class RottenTomato(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.RottenTomato1, (uint)AID.RottenTomato2], new AOEShapeRect(50f, 3f));
[SkipLocalsInit]
sealed class DevourLong(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.Devour2, (uint)AID.Devour3], new AOEShapeCone(12f, 60f.Degrees()));
[SkipLocalsInit]
sealed class DigestedJuice(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.DigestedJuice, (uint)AID.DigestedJuice2], new AOEShapeRect(40f, 25f));
[SkipLocalsInit]
sealed class Malady(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Malady1, 11f);

[SkipLocalsInit]
sealed class SpinningInhale(ModuleBase module) : Components.GenericRotatingAOE(module)
{
    private readonly AOEShapeCone cone = new(30f, 15f.Degrees());

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.SpinningInhaleVisual)
        {
            Sequences.Add(new(cone, spell.LocXZ, spell.Rotation, -15f.Degrees(), Module.CastFinishAt(spell, 0.15d), 0.15d, 25, 10));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.SpinningInhale3)
        {
            AdvanceSequence(caster.Position, spell.Rotation, World.CurrentTime);
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        // encourage AI to stay close to hitbox to dodge properly
        if (Sequences.Count != 0)
        {
            base.AddAIHints(slot, actor, assignment, hints);
            hints.AddForbiddenZone(new SDInvertedCircle(Module.Center, 6f), Sequences.Ref(0).NextActivation);
        }
    }
}

[SkipLocalsInit]
sealed class CE210ImbalancedDietStates : StateMachineBuilder
{
    public CE210ImbalancedDietStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<CursedScreech>()
            .ActivateOnEnter<ShrillPeal>()
            .ActivateOnEnter<Inhale>()
            .ActivateOnEnter<DevourShort>()
            .ActivateOnEnter<RottenOnion>()
            .ActivateOnEnter<RottenTomato>()
            .ActivateOnEnter<DevourLong>()
            .ActivateOnEnter<DigestedJuice>()
            .ActivateOnEnter<Malady>()
            .ActivateOnEnter<SpinningInhale>();
    }
}

[ModuleInfo(Group = ModuleGroup.CriticalEngagement, CFCID = 1093u, NameID = 54u, PrimaryActorOID = (uint)OID.Algol, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Gynorhino (ported from BMR)")]
[SkipLocalsInit]
public sealed class CE210ImbalancedDiet(WorldState ws, Actor primary) : ModuleBase(ws, primary, new WPos(765f, 0f).Quantized(), new ArenaBoundsCircle(24f))
{
    protected override bool CheckPull() => base.CheckPull() && Raid.Player()!.Position.InCircle(Module.Center, 24f);
}
