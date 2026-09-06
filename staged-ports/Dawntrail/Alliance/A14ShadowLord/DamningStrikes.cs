// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A14ShadowLord;

sealed class DamningStrikes(ModuleBase module) : Components.GenericTowers(module)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.DamningStrikesImpact1 or (uint)AID.DamningStrikesImpact2 or (uint)AID.DamningStrikesImpact3)
        {
            Towers.Add(new(spell.LocXZ, 3f, 8, 8, default, Module.CastFinishAt(spell), caster.InstanceID));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.DamningStrikesImpact1 or (uint)AID.DamningStrikesImpact2 or (uint)AID.DamningStrikesImpact3)
        {
            ++NumCasts;

            var count = Towers.Count;
            var id = caster.InstanceID;
            var towers = CollectionsMarshal.AsSpan(Towers);
            for (var i = 0; i < count; ++i)
            {
                ref var t = ref towers[i];
                if (t.ActorID == id)
                {
                    Towers.RemoveAt(i);
                    break;
                }
            }

            var party = Raid.WithSlot(false, false, true);
            var lenP = party.Length;
            BitMask forbidden = default;
            var targets = CollectionsMarshal.AsSpan(spell.Targets);
            var len = targets.Length;
            for (var i = 0; i < len; ++i)
            {
                ref readonly var targ = ref targets[i];
                for (var j = 0; j < lenP; ++j)
                {
                    ref readonly var p = ref party[j];
                    if (targ.ID == p.Item2.InstanceID)
                    {
                        forbidden[p.Item1] = true;
                        break;
                    }
                }
            }

            towers = CollectionsMarshal.AsSpan(Towers);
            var len2 = towers.Length;
            for (var i = 0; i < len2; ++i)
            {
                ref var t = ref towers[i];
                t.ForbiddenSoakers |= forbidden;
            }
        }
    }
}
