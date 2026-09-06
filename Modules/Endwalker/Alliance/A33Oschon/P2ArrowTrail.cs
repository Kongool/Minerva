// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A33Oschon;

class P2ArrowTrail(ModuleBase module) : Components.Exaflare(module, new AOEShapeRect(10f, 5f))
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.ArrowTrailHint)
        {
            var dir = new WDir(default, 5f);
            Lines.Add(new(caster.Position - dir, dir, Module.CastFinishAt(spell, 0.4d), 0.5d, 8, 3, Angle.AnglesCardinals[1]));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.ArrowTrailAOE)
        {
            ++NumCasts;
            var count = Lines.Count;
            var pos = caster.Position - new WDir(default, 5f);
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

class P2DownhillArrowTrailDownhill(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ArrowTrailDownhill, 6f);
