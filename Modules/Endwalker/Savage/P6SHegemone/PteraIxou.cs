// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P6SHegemone;

class PteraIxou(ModuleBase module) : Components.CastCounter(module, (uint)AID.PteraIxouAOESnake) // doesn't matter which spell to track
{
    private BitMask _vulnSnake;
    private BitMask _vulnWing;

    private static readonly AOEShapeCone _shape = new(30, 90.Degrees());

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (ForbiddenCenters(slot).Any(dir => _shape.Check(actor.Position, Center, dir)))
            hints.Add("GTFO from aoe!");
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        foreach (var dir in ForbiddenCenters(pcSlot))
            _shape.Draw(Arena, Center, dir);
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        switch ((SID)status.ID)
        {
            case SID.GlossalResistanceDown:
                _vulnSnake.Set(Raid.FindSlot(actor.InstanceID));
                break;
            case SID.ChelicResistanceDown:
                _vulnWing.Set(Raid.FindSlot(actor.InstanceID));
                break;
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        switch ((SID)status.ID)
        {
            case SID.GlossalResistanceDown:
                _vulnSnake.Clear(Raid.FindSlot(actor.InstanceID));
                break;
            case SID.ChelicResistanceDown:
                _vulnWing.Clear(Raid.FindSlot(actor.InstanceID));
                break;
        }
    }

    private IEnumerable<Angle> ForbiddenCenters(int slot)
    {
        if (_vulnSnake[slot])
            yield return 90.Degrees();
        if (_vulnWing[slot])
            yield return -90.Degrees();
    }
}

class PteraIxouSpreadStack(ModuleBase module) : Components.CastStackSpread(module, (uint)AID.PteraIxouUnholyDarkness, (uint)AID.PteraIxouDarkSphere, 6, 10, 3);
