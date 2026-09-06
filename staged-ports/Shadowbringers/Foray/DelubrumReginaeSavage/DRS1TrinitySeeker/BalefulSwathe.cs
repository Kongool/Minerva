// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS1TrinitySeeker;

sealed class BalefulSwathe(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.BalefulSwathe)
{
    private readonly DateTime _activation = module.World.FutureTime(7.6d); // from verdant path cast start
    private static readonly AOEShapeRect _shape = new(50f, 50f, -5f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var aoes = new AOEInstance[2];
        aoes[0] = new(_shape, Module.PrimaryActor.Position, Module.PrimaryActor.Rotation + 90f.Degrees(), _activation);
        aoes[1] = new(_shape, Module.PrimaryActor.Position, Module.PrimaryActor.Rotation - 90f.Degrees(), _activation);
        return aoes;
    }
}
