// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Trial.T01Valigarmanda;

sealed class StranglingCoilSusurrantBreath(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeDonut donut = new(8f, 30f);
    private static readonly AOEShapeCone cone = new(50f, 40f.Degrees());
    private readonly ArcaneLightning aoe = module.FindComponent<ArcaneLightning>()!;
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => aoe.AOEs.Count == 0 ? _aoe : [];

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        AOEShape? shape = spell.Action.ID switch
        {
            (uint)AID.StranglingCoil => donut,
            (uint)AID.SusurrantBreath => cone,
            _ => null
        };
        if (shape != null)
        {
            _aoe = [new(shape, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell))];
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.StranglingCoil or (uint)AID.SusurrantBreath)
        {
            _aoe = [];
        }
    }
}
