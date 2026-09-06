// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Trial.T05Yojimbo;

class DragonsLair(ModuleBase module) : Components.Exaflare(module, _rect)
{
    private static readonly AOEShapeRect _rect = new(3f, 3f);

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.DragonsHead)
        {
            Lines.Add(new(actor.Position, 3f * actor.Rotation.ToDirection(), World.FutureTime(5.7d), 0.6d, 20, 20, actor.Rotation));
        }
    }

    public override void Update()
    {
        var now = World.CurrentTime;
        foreach (var line in Lines)
        {
            if (line.ExplosionsLeft > 0 && now >= line.NextExplosion)
                AdvanceLine(line, line.Next);
        }
        base.Update();
    }
}
