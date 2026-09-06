// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Unreal.Un2Sephirot;

class P3GevurahChesed(ModuleBase module) : Components.CastCounter(module, (uint)AID.LifeForce) // doesn't matter which spell to track
{
    private BitMask _physResistMask;
    private int _physSide; // 0 if not active, -1 if left, +1 if right

    private static readonly AOEShape _shape = new AOEShapeRect(40, 10);

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var side = ForbiddenSide(slot);
        if (side != 0 && _shape.Check(actor.Position, Origin(side), 0.Degrees()))
            hints.Add("GTFO from aoe!");
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        var side = ForbiddenSide(pcSlot);
        if (side != 0)
            _shape.Draw(Arena, Origin(side), 0.Degrees());
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if ((SID)status.ID == SID.ForceAgainstMight)
            _physResistMask.Set(Raid.FindSlot(actor.InstanceID));
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID is AID.GevurahChesed or AID.ChesedGevurah)
            _physSide = (AID)spell.Action.ID == AID.GevurahChesed ? -1 : +1;
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID is AID.GevurahChesed or AID.ChesedGevurah)
            _physSide = 0;
    }

    private int ForbiddenSide(int slot)
    {
        if (_physSide == 0 || _physResistMask.None())
            return 0;
        return _physResistMask[slot] ? -_physSide : +_physSide;
    }

    private WPos Origin(int side) => new(side * 10, -20);
}
