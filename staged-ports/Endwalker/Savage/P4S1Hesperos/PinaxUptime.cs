// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P4S1Hesperos;

// component showing where to drag boss for max pinax uptime
class PinaxUptime(ModuleBase module) : ModuleComponent(module)
{
    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (pc.Role != Role.Tank)
            return;

        // draw position between lighting and fire squares
        var assignments = Module.FindComponent<SettingTheScene>()!;
        var doubleOffset = assignments.Direction(assignments.Assignment(SettingTheScene.Element.Fire)) + assignments.Direction(assignments.Assignment(SettingTheScene.Element.Lightning));
        if (doubleOffset == default)
            return;

        Arena.ZoneCircleOutline(Center + 9 * doubleOffset, 2, Colors.Safe);
    }
}
