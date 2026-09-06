// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A32Llymlaen;

sealed class TorrentialTridentLanding(ModuleBase module) : Components.CastCounter(module, (uint)AID.TorrentialTridentLanding);

sealed class TorrentialTridentAOE(ModuleBase module) : Components.GenericAOEs(module)
{
    public readonly List<AOEInstance> AOEs = [];
    private static readonly AOEShapeCircle _shape = new(18f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = AOEs.Count;
        if (count == 0)
            return [];
        var max = count > 5 ? 5 : count;
        var aoes = CollectionsMarshal.AsSpan(AOEs);
        if (count > 1)
        {
            ref var aoe0 = ref aoes[0];
            aoe0.Color = Colors.Danger;
        }
        return aoes[..max];
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.TorrentialTridentLanding:
                AOEs.Add(new(_shape, caster.Position.Quantized(), default, World.FutureTime(13.8d)));
                break;
            case (uint)AID.TorrentialTridentAOE:
                ++NumCasts;
                if (AOEs.Count != 0)
                    AOEs.RemoveAt(0);
                break;
        }
    }
}
