// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Hunt.RankS.Okina;

public enum OID : uint
{
    Boss = 0x1AB1, // R=8.0
}

public enum AID : uint
{
    AutoAttack = 872, // 1AB1->player, no cast, single-target
    Hydrocannon = 7990, // 1AB1->self, no cast, range 22+R width 6 rect
    ElectricSwipe = 7991, // 1AB1->self, 2.5s cast, range 17+R 60-degree cone, applies paralysis
    BodySlam = 7993, // 1AB1->location, 4.0s cast, range 10 circle, knockback 20, away from source
    ElectricWhorl = 7996, // 1AB1->self, 4.0s cast, range 8-60 donut
    Expulsion = 7995, // 1AB1->self, 3.0s cast, range 6+R circle, knockback 30, away from source
    Immersion = 7994, // 1AB1->self, 3.0s cast, range 60+R circle
    RubyTide = 7992, // Boss->self, 2.0s cast, single-target, boss gives itself Dmg up buff
}

class Hydrocannon(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Hydrocannon, new AOEShapeRect(30, 3));
class ElectricWhorl(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ElectricWhorl, new AOEShapeDonut(8, 60));
class Expulsion(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Expulsion, 14);
class ExpulsionKB(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.Expulsion, 30, shape: new AOEShapeCircle(14));
class ElectricSwipe(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ElectricSwipe, new AOEShapeCone(25, 30.Degrees()));
class BodySlam(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BodySlam, 10);
class BodySlamKB(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.BodySlam, 20, shape: new AOEShapeCircle(10));
class Immersion(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Immersion);
class RubyTide(ModuleBase module) : Components.CastHint(module, (uint)AID.RubyTide, "Applies damage buff to self");

class OkinaStates : StateMachineBuilder
{
    public OkinaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Hydrocannon>()
            .ActivateOnEnter<ElectricSwipe>()
            .ActivateOnEnter<ElectricWhorl>()
            .ActivateOnEnter<Expulsion>()
            .ActivateOnEnter<ExpulsionKB>()
            .ActivateOnEnter<Immersion>()
            .ActivateOnEnter<BodySlam>()
            .ActivateOnEnter<BodySlamKB>()
            .ActivateOnEnter<RubyTide>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 5984u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class Okina(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
