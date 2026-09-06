// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P10SPandaemonium;

class Border(ModuleBase module) : ModuleComponent(module)
{
    public bool LBridgeActive;
    public bool RBridgeActive;

    public override void OnMapEffect(byte index, uint state)
    {
        if (state is 0x00020001u or 0x00080004u)
        {
            switch (index)
            {
                case 2: RBridgeActive = state == 0x00020001u; break;
                case 3: LBridgeActive = state == 0x00020001u; break;
            }
        }
        if (!LBridgeActive && !RBridgeActive)
            Bounds = P10SPandaemonium.DefaultArena;
        else if (!LBridgeActive && RBridgeActive)
            Bounds = P10SPandaemonium.ArenaR;
        else if (LBridgeActive && !RBridgeActive)
            Bounds = P10SPandaemonium.ArenaL;
        else if (LBridgeActive && RBridgeActive)
            Bounds = P10SPandaemonium.ArenaLR;
    }
}
