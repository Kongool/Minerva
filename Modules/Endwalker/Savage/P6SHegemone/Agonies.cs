// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P6SHegemone;

class Agonies(ModuleBase module) : Components.UniformStackSpread(module, 6, 15, 3)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch ((AID)spell.Action.ID)
        {
            case AID.AgoniesDarkburst1:
            case AID.AgoniesDarkburst2:
            case AID.AgoniesDarkburst3:
                if (World.Actors.Find(spell.TargetID) is var spreadTarget && spreadTarget != null)
                    AddSpread(spreadTarget);
                break;
            case AID.AgoniesUnholyDarkness1:
            case AID.AgoniesUnholyDarkness2:
            case AID.AgoniesUnholyDarkness3:
                if (World.Actors.Find(spell.TargetID) is var stackTarget && stackTarget != null)
                    AddStack(stackTarget);
                break;
            case AID.AgoniesDarkPerimeter1:
            case AID.AgoniesDarkPerimeter2:
                // don't really care about donuts, they auto resolve...
                break;
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        switch ((AID)spell.Action.ID)
        {
            case AID.AgoniesDarkburst1:
            case AID.AgoniesDarkburst2:
            case AID.AgoniesDarkburst3:
                Spreads.RemoveAll(s => s.Target.InstanceID == spell.TargetID);
                break;
            case AID.AgoniesUnholyDarkness1:
            case AID.AgoniesUnholyDarkness2:
            case AID.AgoniesUnholyDarkness3:
                Stacks.RemoveAll(s => s.Target.InstanceID == spell.TargetID);
                break;
            case AID.AgoniesDarkPerimeter1:
            case AID.AgoniesDarkPerimeter2:
                // don't really care about donuts, they auto resolve...
                break;
        }
    }
}
