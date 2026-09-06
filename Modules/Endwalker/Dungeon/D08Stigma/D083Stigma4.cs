// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Dungeon.D08Stigma.D083Stigma4;

public enum OID : uint
{
    Boss = 0x3428, // R6.990, x1
    OmegaFrame = 0x342A, // R8.995, x0 (spawn during fight)
    HybridDragon = 0x342B, // R5.000, x0 (spawn during fight)
    ProtoRocketPunch = 0x3429, // R2.500, x0 (spawn during fight)
    Helper = 0x233C
}

public enum AID : uint
{
    AITakeover = 25641, // Boss->self, 5.0s cast, single-target
    AtomicRay = 25654, // Boss->self, 5.0s cast, range 50 circle //Raidwide

    ElectromagneticReleaseInVisual = 25649, // Boss->self, no cast, single-target
    ElectromagneticReleaseIn = 25650, // Helper->self, 10.0s cast, range 7-60 donut
    ElectromagneticReleaseOutVisual = 25651, // Boss->self, no cast, single-target
    ElectromagneticReleaseOut = 25652, // Helper->self, 10.0s cast, range 16 circle

    FireBreath = 25646, // HybridDragon->self, 7.0s cast, range 40 120-degree cone

    Mindhack = 25648, // Boss->self, 5.0s cast, range 50 circle //Raidwide and forced march
    MultiAITakeover = 27723, // Boss->self, 5.0s cast, single-target

    Plasmafodder = 25653, // Helper->player, no cast, single-target

    ProtoWaveCannon1 = 25642, // OmegaFrame->self, 7.0s cast, range 60 180-degree cone
    ProtoWaveCannon2 = 25643, // OmegaFrame->self, 7.0s cast, range 60 180-degree cone

    Rush = 25645, // ProtoRocketPunch->location, 5.0s cast, width 5 rect charge

    SelfDestructOmega = 25644, // OmegaFrame->self, 15.0s cast, range 60 circle
    SelfDestructDragon = 25647, // HybridDragon->self, 15.0s cast, range 60 circle

    Touchdown = 26873 // HybridDragon->self, 2.0s cast, range 7 circle
}

public enum SID : uint
{
    AboutFace = 1959, // Boss->player, extra=0x0
    ForwardMarch = 1958, // Boss->player, extra=0x0
    RightFace = 1961, // Boss->player, extra=0x0
    LeftFace = 1960 // Boss->player, extra=0x0
}

class Mindhack(ModuleBase module) : Components.StatusDrivenForcedMarch(module, 2f, (uint)SID.ForwardMarch, (uint)SID.AboutFace, (uint)SID.LeftFace, (uint)SID.RightFace, activationLimit: 8);

class ElectromagneticReleaseIn(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ElectromagneticReleaseIn, new AOEShapeDonut(7f, 60f));
class ElectromagneticReleaseOut(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ElectromagneticReleaseOut, 16f);

class FireBreath(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FireBreath, new AOEShapeCone(40f, 60f.Degrees()));
class Touchdown(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Touchdown, 7f);

class ProtoWaveCannon(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.ProtoWaveCannon1, (uint)AID.ProtoWaveCannon2], new AOEShapeCone(60f, 90f.Degrees()));
class Rush(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.Rush, 2.5f);

class SelfDestructOmega(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.SelfDestructOmega);
class SelfDestructDragon(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.SelfDestructDragon);
class AtomicRay(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AtomicRay);

class D083Stigma4States : StateMachineBuilder
{
    public D083Stigma4States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Mindhack>()
            .ActivateOnEnter<ElectromagneticReleaseIn>()
            .ActivateOnEnter<ElectromagneticReleaseOut>()
            .ActivateOnEnter<FireBreath>()
            .ActivateOnEnter<Touchdown>()
            .ActivateOnEnter<ProtoWaveCannon>()
            .ActivateOnEnter<Rush>()
            .ActivateOnEnter<SelfDestructOmega>()
            .ActivateOnEnter<SelfDestructDragon>()
            .ActivateOnEnter<AtomicRay>();
    }
}

[ModuleInfo(CFCID = 784u, NameID = 10404u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class D083Stigma4(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(0, 2.5f), new ArenaBoundsSquare(20));
