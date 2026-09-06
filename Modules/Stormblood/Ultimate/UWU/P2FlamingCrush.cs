// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Ultimate.UWU;

class FlamingCrush(ModuleBase module) : Components.UniformStackSpread(module, 4f, default, 6, 6)
{
    protected BitMask Avoid;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.FlamingCrush)
        {
            AddStack(actor, default, Avoid);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.FlamingCrush)
        {
            Stacks.Clear();
        }
    }
}

// during P2, everyone except searing wind targets (typically two healers) should stack
class P2FlamingCrush : FlamingCrush
{
    public P2FlamingCrush(ModuleBase module) : base(module)
    {
        if (module.FindComponent<P2SearingWind>() is var searingWind && searingWind != null)
            foreach (var sw in searingWind.Spreads)
                Avoid.Set(Raid.FindSlot(sw.Target.InstanceID));
    }
}

// during P4 (annihilation), everyone should stack (except maybe ranged/caster that will handle mesohigh)
class P4FlamingCrush(ModuleBase module) : FlamingCrush(module) { }

// during P5 (suppression), everyone except mesohigh handler (typically tank) should stack
class P5FlamingCrush : FlamingCrush
{
    public P5FlamingCrush(ModuleBase module) : base(module)
    {
        Avoid = Raid.WithSlot(true, true, true).WhereActor(p => p.FindStatus((uint)SID.ThermalLow) != null && p.Role != Role.Healer).Mask();
    }
}
