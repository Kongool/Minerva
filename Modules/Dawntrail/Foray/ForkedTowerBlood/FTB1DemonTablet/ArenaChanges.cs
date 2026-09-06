// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerBlood.FTB1DemonTablet;

sealed class ArenaChanges(ModuleBase module) : ModuleComponent(module)
{
    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x00)
        {
            if (state == 0x00020001u)
            {
                Bounds = FTB1DemonTablet.DefaultArena;
            }
            else if (state == 0x00080004u)
            {
                Bounds = FTB1DemonTablet.CompleteArena;
            }
        }
        else if (index == 0x01 && state == 0x00020001u)
        {
            Bounds = FTB1DemonTablet.RotationArena;
        }
    }
}
