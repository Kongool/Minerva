// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Ultimate.UCOB;

class Twister(ModuleBase module) : Components.CastTwister(module, 2f, (uint)OID.VoidzoneTwister, (uint)AID.Twister, 0.3f, 0.5f); // TODO: verify radius

class P1Twister(ModuleBase module) : Twister(module)
{
    public override bool KeepOnPhaseChange => true;
}
