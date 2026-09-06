// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex1Zodiark;

// state related to adikia mechanic
class Adikia(ModuleBase module) : ModuleComponent(module)
{
    private readonly List<Actor> _casters = [];

    private static readonly AOEShapeCircle _shape = new(21);

    public bool Done => _casters.Count == 0;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (_casters.Any(c => _shape.Check(actor.Position, c)))
            hints.Add("GTFO from side smash aoe!");
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        foreach (var c in _casters)
            _shape.Draw(Arena, c);
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID is AID.AdikiaL or AID.AdikiaR)
            _casters.Add(caster);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID is AID.AdikiaL or AID.AdikiaR)
            _casters.Remove(caster);
    }
}
