// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Dungeon.D08Stigma.D082ArchLambda;

public enum OID : uint
{
    Boss = 0x3416, // R=6.0
    Helper = 0x233C
}
public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target

    AtomicFlame = 25524, // Boss->self, 5.0s cast, range 40 circle //Raidwide
    AutoMobileAssaultCannon = 25515, // Boss->self, 5.9s cast, single-target
    AutoMobileSniperCannon1 = 25520, // Boss->location, 7.0s cast, single-target
    AutoMobileSniperCannon2 = 25522, // Helper->self, no cast, range 40 width 6 rect 
    Entrench = 25521, // Helper->self, 7.5s cast, range 41 width 8 rect
    Tread1 = 25516, // Boss->location, no cast, width 8 rect charge
    Tread2 = 25517, // Boss->location, no cast, width 8 rect charge
    Unknown1 = 25514, // Boss->location, no cast, single-target
    Unknown2 = 25518, // Helper->location, 1.5s cast, width 8 rect charge
    WaveCannon = 25519, // Boss->self, 2.5s cast, range 40 180-degree cone
    Wheel = 25525 // Boss->player, 5.0s cast, single-target //Tankbuster
}

class Tread1(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.Tread1, 4);
class Tread2(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.Tread2, 4);
class Unknown2(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.Unknown2, 4);

class AutoMobileSniperCannon2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AutoMobileSniperCannon2, new AOEShapeRect(40, 3));
class Entrench(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Entrench, new AOEShapeRect(41, 4));

class WaveCannon(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WaveCannon, new AOEShapeCone(40, 90.Degrees()));

class AtomicFlame(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AtomicFlame);
class Wheel(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Wheel);

class D082ArchLambdaStates : StateMachineBuilder
{
    public D082ArchLambdaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Tread1>()
            .ActivateOnEnter<Tread2>()
            .ActivateOnEnter<Unknown2>()
            .ActivateOnEnter<AutoMobileSniperCannon2>()
            .ActivateOnEnter<Entrench>()
            .ActivateOnEnter<WaveCannon>()
            .ActivateOnEnter<AtomicFlame>()
            .ActivateOnEnter<Wheel>();
    }
}

[ModuleInfo(CFCID = 784u, NameID = 10403u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class D082ArchLambda(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(0, 0), new ArenaBoundsCircle(20));
