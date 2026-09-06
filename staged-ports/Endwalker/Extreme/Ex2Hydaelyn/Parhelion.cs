// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex2Hydaelyn;

class Parhelion(ModuleBase module) : ModuleComponent(module)
{
    private readonly List<Actor> _completedParhelions = [];
    private bool _subparhelions;

    private static readonly AOEShapeRect _beacon = new(45, 3);

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (ActiveParhelions().Any(p => _beacon.Check(actor.Position, p)))
            hints.Add("GTFO from aoe!");
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        foreach (var p in ActiveParhelions())
            _beacon.Draw(Arena, p);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch ((AID)spell.Action.ID)
        {
            case AID.BeaconParhelion:
                _completedParhelions.Add(caster);
                _subparhelions = _completedParhelions.Count >= 15;
                break;
            case AID.BeaconSubparhelion:
                _completedParhelions.Remove(caster);
                break;
        }
    }

    private IEnumerable<Actor> ActiveParhelions()
    {
        if (_subparhelions)
            return _completedParhelions.Take(10);
        else
            return Module.Enemies((uint)OID.Parhelion).Where(p => p.CastInfo != null);
    }
}
