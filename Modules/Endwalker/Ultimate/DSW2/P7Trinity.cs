// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Ultimate.DSW2;

// TODO: improve...
sealed class P7Trinity(ModuleBase module) : Components.GenericBaitAway(module, centerAtTarget: true)
{
    private readonly Actor? _source = module.Enemies((uint)OID.DragonKingThordan).FirstOrDefault();

    private static readonly AOEShapeCircle _shape = new(3);

    public override void Update()
    {
        CurrentBaits.Clear();
        if (_source != null)
        {
            foreach (var target in Raid.WithoutSlot(false, true, true).Where(p => p.Role == Role.Tank))
                CurrentBaits.Add(new(_source, target, _shape));
            var closest = Raid.WithoutSlot(false, true, true).Where(p => p.Role != Role.Tank).Closest(_source.Position);
            if (closest != null)
                CurrentBaits.Add(new(_source, closest, _shape));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.TrinityAOE1 or (uint)AID.TrinityAOE2 or (uint)AID.TrinityAOE3)
        {
            ++NumCasts;
        }
    }
}
