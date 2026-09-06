// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Hunt.RankA.FanAil;

public enum OID : uint
{
    Boss = 0x35C1 // R5.040, x1
}

public enum AID : uint
{
    AutoAttack = 27381, // Boss->player, no cast, single-target

    Divebomb = 27373, // Boss->players, 5.0s cast, range 30 width 11 rect
    DivebombDisappear = 27374, // Boss->location, no cast, single-target
    DivebombReappear = 27375, // Boss->self, 1.0s cast, single-target
    LiquidHell = 27376, // Boss->location, 3.0s cast, range 6 circle
    Plummet = 27378, // Boss->self, 4.0s cast, range 8 90-degree cone
    DeathSentence = 27379, // Boss->player, 5.0s cast, single-target
    CycloneWing = 27380 // Boss->self, 5.0s cast, range 35 circle
}

class Divebomb(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeRect rect = new(30f, 5.5f);
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (_aoe.Length != 0)
        {
            ref var aoe = ref _aoe[0];
            aoe.Rotation = Module.PrimaryActor.Rotation; // aoe aims at a player, so rotation can change for a moment after cast started
        }
        return _aoe;
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Divebomb)
        {
            _aoe = [new(rect, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell))];
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Divebomb)
        {
            _aoe = [];
        }
    }
}

class LiquidHell(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LiquidHell, 6f);
class Plummet(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Plummet, new AOEShapeCone(8f, 45f.Degrees()));
class DeathSentence(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.DeathSentence);
class CycloneWing(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.CycloneWing);

class FanAilStates : StateMachineBuilder
{
    public FanAilStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Divebomb>()
            .ActivateOnEnter<LiquidHell>()
            .ActivateOnEnter<Plummet>()
            .ActivateOnEnter<DeathSentence>()
            .ActivateOnEnter<CycloneWing>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 10633u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class FanAil(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
