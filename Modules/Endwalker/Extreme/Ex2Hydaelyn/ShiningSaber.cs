// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex2Hydaelyn;

// state related to shining saber mechanic (shared damage)
class ShiningSaber(ModuleBase module) : Components.UniformStackSpread(module, 6, 0, 8, 8)
{
    public override void Update()
    {
        if (Module.PrimaryActor.CastInfo != null)
        {
            Stacks.Clear();
            if (World.Actors.Find(Module.PrimaryActor.TargetID) is var target && target != null)
                AddStack(target);
        }
        base.Update();
    }
}
