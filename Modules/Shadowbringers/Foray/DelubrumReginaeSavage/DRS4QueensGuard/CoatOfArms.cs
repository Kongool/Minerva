// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS4QueensGuard;

sealed class CoatOfArms(ModuleBase module) : Components.DirectionalParry(module, [(uint)OID.AetherialWard])
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        var sides = spell.Action.ID switch
        {
            (uint)AID.CoatOfArmsFB => Side.Front | Side.Back,
            (uint)AID.CoatOfArmsLR => Side.Left | Side.Right,
            _ => Side.None
        };
        if (sides != Side.None)
            PredictParrySide(caster.InstanceID, sides);
    }
}
