// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Ultimate.DSW1;

// TODO: consider adding invuln hint for tether tank?..
sealed class HolyShieldBash : Components.BaitAwayTethers
{
    public HolyShieldBash(ModuleBase module) : base(module, new AOEShapeRect(80f, 4f), (uint)TetherID.HolyBladedance, (uint)AID.HolyShieldBash)
    {
        BaiterPriority = PlayerPriority.Danger;
        // TODO: consider selecting specific tank rather than any
        ForbiddenPlayers = Raid.WithSlot(true, true, true).WhereActor(a => a.Role != Role.Tank).Mask();
    }
}

// note: this is not really a 'bait', but component works well enough
sealed class HolyBladedance(ModuleBase module) : Components.GenericBaitAway(module, (uint)AID.HolyBladedanceAOE)
{
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.HolyShieldBash && World.Actors.Find(spell.MainTargetID) is var target && target != null)
            CurrentBaits.Add(new(caster, target, new AOEShapeCone(16f, 45f.Degrees())));
    }
}

sealed class Heavensblaze(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.Heavensblaze, 4f, 7, 7)
{
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        // bladedance target shouldn't stack
        if (spell.Action.ID == (uint)AID.HolyShieldBash)
            foreach (ref var s in Stacks.AsSpan())
                s.ForbiddenPlayers.Set(Raid.FindSlot(spell.MainTargetID));
    }
}
