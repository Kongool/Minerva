// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P10SPandaemonium;

class Silkspit(ModuleBase module) : Components.UniformStackSpread(module, default, 7f)
{
    private readonly List<Actor> _pillars = module.Enemies((uint)OID.Pillar);

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        base.AddHints(slot, actor, hints);
        if (IsSpreadTarget(actor) && _pillars.InRadius(actor.Position, SpreadRadius).Any())
            hints.Add("GTFO from pillars!");
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        base.DrawArenaBackground(pcSlot, pc);
        Arena.Actors(_pillars, Colors.Object, true);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.SilkspitAOE)
            Spreads.Clear();
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.Silkspit)
            AddSpread(actor, World.FutureTime(8));
    }
}
