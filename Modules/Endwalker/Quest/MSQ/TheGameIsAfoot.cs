// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Quest.MSQ.TheGameIsAfoot;

public enum OID : uint
{

    Boss = 0x4037, // R6.51
    StickyVoidzone = 0x1EB913, // R0.5
    Whirlwind = 0x4038, // R3.0
    LeftWing = 0x404B, // R3.0
    RightWing = 0x404A, // R3.0
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->tank, no cast, single-target
    Teleport = 34885, // Boss->location, no cast, single-target

    WindUnbound = 34883, // Boss->self, 5.0s cast, range 40 circle
    SnatchMorsel = 34884, // Boss->tank, 5.0s cast, single-target
    PeckingFlurry = 34886, // Boss->self, 4.0s cast, range 40 circle
    FallingRock = 34888, // Helper->location, 5.0s cast, range 6 circle
    StickySpitVisual = 34889, // Boss->self, 4.0s cast, single-target
    StickySpit = 34890, // Helper->players, 5.0s cast, range 6 circle
    GlidingSwoop = 34891, // Boss->location, 10.0s cast, width 16 rect charge, no need to draw this - player can't move while active
    Swoop = 35717, // Boss->location, 5.0s cast, width 16 rect charge
    FurlingFlappingVisual = 34892, // Boss->self, 5.0s cast, single-target, spread
    FurlingFlapping = 34893, // Helper->player, 5.0s cast, range 8 circle
    Whirlwind = 34094, // Whirlwind->self, 0.5s cast, single-target
    DeadlySwoop = 35888, // Boss->location, 30.0s cast, width 16 rect charge
    RufflingOfFeathers = 34895, // Boss->self, 3.0s cast, single-target
    FullFledgedTantrum = 35063, // Helper->self, no cast, range 40 circle
    WingDeathVisual = 34896 // Boss->self, no cast, single-target
}

class PeckingFlurry(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.PeckingFlurry);
class WindUnbound(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.WindUnbound);
class SnatchMorsel(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.SnatchMorsel);
class FallingRock(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FallingRock, 6, 8);
class StickySpit(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.StickySpit, 6);
class Swoop(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.Swoop, 8);
class FurlingFlapping(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.FurlingFlapping, 8);
class DeadlySwoop(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.DeadlySwoop, 8)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count == 0)
            return;
        if (Raid.WithoutSlot(true, false).Length == 3) // only targetable allies count towards the party
            base.AddAIHints(slot, actor, assignment, hints);
    }
}

class GiantColibriStates : StateMachineBuilder
{
    public GiantColibriStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<PeckingFlurry>()
            .ActivateOnEnter<WindUnbound>()
            .ActivateOnEnter<SnatchMorsel>()
            .ActivateOnEnter<FallingRock>()
            .ActivateOnEnter<StickySpit>()
            .ActivateOnEnter<Swoop>()
            .ActivateOnEnter<DeadlySwoop>()
            .ActivateOnEnter<FurlingFlapping>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 70288u, NameID = 12499u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class GiantColibri(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    public static readonly ArenaBoundsCustom arena = new([new Polygon(new(425, -440), 14.5f, 48),
    new Rectangle(new(425.27f, -426.5f), 3.8f, 1), new Rectangle(new(424.95f, -453.5f), 4.15f, 1)]);
}
