// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Dungeon.D03Copperbell.D033Gyges;

public enum OID : uint
{
    Boss = 0x38C9,
    Helper = 0x233C // x5
}

public enum AID : uint
{
    AutoAttack = 6499, // Boss->player, no cast
    GiganticSwing = 28762, // Boss->self, 6.0s cast, range 4-40 donut aoe
    GiganticSmash = 28760, // Boss->location, 6.0s cast, range 10 aoe
    GiganticBlast = 28761, // Helper->self, 6.0s cast, range 8 aoe
    GrandSlam = 28764, // Boss->player, 5.0s cast, tankbuster
    ColossalSlam = 28763 // Boss->self, 4.0s cast, range 40 60-degree cone aoe
}

class GiganticSwing(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GiganticSwing, new AOEShapeDonut(4, 40));
class GiganticSmash(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GiganticSmash, 10);
class GiganticBlast(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GiganticBlast, 8);
class GrandSlam(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.GrandSlam);
class ColossalSlam(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ColossalSlam, new AOEShapeCone(40, 30.Degrees()));

class D033GygesStates : StateMachineBuilder
{
    public D033GygesStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<GiganticSwing>()
            .ActivateOnEnter<GiganticSmash>()
            .ActivateOnEnter<GiganticBlast>()
            .ActivateOnEnter<GrandSlam>()
            .ActivateOnEnter<ColossalSlam>();
    }
}

[ModuleInfo(CFCID = 3u, NameID = 101u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class D033Gyges(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-100, 6), new ArenaBoundsCircle(19.5f));
