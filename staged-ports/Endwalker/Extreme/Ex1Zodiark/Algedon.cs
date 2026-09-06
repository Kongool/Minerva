// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex1Zodiark;

// state related to algedon mechanic
class Algedon(ModuleBase module) : ModuleComponent(module)
{
    private Actor? _caster;

    private static readonly AOEShapeRect _shape = new(60, 15);

    public bool Done => _caster == null;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (_shape.Check(actor.Position, _caster))
            hints.Add("GTFO from diagonal aoe!");
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        _shape.Draw(Arena, _caster);
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID == AID.AlgedonAOE)
            _caster = caster;
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID == AID.AlgedonAOE)
            _caster = null;
    }
}
