// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C03AAI.C031Ketuduke;

class BlowingBubbles(ModuleBase module) : ModuleComponent(module)
{
    private readonly List<Actor> _actors = [];

    private static readonly AOEShapeCircle _shape = new(5);

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID is (uint)OID.NAiryBubbleExaflare or (uint)OID.SAiryBubbleExaflare)
            _actors.Add(actor);
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        foreach (var a in _actors)
            _shape.Draw(Arena, a);
    }
}
