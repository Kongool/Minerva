// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex2Hydaelyn;

class Spectrum(ModuleBase module) : Components.CastCounter(module, (uint)AID.BrightSpectrum)
{
    private const float _radius = 5;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        int tanksInRange = 0, nonTanksInRange = 0;
        foreach (var other in Raid.WithoutSlot(false, true, true).InRadiusExcluding(actor, _radius))
        {
            if (other.Role == Role.Tank)
                ++tanksInRange;
            else
                ++nonTanksInRange;
        }

        if (nonTanksInRange != 0 || actor.Role != Role.Tank && tanksInRange != 0)
            hints.Add("Spread!");

        if (actor.Role == Role.Tank && tanksInRange == 0)
            hints.Add("Stack with co-tank");
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        Arena.ZoneCircleOutline(pc.Position, _radius, Colors.Danger);
        foreach (var player in Raid.WithoutSlot(false, true, true).Exclude(pc))
            Arena.Actor(player, player.Position.InCircle(pc.Position, _radius) ? Colors.PlayerInteresting : Colors.PlayerGeneric);
    }
}
