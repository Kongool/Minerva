// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M07NBruteAbombinator;

sealed class QuarrySwamp(ModuleBase module) : Components.CastLineOfSightAOE(module, (uint)AID.QuarrySwamp, 60f)
{
    public override ReadOnlySpan<Actor> BlockerActors() => CollectionsMarshal.AsSpan(Module.Enemies((uint)OID.BloomingAbomination));

    public override void Update()
    {
        if (Casters.Count != 0 && BlockerActors().Length != 0)
        {
            Safezones.Clear();
            Refresh();
            AddSafezone(Module.CastFinishAt(Casters[0].CastInfo));
        }
    }
}
