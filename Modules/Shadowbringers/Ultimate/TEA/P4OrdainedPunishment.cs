// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Ultimate.TEA;

[SkipLocalsInit]
sealed class P4OrdainedCapitalPunishment(ModuleBase module) : Components.GenericSharedTankbuster(module, (uint)AID.OrdainedCapitalPunishmentAOE, 4f)
{
    public override void Update()
    {
        Target = Source != null ? World.Actors.Find(Source.TargetID) : null;
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.OrdainedCapitalPunishment)
        {
            Source = caster;
            Activation = Module.CastFinishAt(spell, 3.1d);
        }
    }
}

// TODO: dedicated tankbuster component with tankswap hint
[SkipLocalsInit]
sealed class P4OrdainedPunishment(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.OrdainedPunishment, 5f, tankbuster: true);
