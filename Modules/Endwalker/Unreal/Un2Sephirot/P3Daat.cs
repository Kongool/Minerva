// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Unreal.Un2Sephirot;

class P3Daat(ModuleBase module) : Components.CastCounter(module, (uint)AID.DaatRandom)
{
    private const float radius = 5;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (Raid.WithoutSlot(false, true, true).InRadiusExcluding(actor, radius).Any())
            hints.Add("Spread!");
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        Arena.ZoneCircleOutline(pc.Position, radius, Colors.Danger);
    }
}
