// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.Duel.Duel4Dabog;

class RightArmComet(ModuleBase module, uint aid, float distance) : Components.SimpleKnockbacks(module, aid, distance, shape: new AOEShapeCircle(_radius))
{
    private const float _radius = 5f;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        base.AddHints(slot, actor, hints);
        var count = Casters.Count;
        if (count == 0)
        {
            return;
        }
        var casters = CollectionsMarshal.AsSpan(Casters);
        for (var i = 0; i < count; ++i)
        {
            ref readonly var c = ref casters[i];
            if (!Shape!.Check(actor.Position, c.Origin, default))
            {
                hints.Add("Soak the tower!");
                return;
            }
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        base.DrawArenaForeground(pcSlot, pc);
        var count = Casters.Count;
        if (count == 0)
        {
            return;
        }
        var casters = CollectionsMarshal.AsSpan(Casters);
        for (var i = 0; i < count; ++i)
        {
            ref readonly var c = ref casters[i];
            Arena.ZoneCircleOutline(c.Origin, _radius, pc.Position.InCircle(c.Origin, _radius) ? Colors.Safe : default, 2f);
        }
    }
}
sealed class RightArmCometShort(ModuleBase module) : RightArmComet(module, (uint)AID.RightArmCometKnockbackShort, 12f);
sealed class RightArmCometLong(ModuleBase module) : RightArmComet(module, (uint)AID.RightArmCometKnockbackLong, 25f);
