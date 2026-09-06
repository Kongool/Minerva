// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P8S2;

class EndOfDaysTethered(ModuleBase module) : ModuleComponent(module)
{
    private readonly List<(Actor source, Actor target)> _tethers = []; // enemy -> player

    private static readonly AOEShapeRect _shape = new(60, 5);

    public bool Active => _tethers.Count > 0;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var tetheredCaster = _tethers.FirstOrDefault(t => t.target == actor).source;
        if (tetheredCaster == null)
            return; // non-tethered players shouldn't need to worry about this mechanic

        if (Raid.WithoutSlot(false, true, true).Exclude(actor).InShape(_shape, tetheredCaster).Count != 0)
            hints.Add("Bait away from raid!");
        if (_tethers.Any(t => t.target != actor && _shape.Check(actor.Position, t.source)))
            hints.Add("Move away from other baits!");
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        foreach (var t in _tethers)
            _shape.Draw(Arena, t.source);
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var t in _tethers)
            Arena.AddLine(t.source.Position, t.target.Position, Colors.Danger);
    }

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if ((OID)source.OID == OID.IllusoryHephaistosMovable)
        {
            var target = World.Actors.Find(tether.Target);
            if (target != null)
                _tethers.Add((source, target));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if ((AID)spell.Action.ID == AID.EndOfDaysMovable)
            _tethers.RemoveAll(e => e.source == caster);
    }
}
