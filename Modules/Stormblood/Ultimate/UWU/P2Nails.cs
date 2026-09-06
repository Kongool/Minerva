// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Ultimate.UWU;

// TODO: kill priorities
class P2Nails(ModuleBase module) : ModuleComponent(module)
{
    private readonly List<Actor> _nails = module.Enemies((uint)OID.InfernalNail);

    public bool Active => _nails.Any(a => a.IsTargetable);

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        Arena.Actors(_nails);
    }
}
