// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P11SThemis;

class JuryOverrulingProtean(ModuleBase module) : Components.BaitAwayEveryone(module, module.PrimaryActor, new AOEShapeRect(50f, 4f))
{
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.JuryOverrulingProteanLight or (uint)AID.JuryOverrulingProteanDark)
            ++NumCasts;
    }
}

class IllusoryGlare(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.IllusoryGlare, 5f);
class IllusoryGloom(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.IllusoryGloom, new AOEShapeDonut(2f, 9f));
