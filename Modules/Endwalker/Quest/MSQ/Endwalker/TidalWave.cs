// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Quest.MSQ.Endwalker;

// TODO: Make AI function for Destination Unsafe
sealed class TidalWave(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.TidalWaveVisual, 25f, kind: Kind.DirForward, stopAtWall: true)
{
    private readonly Megaflare _megaflare = module.FindComponent<Megaflare>()!;

    public override bool DestinationUnsafe(int slot, Actor actor, WPos pos)
    {
        var aoes = CollectionsMarshal.AsSpan(_megaflare.Casters);
        var len = aoes.Length;
        for (var i = 0; i < len; ++i)
        {
            ref readonly var aoe = ref aoes[i];
            if (aoe.Check(pos))
            {
                return true;
            }
        }
        return false;
    }
}
