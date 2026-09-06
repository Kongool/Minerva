// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P2SHippokampos;

class OminousBubbling(ModuleBase module) : Components.CastCounter(module, (uint)AID.OminousBubblingAOE)
{
    private const float _radius = 6;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var healersInRange = Raid.WithoutSlot(false, true, true).Where(a => a.Role == Role.Healer).InRadius(actor.Position, _radius).Count();
        if (healersInRange > 1)
            hints.Add("Hit by two aoes!");
        else if (healersInRange == 0)
            hints.Add("Stack with healer!");
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var player in Raid.WithoutSlot(false, true, true))
        {
            if (player.Role == Role.Healer)
            {
                Arena.Actor(player, Colors.Danger);
                Arena.ZoneCircleOutline(player.Position, _radius, Colors.Danger);
            }
            else
            {
                Arena.Actor(player, Colors.PlayerGeneric);
            }
        }
    }
}
