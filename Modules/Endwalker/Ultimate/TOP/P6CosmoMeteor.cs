// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Ultimate.TOP;

sealed class P6CosmoMeteorPuddles(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CosmoMeteorAOE, 10f);

sealed class P6CosmoMeteorAddComet(ModuleBase module) : Components.Adds(module, (uint)OID.CosmoComet);

sealed class P6CosmoMeteorAddMeteor(ModuleBase module) : Components.Adds(module, (uint)OID.CosmoMeteor);

sealed class P6CosmoMeteorSpread : Components.UniformStackSpread
{

    public P6CosmoMeteorSpread(ModuleBase module) : base(module, default, 5f)
    {
        AddSpreads(Raid.WithoutSlot(true, true, true));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.CosmoMeteorSpread)
            ++NumCasts;
    }
}

sealed class P6CosmoMeteorFlares(ModuleBase module) : Components.UniformStackSpread(module, 6f, 20f, 5) // TODO: verify flare falloff
{
    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.OptimizedMeteor)
        {
            AddSpread(actor, World.FutureTime(8.1d));
            if (Spreads.Count == 3)
            {
                // TODO: how is the stack target selected?
                var stackTarget = Raid.WithoutSlot(false, true, true).FirstOrDefault(p => !IsSpreadTarget(p));
                if (stackTarget != null)
                    AddStack(stackTarget, World.FutureTime(8.1f));
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.CosmoMeteorStack or (uint)AID.CosmoMeteorFlare)
        {
            Spreads.Clear();
            Stacks.Clear();
        }
    }
}
