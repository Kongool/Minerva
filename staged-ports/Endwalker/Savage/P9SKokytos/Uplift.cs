// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P9SKokytos;

class Uplift(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Uplift, new AOEShapeRect(4, 8));

class ArenaChanges(ModuleBase module) : ModuleComponent(module)
{
    public override void OnMapEffect(byte index, uint state)
    {
        if (state == 0x00080004 && index is 2 or 3)
            Module.Bounds = P9SKokytos.arena;
        if (state == 0x00020001)
        {
            if (index == 2)
                Module.Bounds = P9SKokytos.arenaUplift0;
            if (index == 3)
                Module.Bounds = P9SKokytos.arenaUplift45;
        }
    }
}
