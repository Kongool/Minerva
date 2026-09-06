// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P10SPandaemonium;

class PartedPlumes : Components.SimpleAOEs
{
    public PartedPlumes(ModuleBase module) : base(module, (uint)AID.PartedPlumes, new AOEShapeCone(50f, 10f.Degrees()), 16) { MaxDangerColor = 2; }
}

class PartedPlumesVoidzone(ModuleBase module) : Components.GenericAOEs(module, default, "GTFO from voidzone!")
{
    private readonly AOEShapeCircle _shape = new(8f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        return new AOEInstance[1] { new(_shape, new WPos(100f, 100f)) };
    }
}
