// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Extreme.Ex6Byakko;

class AratamaPuddleBait(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.AratamaPuddle, (uint)AID.AratamaPuddle, 4f, 5.1f)
{
    private DateTime _nextSpread;

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == SpreadAction && World.CurrentTime > _nextSpread)
        {
            if (++NumFinishedSpreads >= 3)
                Spreads.Clear();
            else
                _nextSpread = World.FutureTime(0.5f); // protection in case one target dies
        }
    }
}

class AratamaPuddleVoidzone(ModuleBase module) : Components.Voidzone(module, 4f, GetVoidzones)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.AratamaPuddle);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (z.EventState != 7)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }
}
