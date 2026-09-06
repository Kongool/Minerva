// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P11SThemis;

class UpheldOverruling(ModuleBase module) : Components.UniformStackSpread(module, 6f, 13f, 7, 7)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.UpheldOverrulingLight:
            case (uint)AID.UpheldRulingLight:
                if (World.Actors.Find(caster.Tether.Target) is var stackTarget && stackTarget != null)
                    AddStack(stackTarget, Module.CastFinishAt(spell, 0.3d));
                break;
            case (uint)AID.UpheldOverrulingDark:
            case (uint)AID.UpheldRulingDark:
                if (World.Actors.Find(caster.Tether.Target) is var spreadTarget && spreadTarget != null)
                    AddSpread(spreadTarget, Module.CastFinishAt(spell, 0.3d));
                break;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.UpheldOverrulingAOELight:
            case (uint)AID.UpheldRulingAOELight:
                Stacks.Clear();
                break;
            case (uint)AID.UpheldOverrulingAOEDark:
            case (uint)AID.UpheldRulingAOEDark:
                Spreads.Clear();
                break;
        }
    }
}

abstract class Lightburst(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, 13f);
class LightburstBoss(ModuleBase module) : Lightburst(module, (uint)AID.LightburstBoss);
class LightburstClone(ModuleBase module) : Lightburst(module, (uint)AID.LightburstClone);

abstract class DarkPerimeter(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeDonut(8f, 50f));
class DarkPerimeterBoss(ModuleBase module) : DarkPerimeter(module, (uint)AID.DarkPerimeterBoss);
class DarkPerimeterClone(ModuleBase module) : DarkPerimeter(module, (uint)AID.DarkPerimeterClone);
