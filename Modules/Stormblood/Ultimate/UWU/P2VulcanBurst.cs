// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Ultimate.UWU;

class VulcanBurst(ModuleBase module, uint aid, Actor? source) : Components.GenericKnockback(module, aid)
{
    protected Actor? SourceActor = source;

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor)
    {
        if (SourceActor != null)
            return new Knockback[1] { new(SourceActor.Position, 15f) }; // TODO: activation
        return [];
    }
}

class P2VulcanBurst(ModuleBase module) : VulcanBurst(module, (uint)AID.VulcanBurst, ((UWU)module).Ifrit());
class P4VulcanBurst(ModuleBase module) : VulcanBurst(module, (uint)AID.VulcanBurstUltima, ((UWU)module).Ultima());
