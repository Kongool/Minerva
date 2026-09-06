// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Hunt.RankA.LunatenderQueen;

public enum OID : uint
{
    Boss = 0x35DF // R5.320, x1
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target

    AvertYourEyes = 27363, // Boss->self, 7.0s cast, range 40 circle
    YouMayApproach = 27364, // Boss->self, 7.0s cast, range 6-40 donut
    AwayWithYou = 27365, // Boss->self, 7.0s cast, range 15 circle
    Needles = 27366, // Boss->self, 3.0s cast, range 6 circle
    WickedWhim = 27367, // Boss->self, 4.0s cast, single-target
    AvertYourEyesInverted = 27369, // Boss->self, 7.0s cast, range 40 circle
    YouMayApproachInverted = 27370, // Boss->self, 7.0s cast, range 15 circle
    AwayWithYouInverted = 27371 // Boss->self, 7.0s cast, range 6-40 donut
}

class AvertYourEyes(ModuleBase module) : Components.CastGaze(module, (uint)AID.AvertYourEyes);
class YouMayApproach(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.YouMayApproach, new AOEShapeDonut(6, 40));
class AwayWithYou(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AwayWithYou, 15);
class Needles(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Needles, 6);
class WickedWhim(ModuleBase module) : Components.CastHint(module, (uint)AID.WickedWhim, "Invert next cast");
class AvertYourEyesInverted(ModuleBase module) : Components.CastGaze(module, (uint)AID.AvertYourEyesInverted, true);
class YouMayApproachInverted(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.YouMayApproachInverted, 15);
class AwayWithYouInverted(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AwayWithYouInverted, new AOEShapeDonut(6, 40));

class LunatenderQueenStates : StateMachineBuilder
{
    public LunatenderQueenStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<AvertYourEyes>()
            .ActivateOnEnter<YouMayApproach>()
            .ActivateOnEnter<AwayWithYou>()
            .ActivateOnEnter<Needles>()
            .ActivateOnEnter<WickedWhim>()
            .ActivateOnEnter<AvertYourEyesInverted>()
            .ActivateOnEnter<YouMayApproachInverted>()
            .ActivateOnEnter<AwayWithYouInverted>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 10629u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class LunatenderQueen(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
