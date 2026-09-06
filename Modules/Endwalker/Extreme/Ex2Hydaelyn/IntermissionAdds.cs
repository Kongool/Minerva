// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex2Hydaelyn;

// component for intermission adds (crystals & echoes)
class IntermissionAdds(ModuleBase module) : ModuleComponent(module)
{
    private readonly HashSet<ulong> _activeCrystals = [];

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var echo in Module.Enemies((uint)OID.Echo))
            Arena.Actor(echo);

        // note that there are two crystals per position, one targetable and one not - untargetable one can be tethered to second echo
        foreach (var crystal in Module.Enemies((uint)OID.CrystalOfLight))
        {
            if (crystal.IsTargetable && !crystal.IsDead)
            {
                var isActive = _activeCrystals.Contains(crystal.InstanceID);
                Arena.Actor(crystal, isActive ? Colors.Danger : Colors.PlayerGeneric);
            }

            var tether = World.Actors.Find(crystal.Tether.Target);
            if (tether != null)
                Arena.AddLine(crystal.Position, tether.Position, Colors.Danger);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if ((AID)spell.Action.ID == AID.IncreaseConviction)
            _activeCrystals.Add(caster.InstanceID);
    }
}
