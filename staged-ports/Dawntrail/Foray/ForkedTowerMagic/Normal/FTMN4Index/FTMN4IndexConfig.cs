// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerMagic.Normal.FTMN4Index;

[ConfigDisplay(Order = 0x174, Parent = typeof(DawntrailConfig))]
public sealed class IndexConfig : ConfigNode
{
    [PropertyDisplay("Force AI to target closest add when spawned")]
    public bool ForceAddTargeting = false;

    [PropertyDisplay("Force AI to target boss if no adds and no current target")]
    public bool ForceBossTargeting = false;
}
