// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Modules.Dawntrail.Extreme.Ex8Enuo;

sealed class DenseAiryEmptiness(ModuleBase module) : Components.GenericBaitStack(module)
{
    //TODO: Cone widths are estimates.
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.DenseEmptiness)
        {
            var party = Raid.WithoutSlot(true, false, false);
            var len = party.Length;
            var delay = 5f;
            for (var i = 0; i < len; i++)
            {
                var p = party[i];
                if (p.Role == Role.Healer)
                {
                    CurrentBaits.Add(new(Module.PrimaryActor, p, new AOEShapeCone(60f, 50f.Degrees()), World.FutureTime(delay)));
                }
            }
        }
        if (spell.Action.ID == (uint)AID.AiryEmptiness)
        {
            var party = Raid.WithoutSlot(true, false, false);
            var len = party.Length;
            var delay = 5f;
            for (var i = 0; i < len; i++)
            {
                var p = party[i];
                if (p.Role is Role.Healer or Role.Tank)
                {
                    CurrentBaits.Add(new(Module.PrimaryActor, p, new AOEShapeCone(60f, 30f.Degrees()), World.FutureTime(delay)));
                }
            }
        }
    }
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is ((uint)AID.AiryEmptiness1) or ((uint)AID.DenseEmptiness1))
        {
            CurrentBaits.Clear();
        }
    }
}
