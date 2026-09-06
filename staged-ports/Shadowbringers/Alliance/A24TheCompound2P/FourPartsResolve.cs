// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A24TheCompound2P;

sealed class FourPartsResolveCircle(ModuleBase module) : Components.GenericBaitAway(module, centerAtTarget: true)
{
    private static readonly AOEShapeCircle circle = new(6f);
    private readonly A24TheCompound2P bossmod = (A24TheCompound2P)module;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID is (uint)IconID.Icon1 or (uint)IconID.Icon3)
        {
            CurrentBaits.Add(new(bossmod.BossP2!, actor, circle, World.FutureTime(iconID == (uint)IconID.Icon1 ? 7.5d : 12.2d)));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (CurrentBaits.Count != 0 && spell.Action.ID == (uint)AID.FourPartsResolveCircle)
        {
            CurrentBaits.RemoveAt(0);
        }
    }

    public override void Update()
    {
        var count = CurrentBaits.Count - 1; // assume bait is cancelled if target dies
        for (var i = count; i >= 0; --i)
        {
            if (CurrentBaits[i].Target.IsDead)
            {
                CurrentBaits.RemoveAt(i);
            }
        }
    }
}

sealed class FourPartsResolveRect(ModuleBase module) : Components.GenericBaitAway(module)
{
    private static readonly AOEShapeRect rect = new(85f, 6f);
    private readonly A24TheCompound2P bossmod = (A24TheCompound2P)module;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID is (uint)IconID.Icon2 or (uint)IconID.Icon4)
        {
            CurrentBaits.Add(new(bossmod.BossP2!, actor, rect, World.FutureTime(iconID == (uint)IconID.Icon2 ? 8.8d : 13.6d)));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (CurrentBaits.Count != 0 && spell.Action.ID == (uint)AID.FourPartsResolveRect)
        {
            CurrentBaits.RemoveAt(0);
        }
    }

    public override void Update()
    {
        var count = CurrentBaits.Count - 1; // assume bait is cancelled if target dies
        for (var i = count; i >= 0; --i)
        {
            if (CurrentBaits[i].Target.IsDead)
            {
                CurrentBaits.RemoveAt(i);
            }
        }
    }
}
