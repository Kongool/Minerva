// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Dungeon.D07Smileton.D072Frameworker;

public enum OID : uint
{
    Boss = 0x34D1, // R=4.05
    Helper = 0x233C,
    Helper2 = 0x1E8FB8, // R2.000, x3 (spawn during fight), EventObj type
    Helper3 = 0x1EA1A1, // R2.000, x4 (spawn during fight), EventObj type
    SmileySupporter = 0x34D2, // R2.300, x2
    PrintedWorker = 0x34E9, // R4.050, x2
}

public enum AID : uint
{
    AutoAttack = 870, // Boss->player, no cast, single-target
    CircularSaw = 26437, // Boss->self, 5.0s cast, range 40 circle //Raidwide
    LeapForward1 = 26438, // Boss->location, 7.0s cast, range 15 circle
    LeapForward2 = 26439, // PrintedWorker->location, 7.0s cast, range 15 circle
    OmnidimensionalOnslaughtVisual = 26440, // Boss->self, 5.0s cast, single-target
    OmnidimensionalOnslaughtAOE = 26441, // Helper->self, 5.0s cast, range 40 45-degree cone
    PrintWorkers1 = 26443, // SmileySupporter->self, no cast, single-target
    PrintWorkers2 = 28092, // Boss->self, 3.0s cast, single-target
    UnknownAbility = 26442, // Helper->Boss, no cast, single-target
}

public enum SID : uint
{
    UnknownStatus = 2056, // none->Boss/PrintedWorker, extra=0xE1
}

public enum TetherID : uint
{
    Tether23 = 23, // SmileySupporter->Boss
}

class OmnidimensionalOnslaughtAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.OmnidimensionalOnslaughtAOE, new AOEShapeCone(40, 22.5f.Degrees()));

class LeapForward1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LeapForward1, 15);
class LeapForward2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LeapForward2, 15);

//class SteelBeam(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.SteelBeam);
class CircularSaw(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.CircularSaw);

class D072FrameworkerStates : StateMachineBuilder
{
    public D072FrameworkerStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<LeapForward1>()
            .ActivateOnEnter<LeapForward2>()
            .ActivateOnEnter<OmnidimensionalOnslaughtAOE>()
            .ActivateOnEnter<CircularSaw>();
    }
}

[ModuleInfo(CFCID = 794u, NameID = 10333u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class D072Frameworker(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(64, -112), new ArenaBoundsCircle(20));
