// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS1TrinitySeeker;

sealed class ActOfMercy(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.ActOfMercy)
{
    private readonly DateTime _activation = module.World.FutureTime(7.6d); // from verdant path cast start
    private static readonly AOEShapeCross _shape = new(50f, 4f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        return new AOEInstance[1] { new(_shape, Module.PrimaryActor.Position, Module.PrimaryActor.Rotation, _activation) };
    }
}
