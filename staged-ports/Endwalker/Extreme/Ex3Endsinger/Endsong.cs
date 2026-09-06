// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex3Endsigner;

class Endsong(ModuleBase module) : ModuleComponent(module)
{
    private readonly List<Actor> _active = [];

    private static readonly AOEShapeCircle _aoe = new(15f);

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (_active.Any(a => _aoe.Check(actor.Position, a)))
            hints.Add("GTFO from aoe!");
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        foreach (var a in _active)
            _aoe.Draw(Arena, a);
    }

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID is (uint)TetherID.EndsongFirst or (uint)TetherID.EndsongNext)
            _active.Add(source);
    }

    public override void OnUntethered(Actor source, in ActorTetherInfo tether)
    {
        _active.Remove(source);
    }
}
