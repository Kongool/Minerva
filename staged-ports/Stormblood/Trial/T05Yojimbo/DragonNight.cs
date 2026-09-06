// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Trial.T05Yojimbo;

class DragonNight(ModuleBase module) : ModuleComponent(module)
{
    private DateTime _hintExpire;

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (id == 0x008F && actor.OID == (uint)OID.DragonsHead)
            _hintExpire = World.FutureTime(5.5d);
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (World.CurrentTime < _hintExpire)
            hints.Add("Raidwide after dragon heads leave arena");
    }
}
