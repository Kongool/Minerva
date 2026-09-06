// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C02AMR.C022Gorai;

sealed class ImpurePurgationBait(ModuleBase module) : Components.BaitAwayEveryone(module, module.PrimaryActor, new AOEShapeCone(60f, 22.5f.Degrees()))
{
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.NImpurePurgationBait or (uint)AID.SImpurePurgationBait)
        {
            ++NumCasts;
            CurrentBaits.Clear();
        }
    }
}

abstract class ImpurePurgationAOE(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(60f, 22.5f.Degrees()));
sealed class NImpurePurgationAOE(ModuleBase module) : ImpurePurgationAOE(module, (uint)AID.NImpurePurgationAOE);
sealed class SImpurePurgationAOE(ModuleBase module) : ImpurePurgationAOE(module, (uint)AID.SImpurePurgationAOE);
