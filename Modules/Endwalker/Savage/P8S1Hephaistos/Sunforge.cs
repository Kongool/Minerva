// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P8S1Hephaistos;

sealed class SunforgeCenterHint(ModuleBase module) : Components.CastHint(module, (uint)AID.SunforgeCenter, "Avoid center")
{
    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        if (Active)
        {
            Arena.ZoneRect(Center, new WDir(1, 0), 21, -7, 21, Colors.SafeFromAOE);
            Arena.ZoneRect(Center, new WDir(-1, 0), 21, -7, 21, Colors.SafeFromAOE);
        }
    }
}

sealed class SunforgeSidesHint(ModuleBase module) : Components.CastHint(module, (uint)AID.SunforgeSides, "Avoid sides")
{
    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        if (Active)
        {
            Arena.ZoneRect(Center, new WDir(0, 1), 21, 21, 7, Colors.SafeFromAOE);
        }
    }
}

sealed class SunforgeCenter(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ScorchingFang, new AOEShapeRect(42f, 14f));
sealed class SunforgeSides(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SunsPinion, new AOEShapeRect(14f, 21f));
sealed class SunforgeCenterIntermission(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ScorchingFangIntermission, new AOEShapeRect(42f, 7f));
sealed class SunforgeSidesIntermission(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ScorchedPinion, new AOEShapeRect(14f, 42f));
