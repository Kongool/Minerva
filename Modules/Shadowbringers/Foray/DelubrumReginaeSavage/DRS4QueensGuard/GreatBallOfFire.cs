// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS4QueensGuard;

sealed class GreatBallOfFire(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<Actor> _smallFlames = module.Enemies((uint)OID.RagingFlame);
    private readonly List<Actor> _bigFlames = module.Enemies((uint)OID.ImmolatingFlame);
    private readonly DateTime _activation = module.World.FutureTime(6.6d);

    private static readonly AOEShapeCircle _shapeSmall = new(10f);
    private static readonly AOEShapeCircle _shapeBig = new(18f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var countS = _smallFlames.Count;
        var countB = _bigFlames.Count;
        var aoes = new AOEInstance[countS + countB];
        var index = 0;
        for (var i = 0; i < countS; ++i)
        {
            var f = _smallFlames[i];
            aoes[index++] = new(_shapeSmall, f.Position, default, Module.CastFinishAt(f.CastInfo, 0, _activation));
        }
        for (var i = 0; i < countB; ++i)
        {
            var f = _bigFlames[i];
            aoes[index++] = new(_shapeBig, f.Position, default, Module.CastFinishAt(f.CastInfo, 0, _activation));
        }
        return aoes;
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.BurnSmall or (uint)AID.BurnBig)
            ++NumCasts;
    }
}
