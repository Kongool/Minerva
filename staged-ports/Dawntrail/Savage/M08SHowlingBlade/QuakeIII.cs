// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M08SHowlingBlade;

sealed class QuakeIII(ModuleBase module) : Components.GenericBaitStack(module, (uint)AID.QuakeIII)
{
    private static readonly AOEShapeCircle circle = new(8f);

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.Heavensearth)
        {
            CurrentBaits.Add(new((WPos)default, actor, circle, World.FutureTime(5.1d)));
        }
    }

    public override void Update()
    {
        var baits = CollectionsMarshal.AsSpan(CurrentBaits);
        var len = baits.Length;
        for (var i = 0; i < len; ++i)
        {
            ref var b = ref baits[i];
            for (var j = 0; j < 5; ++j)
            {
                var center = ArenaChanges.EndArenaPlatforms[j].Center;
                if (b.Target.Position.InCircle(center, 8f))
                {
                    b.Source.PosRot = center.ToVec4();
                    break;
                }
            }
        }
    }
}
