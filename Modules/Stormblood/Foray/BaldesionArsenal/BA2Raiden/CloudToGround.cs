// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Foray.BaldesionArsenal.BA2Raiden;

sealed class CloudToGround(ModuleBase module) : Components.Exaflare(module, 6f)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.CloudToGroundFirst)
        {
            var explosions = spell.LocXZ.InRect(Center, spell.Rotation, 35f, 35f, 15f) ? 8 : spell.LocXZ.InRect(Center, spell.Rotation, 35f, 35f, 20f) ? 6 : 4;
            Lines.Add(new(caster.Position, 8f * spell.Rotation.ToDirection(), Module.CastFinishAt(spell), 1.1d, explosions, 10));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.CloudToGroundFirst or (uint)AID.CloudToGroundRest)
        {
            var count = Lines.Count;
            var pos = caster.Position;
            for (var i = 0; i < count; ++i)
            {
                var line = Lines[i];
                if (line.Next.AlmostEqual(pos, 1f))
                {
                    AdvanceLine(line, pos);
                    if (line.ExplosionsLeft == 0)
                        Lines.RemoveAt(i);
                    return;
                }
            }
        }
    }
}
