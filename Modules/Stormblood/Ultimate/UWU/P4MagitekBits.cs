// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Ultimate.UWU;

class P4MagitekBits(ModuleBase module) : ModuleComponent(module)
{
    private readonly List<Actor> _bits = module.Enemies((uint)OID.MagitekBit);

    public bool Active => _bits.Any(b => b.IsTargetable);

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        Arena.Actors(_bits);
    }
}
