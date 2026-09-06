// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Chaotic.Ch01CloudOfDarkness;

sealed class Break(ModuleBase module) : Components.GenericGaze(module)
{
    public readonly List<Eye> Eyes = [];

    public override ReadOnlySpan<Eye> ActiveEyes(int slot, Actor actor) => CollectionsMarshal.AsSpan(Eyes);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.BreakBoss or (uint)AID.BreakEye)
        {
            Eyes.Add(new(spell.LocXZ, Module.CastFinishAt(spell, 0.9d)));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.BreakBossAOE or (uint)AID.BreakEyeAOE)
        {
            var count = Eyes.Count;
            var pos = spell.LocXZ;
            var eyes = CollectionsMarshal.AsSpan(Eyes);
            for (var i = 0; i < count; ++i)
            {
                if (eyes[i].Position == pos)
                {
                    Eyes.RemoveAt(i);
                    return;
                }
            }
        }
    }
}
