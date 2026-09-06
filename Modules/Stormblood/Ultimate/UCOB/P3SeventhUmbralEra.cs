// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Ultimate.UCOB;

class P3SeventhUmbralEra(ModuleBase module) : Components.GenericKnockback(module, (uint)AID.SeventhUmbralEra)
{
    private readonly DateTime _activation = module.World.FutureTime(5.3d);

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor)
    {
        return new Knockback[1] { new(Center, 11f, _activation, ignoreImmunes: true) };
    }
}

class P3CalamitousFlame(ModuleBase module) : Components.CastCounter(module, (uint)AID.CalamitousFlame);
class P3CalamitousBlaze(ModuleBase module) : Components.CastCounter(module, (uint)AID.CalamitousBlaze);
