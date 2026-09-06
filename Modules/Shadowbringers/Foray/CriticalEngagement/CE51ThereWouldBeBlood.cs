// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.CriticalEngagement.CE51ThereWouldBeBlood;

public enum OID : uint
{
    Boss = 0x319A, // R6.000, x1
    EmbitteredSoul = 0x319B, // R3.600, spawn during fight
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 6497, // Boss->player, no cast, single-target

    Electrocution = 24706, // Helper->self, no cast, range 25-30 donut, deathwall around arena
    CloudOfLocusts = 23568, // Boss->self, 6.5s cast, range 15 circle
    PlagueOfLocusts = 23569, // Boss->self, 6.5s cast, range 6-40 donut
    DivestingGale = 23570, // Helper->location, 3.0s cast, range 5 circle
    Camisado = 23469, // Boss->player, 4.0s cast, single-target, tankbuster
    DreadWind = 23470, // Boss->self, 4.0s cast, raidwide
    Teleport = 23580, // Boss->location, no cast, teleport
    GaleCannon = 21475, // Boss->self, 5.0s cast, range 30 width 12 rect aoe

    FlightOfTheMalefic1 = 24810, // Boss->self, 7.0s cast, single-target, visual (45, -45, -135)
    FlightOfTheMalefic2 = 24811, // Boss->self, 7.0s cast, single-target, visual (-45, -135, 135)
    FlightOfTheMalefic3 = 24812, // Boss->self, 7.0s cast, single-target, visual (45, -45, 135)
    FlightOfTheMalefic4 = 24813, // Boss->self, 7.0s cast, single-target, visual (45, -135, 135)
    FlightOfTheMaleficAOECone = 23579, // Helper->self, 7.0s cast, range 30 90-degree cone aoe
    FlightOfTheMaleficAOECenter = 24322, // Helper->location, 7.0s cast, range 6 circle aoe

    SummonDarkness = 23571, // Boss->self, 3.0s cast, single-target, visual
    TempestOfAnguish = 23572, // EmbitteredSoul->self, 6.5s cast, range 55 width 10 rect aoe
    TragicalGaze = 23573, // EmbitteredSoul->self, 7.5s cast, range 55 circle
}

sealed class CloudOfLocusts(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CloudOfLocusts, 15f);
sealed class PlagueOfLocusts(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PlagueOfLocusts, new AOEShapeDonut(6f, 40f));
sealed class DivestingGale(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DivestingGale, 5f);
sealed class Camisado(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Camisado);
sealed class DreadWind(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.DreadWind);
sealed class GaleCannon(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GaleCannon, new AOEShapeRect(30f, 6f));
sealed class FlightOfTheMaleficCone(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FlightOfTheMaleficAOECone, new AOEShapeCone(30f, 45f.Degrees()));
sealed class FlightOfTheMaleficCenter(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FlightOfTheMaleficAOECenter, 6f);
sealed class TempestOfAnguish(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TempestOfAnguish, new AOEShapeRect(55f, 5f));
sealed class TragicalGaze(ModuleBase module) : Components.CastGaze(module, (uint)AID.TragicalGaze);

sealed class CE51ThereWouldBeBloodStates : StateMachineBuilder
{
    public CE51ThereWouldBeBloodStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<CloudOfLocusts>()
            .ActivateOnEnter<PlagueOfLocusts>()
            .ActivateOnEnter<DivestingGale>()
            .ActivateOnEnter<Camisado>()
            .ActivateOnEnter<DreadWind>()
            .ActivateOnEnter<GaleCannon>()
            .ActivateOnEnter<FlightOfTheMaleficCone>()
            .ActivateOnEnter<FlightOfTheMaleficCenter>()
            .ActivateOnEnter<TempestOfAnguish>()
            .ActivateOnEnter<TragicalGaze>();
    }
}

[ModuleInfo(Group = ModuleGroup.CriticalEngagement, CFCID = 778u, NameID = 24u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")] // bnpcname=10064
public sealed class CE51ThereWouldBeBlood(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-390f, 230f), new ArenaBoundsCircle(25f))
{
    protected override bool CheckPull() => base.CheckPull() && Raid.Player()!.Position.InCircle(Center, 25f);
}
