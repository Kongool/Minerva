// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerMagic.Normal.FTMN1TwoHeadedAevis;

[ConfigDisplay(Order = 0x171, Parent = typeof(DawntrailConfig))]
public sealed class TwoHeadedAevisConfig : ConfigNode
{
    [PropertyDisplay("Force AI to target assigned head")]
    public bool ForceTargeting = false;
}
