// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Ultimate.UWU;

class P2SearingWind(ModuleBase module) : Components.UniformStackSpread(module, default, 14f, includeDeadTargets: true)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.InfernoHowl && World.Actors.Find(spell.TargetID) is var target && target != null)
        {
            AddSpread(target, World.FutureTime(8d));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.SearingWind)
        {
            var index = Enumerable.Range(0, Spreads.Count).MinBy(i => (spell.TargetXZ - Spreads[i].Target.Position).LengthSq());
            if (index < Spreads.Count)
            {
                ref var spread = ref Spreads.Ref(index);
                var status = spread.Target.FindStatus((uint)SID.SearingWind);
                if (status == null || (status.Value.ExpireAt - World.CurrentTime).TotalSeconds < 6d)
                {
                    Spreads.RemoveAt(index);
                }
                else
                {
                    spread.Activation = World.FutureTime(6d);
                }
            }
        }
    }
}
