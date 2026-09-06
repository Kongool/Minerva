// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex5Rubicante;

class ArchInferno(ModuleBase module) : Components.VoidzoneAtCastTarget(module, 5f, (uint)AID.ArchInferno, GetVoidzone, 0f)
{
    private static Actor[] GetVoidzone(ModuleBase module)
    {
        var primary = module.PrimaryActor;
        if (module.PrimaryActor.CastInfo?.IsSpell(AID.ArchInferno) ?? false)
            return [primary];
        return [];
    }
}
class InfernoDevilFirst(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.InfernoDevilFirst, 10f);
class InfernoDevilRest(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.InfernoDevilRest, 10f);
class Conflagration(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Conflagration, new AOEShapeRect(40f, 5f));
class RadialFlagration(ModuleBase module) : Components.SimpleProtean(module, (uint)AID.RadialFlagrationAOE, new AOEShapeCone(21f, 15f.Degrees())); // TODO: verify angle
class SpikeOfFlame(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.SpikeOfFlame, 5f);
class FourfoldFlame(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.FourfoldFlame, 6f, 4, 4);
class TwinfoldFlame(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.TwinfoldFlame, 4f, 2, 2);
