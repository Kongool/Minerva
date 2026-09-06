// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerBlood.FTB4Magitaur;

sealed class AssassinsDagger(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
            return [];

        var max = count > 6 ? 6 : count;
        var aoes = CollectionsMarshal.AsSpan(_aoes);
        if (count > 3)
        {
            var color = Colors.Danger;
            for (var i = 0; i < 3; ++i)
            {
                aoes[i].Color = color;
            }
        }
        return aoes[..max];
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.AssassinsDaggerFirst)
        {
            var offset = -50f.Degrees();
            var dir = spell.LocXZ - caster.Position;
            var rect = new AOEShapeRect(dir.Length(), 3f);
            var angle = Angle.FromDirection(dir);
            var pos = caster.Position.Quantized();
            var act = Module.CastFinishAt(spell);

            for (var i = 0; i < 6; ++i)
            {
                _aoes.Add(new(rect, pos, angle + i * offset, act.AddSeconds(i * 3.9d)));
            }
            if (_aoes.Count == 18)
            {
                SortHelpers.SortAOEByActivation(_aoes);
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.AssassinsDaggerFirst or (uint)AID.AssassinsDaggerRepeat or (uint)AID.AssassinsDaggerLast)
        {
            if (++NumCasts % 6 == 0)
            {
                _aoes.RemoveRange(0, 3);
            }
        }
    }
}
