// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Hunt.RankA.Baal;

public enum OID : uint
{
    Boss = 0x2854 // R=3.2
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target

    SewerWater1 = 17956, // Boss->self, 3.0s cast, range 12 180-degree cone
    SewerWater2 = 17957, // Boss->self, 3.0s cast, range 12 180-degree cone
    SewageWaveFirst1 = 17423, // Boss->self, 5.0s cast, range 30 180-degree cone
    SewageWaveFirst2 = 17424, // Boss->self, 5.0s cast, range 30 180-degree cone
    SewageWaveSecond1 = 17422, // Boss->self, no cast, range 30 180-degree cone
    SewageWaveSecond2 = 17421 // Boss->self, no cast, range 30 180-degree cone
}

abstract class SewerWater(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(12f, 90f.Degrees()));
class SewerWater1(ModuleBase module) : SewerWater(module, (uint)AID.SewerWater1);
class SewerWater2(ModuleBase module) : SewerWater(module, (uint)AID.SewerWater2);

class SewageWave(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCone cone = new(30f, 90f.Degrees());
    private readonly List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
            return [];
        var aoes = new AOEInstance[count];
        for (var i = 0; i < count; ++i)
        {
            var aoe = _aoes[i];
            if (i == 0)
                aoes[i] = count > 1 ? aoe with { Color = Colors.Danger } : aoe;
            else if (i == 1)
                aoes[i] = aoe with { Risky = false };
        }
        return aoes;
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.SewageWaveFirst1 or (uint)AID.SewageWaveFirst2)
        {
            _aoes.Add(new(cone, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell)));
            _aoes.Add(new(cone, spell.LocXZ, spell.Rotation + 180f.Degrees(), Module.CastFinishAt(spell, 2.3f)));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (_aoes.Count != 0 && spell.Action.ID is (uint)AID.SewageWaveFirst1 or (uint)AID.SewageWaveFirst2)
            _aoes.RemoveAt(0);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (_aoes.Count != 0 && spell.Action.ID is (uint)AID.SewageWaveSecond1 or (uint)AID.SewageWaveSecond2)
            _aoes.RemoveAt(0);
    }
}

class BaalStates : StateMachineBuilder
{
    public BaalStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<SewageWave>()
            .ActivateOnEnter<SewerWater1>()
            .ActivateOnEnter<SewerWater2>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 8897u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class Baal(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
