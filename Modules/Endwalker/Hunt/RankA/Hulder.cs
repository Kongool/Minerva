// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Hunt.RankA.Hulder;

public enum OID : uint
{
    Boss = 0x35DD // R5.400, x1
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target

    LayOfMislaidMemory = 27073, // Boss->self, 5.0s cast, range 30 120-degree cone, dmg + vulnerability up + makes player dance for 15s
    TempestuousWrath = 27075, // Boss->location, 3.0s cast, width 8 rect charge
    RottingElegy = 27076, // Boss->self, 5.0s cast, range 5-50 donut
    OdeToLostLove = 27077, // Boss->self, 5.0s cast, range 60 circle
    StormOfColor = 27078 // Boss->player, 4.0s cast, single-target
}

class LayOfMislaidMemory(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LayOfMislaidMemory, new AOEShapeCone(30, 60.Degrees()));
class TempestuousWrath(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.TempestuousWrath, 4);
class RottingElegy(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RottingElegy, new AOEShapeDonut(5, 50));
class OdeToLostLove(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.OdeToLostLove);
class StormOfColor(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.StormOfColor);

class HulderStates : StateMachineBuilder
{
    public HulderStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<LayOfMislaidMemory>()
            .ActivateOnEnter<TempestuousWrath>()
            .ActivateOnEnter<RottingElegy>()
            .ActivateOnEnter<OdeToLostLove>()
            .ActivateOnEnter<StormOfColor>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 10624u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Hulder(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
