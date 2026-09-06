// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M08SHowlingBlade;

sealed class Twinbite(ModuleBase module) : Components.GenericBaitAway(module, (uint)AID.Twinbite, tankbuster: true)
{
    private static readonly AOEShapeCircle circle = new(8f);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.TwinbiteVisual)
        {
            var party = Raid.WithoutSlot(true, true, true);
            var len = party.Length;
            for (var i = 0; i < len; ++i)
            {
                var p = party[i];
                if (p.Role == Role.Tank)
                {
                    CurrentBaits.Add(new((WPos)default, p, circle, Module.CastFinishAt(spell, 0.1d)));
                }
            }
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
