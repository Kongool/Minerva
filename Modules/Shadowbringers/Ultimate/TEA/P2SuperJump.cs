// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Ultimate.TEA;

[SkipLocalsInit]
sealed class P2SuperJump(ModuleBase module) : Components.GenericBaitAway(module, (uint)AID.SuperJumpAOE, centerAtTarget: true)
{
    private static readonly AOEShapeCircle _shape = new(10f);

    public override void Update()
    {
        CurrentBaits.Clear();
        var source = ((TEA)Module).BruteJustice();
        var target = source != null ? Raid.WithoutSlot(false, true, true).Farthest(source.Position) : null;
        if (source != null && target != null)
            CurrentBaits.Add(new(source, target, _shape));
    }
}
