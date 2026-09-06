// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex6Golbez;

// TODO: improve/generalize
class EventideFallTriad(ModuleBase module) : ModuleComponent(module)
{
    public enum Mechanic { None, Parties, Roles }

    private Mechanic _curMechanic;

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (_curMechanic != Mechanic.None)
            hints.Add($"Stack by: {_curMechanic}");
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        var mechanic = (AID)spell.Action.ID switch
        {
            AID.EventideFall => Mechanic.Parties,
            AID.EventideTriad => Mechanic.Roles,
            _ => Mechanic.None
        };
        if (mechanic != Mechanic.None)
            _curMechanic = mechanic;
    }
}
