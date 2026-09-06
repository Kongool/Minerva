// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M03SBruteBomber;

abstract class Proximity(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, 20);  // TODO: verify falloff
sealed class OctoboomDiveProximity(ModuleBase module) : Proximity(module, (uint)AID.OctoboomDiveProximityAOE);
sealed class QuadroboomDiveProximity(ModuleBase module) : Proximity(module, (uint)AID.QuadroboomDiveProximityAOE);

abstract class DiveKB(ModuleBase module, uint aid) : Components.SimpleKnockbacks(module, aid, 25);
sealed class OctoboomDiveKnockback(ModuleBase module) : DiveKB(module, (uint)AID.OctoboomDiveKnockbackAOE);
sealed class QuadroboomDiveKnockback(ModuleBase module) : DiveKB(module, (uint)AID.QuadroboomDiveKnockbackAOE);

class Diveboom(ModuleBase module) : Components.UniformStackSpread(module, 5f, 5f, 2, 2)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.OctoboomDiveProximityAOE:
            case (uint)AID.OctoboomDiveKnockbackAOE:
                AddSpreads(Raid.WithoutSlot(true, true, true), Module.CastFinishAt(spell));
                break;
            case (uint)AID.QuadroboomDiveProximityAOE:
            case (uint)AID.QuadroboomDiveKnockbackAOE:
                // TODO: can target any role
                AddStacks(Raid.WithoutSlot(true, true, true).Where(p => p.Class.IsSupport()), Module.CastFinishAt(spell));
                break;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.DiveboomSpread or (uint)AID.DiveboomPair)
        {
            Spreads.Clear();
            Stacks.Clear();
        }
    }
}
