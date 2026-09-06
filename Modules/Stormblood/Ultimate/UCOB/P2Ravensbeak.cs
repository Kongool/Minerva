// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Ultimate.UCOB;

// TODO: generalize to tankswap
class P2Ravensbeak(ModuleBase module) : ModuleComponent(module)
{
    private Actor? _caster;
    private ulong _targetId;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (_caster == null || _caster.TargetID != _targetId)
            return;

        if (actor.InstanceID == _targetId)
            hints.Add("Pass aggro!");
        else if (actor.Role == Role.Tank)
            hints.Add("Taunt!");
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Ravensbeak)
        {
            _caster = caster;
            _targetId = spell.TargetID;
        }
    }
}
