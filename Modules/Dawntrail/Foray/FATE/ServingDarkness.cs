// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.FATE.ServingDarkness;

public enum OID : uint
{
    Boss = 0x4772, // R3.5
    Helper = 0x4773
}

public enum AID : uint
{
    AutoAttack = 42901, // Boss->player, no cast, single-target
    Teleport1 = 42196, // Boss->location, no cast, single-target
    Teleport2 = 42186, // Boss->location, no cast, single-target

    Menace = 42175, // Boss->self, 6.0s cast, range 20 circle
    DismalRoarVisual = 42184, // Boss->self, 5.0s cast, single-target
    DismalRoar = 42185, // Helper->self, 5.0s cast, range 60 circle
    SoulSweepTarget = 42192, // Boss->player, no cast, single-target
    SoulSweep = 42177, // Boss->self, 6.0s cast, range 60 130-degree cone

    SweepingCharge = 42178, // Boss->location, 8.0s cast, width 8 rect charge
    SweepingChargeCone = 42181, // Boss->self, 2.0s cast, range 60 130-degree cone
    MenacingCharge = 42179, // Boss->location, 8.0s cast, width 8 rect charge
    MenaceCharge = 42180, // Boss->self, 2.0s cast, range 20 circle

    HallOfSorrowVisual = 42182, // Boss->self, 3.0s cast, single-target
    HallOfSorrow = 42183, // Helper->location, 4.0s cast, range 10 circle
}

sealed class DismalRoar(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.DismalRoar);
sealed class Menace(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Menace, 20f);
sealed class SoulSweep(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.SoulSweep, (uint)AID.SweepingChargeCone], new AOEShapeCone(60f, 65f.Degrees()));
sealed class SweepingMenacingCharge(ModuleBase module) : Components.SimpleChargeAOEGroups(module, [(uint)AID.SweepingCharge, (uint)AID.MenacingCharge], 4f)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        if (Module.PrimaryActor.CastInfo is var spell && spell != null && spell.Action.ID == (uint)AID.SweepingCharge)
        {
            hints.GoalZones.Add(AIHints.GoalSingleTarget(spell.LocXZ, 5f, 5f)); // follow the charge
        }
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (Module.PrimaryActor.CastInfo is var spell && spell != null && spell.Action.ID == (uint)AID.SweepingCharge)
        {
            hints.Add("Follow the charge!");
        }
    }
}

sealed class HallOfSorrow(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HallOfSorrow, 10f);

sealed class MenaceCharge(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];
    private static readonly AOEShapeCircle circle = new(20f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.MenacingCharge)
        {
            _aoe = [new(circle, spell.LocXZ, default, Module.CastFinishAt(spell, 5.2d))];
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.MenaceCharge)
        {
            _aoe = [];
        }
    }
}

sealed class ServingDarknessStates : StateMachineBuilder
{
    public ServingDarknessStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<DismalRoar>()
            .ActivateOnEnter<Menace>()
            .ActivateOnEnter<SoulSweep>()
            .ActivateOnEnter<SweepingMenacingCharge>()
            .ActivateOnEnter<HallOfSorrow>()
            .ActivateOnEnter<MenaceCharge>();
    }
}

[ModuleInfo(Group = ModuleGroup.ForayFATE, CFCID = 1018u, NameID = 1972u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class ServingDarkness(WorldState ws, Actor primary) : OpenWorldFate(ws, primary);
