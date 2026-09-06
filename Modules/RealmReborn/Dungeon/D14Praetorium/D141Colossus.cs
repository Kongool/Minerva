// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Dungeon.D14Praetorium.D141Colossus;

public enum OID : uint
{
    Boss = 0x3872, // R3.5
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 870, // Boss->player, no cast, single-target

    CeruleumVent = 28474, // Boss->self, 5.0s cast, raidwide
    Teleport = 28467, // Boss->location, no cast, single-target, teleport
    PrototypeLaserAlpha = 28468, // Boss->self, 5.0s cast, single-target, visual
    IronKissAlpha1 = 28469, // Helper->location, 7.0s cast, range 6 circle aoe (inner set)
    IronKissAlpha2 = 28470, // Helper->location, 9.0s cast, range 6 circle aoe (outer set)
    PrototypeLaserBeta = 28471, // Boss->self, 5.0s cast, single-target, visual
    IronKissBeta = 28472, // Helper->player, 5.0s cast, range 5 circle spread
    GrandSword = 28473 // Boss->self, 5.0s cast, range 25 90-degree cone aoe
}

class CeruleumVent(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.CeruleumVent);
class PrototypeLaserAlpha1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.IronKissAlpha1, 6);
class PrototypeLaserAlpha2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.IronKissAlpha2, 6);
class PrototypeLaserBeta(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.IronKissBeta, 5);
class GrandSword(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GrandSword, new AOEShapeCone(25, 45.Degrees()));

class D141ColossusStates : StateMachineBuilder
{
    public D141ColossusStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<CeruleumVent>()
            .ActivateOnEnter<PrototypeLaserAlpha1>()
            .ActivateOnEnter<PrototypeLaserAlpha2>()
            .ActivateOnEnter<PrototypeLaserBeta>()
            .ActivateOnEnter<GrandSword>();
    }
}

[ModuleInfo(CFCID = 16u, NameID = 2134u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class D141Colossus(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(192, 0), 14.5f * CosPI.Pi48th, 48)],
    [new Rectangle(new(207, 0), 1.25f, 20f), new Rectangle(new(177, 0), 1.8f, 20)]);
}
