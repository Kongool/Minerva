// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Ultimate.FRU;

abstract class P2Banish(ModuleBase module) : Components.UniformStackSpread(module, 5f, 5f, 2, 2)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.BanishStack:
                // TODO: this can target either supports or dd
                AddStacks(Raid.WithoutSlot(true, true, true).Where(p => p.Class.IsSupport()), Module.CastFinishAt(spell, 0.1d));
                break;
            case (uint)AID.BanishSpread:
                AddSpreads(Raid.WithoutSlot(true, true, true), Module.CastFinishAt(spell, 0.1d));
                break;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.BanishStackAOE:
                Stacks.Clear();
                break;
            case (uint)AID.BanishSpreadAOE:
                Spreads.Clear();
                break;
        }
    }
}
