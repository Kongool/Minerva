// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS7StygimolochLord;

sealed class ViciousSwipe(ModuleBase module) : Components.GenericKnockback(module, (uint)AID.ViciousSwipe)
{
    private readonly Knockback[] _kb = [new(module.PrimaryActor.Position, 15f, module.World.FutureTime(module.StateMachine.ActiveState?.Duration ?? default), _shape)];

    private static readonly AOEShapeCircle _shape = new(8f);

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor) => _kb;

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        Arena.ZoneCircleOutline(Module.PrimaryActor.Position, _shape.Radius);
    }
}
