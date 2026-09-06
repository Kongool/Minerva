// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P9SKokytos;

class DualityOfDeath(ModuleBase module) : Components.GenericBaitAway(module, (uint)AID.DualityOfDeathFire, centerAtTarget: true)
{
    private ulong _firstFireTarget;

    private static readonly AOEShapeCircle _shape = new(6);

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (IsBaitTarget(actor))
        {
            if (Raid.WithoutSlot(false, true, true).InRadiusExcluding(actor, _shape.Radius).Any())
                hints.Add("GTFO from raid!");
            if (Module.PrimaryActor.TargetID == _firstFireTarget)
                hints.Add(actor.InstanceID != _firstFireTarget ? "Taunt!" : "Pass aggro!");
        }
        else if (ActiveBaits.Any(b => IsClippedBy(actor, ref b)))
        {
            hints.Add("GTFO from tanks!");
        }
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.DualityOfDeath)
        {
            CurrentBaits.Add(new(Module.PrimaryActor, actor, _shape));
            _firstFireTarget = Module.PrimaryActor.TargetID;
        }
    }
}
