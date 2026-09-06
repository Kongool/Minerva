// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P12S2PallasAthena;

class CrushHelm(ModuleBase module) : ModuleComponent(module)
{
    public int NumSmallHits { get; private set; }
    public int NumLargeHits { get; private set; }
    private DateTime _lastSmallHit;

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch ((AID)spell.Action.ID)
        {
            case AID.CrushHelmAOEFirst:
                if (World.CurrentTime > _lastSmallHit.AddSeconds(0.2f))
                {
                    ++NumSmallHits;
                    _lastSmallHit = World.CurrentTime;
                }
                break;
            case AID.CrushHelmAOERest:
                ++NumLargeHits;
                break;
        }
    }
}
