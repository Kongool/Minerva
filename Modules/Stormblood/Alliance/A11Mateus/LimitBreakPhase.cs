// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Alliance.A11Mateus;

class BlizzardSphere(ModuleBase module) : Components.StretchTetherSingle(module, (uint)TetherID.KiteTether, 10, needToKite: true)
{
    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (source.OID == (uint)OID.BlizzardSphere) // same tether ID is used by 2 mechanics, so we need to filter
            base.OnTethered(source, tether);
    }

    public override void OnUntethered(Actor source, in ActorTetherInfo tether)
    {
        if (source.OID == (uint)OID.BlizzardSphere)
            base.OnUntethered(source, tether);
    }
}

class FinRays(ModuleBase module) : Components.Cleave(module, (uint)AID.FinRays, new AOEShapeCone(11.8f, 60f.Degrees()), [(uint)OID.AzureGuard]);
