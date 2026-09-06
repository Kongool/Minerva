// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex4Barbariccia;

class SavageBarbery(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    public int NumActiveCasts => _aoes.Count;
    private static readonly AOEShapeDonut donut = new(6f, 20f);
    private static readonly AOEShapeRect rect = new(40f, 6f);
    private static readonly AOEShapeCircle circle = new(20f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        AOEShape? shape = spell.Action.ID switch
        {
            (uint)AID.SavageBarberyDonutAOE => donut,
            (uint)AID.SavageBarberyRectAOE => rect,
            (uint)AID.SavageBarberyDonutSword or (uint)AID.SavageBarberyRectSword => circle,
            _ => null
        };
        if (shape != null)
            _aoes.Add(new(shape, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell)));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.SavageBarberyDonutAOE or (uint)AID.SavageBarberyRectAOE or (uint)AID.SavageBarberyDonutSword or (uint)AID.SavageBarberyRectSword)
            _aoes.RemoveAt(0);
    }
}

class HairRaid(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    public int NumActiveCasts => _aoes.Count;
    private static readonly AOEShapeCone cone = new(40f, 60f.Degrees()); // TODO: verify angle
    private static readonly AOEShapeDonut donut = new(6f, 20f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        AOEShape? shape = spell.Action.ID switch
        {
            (uint)AID.HairRaidConeAOE => cone,
            (uint)AID.HairRaidDonutAOE => donut,
            _ => null
        };
        if (shape != null)
            _aoes.Add(new(shape, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell)));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.HairRaidConeAOE or (uint)AID.HairRaidDonutAOE)
            _aoes.RemoveAt(0);
    }
}

class HairSprayDeadlyTwist(ModuleBase module) : Components.CastStackSpread(module, (uint)AID.DeadlyTwist, (uint)AID.HairSpray, 6f, 5, 4);
