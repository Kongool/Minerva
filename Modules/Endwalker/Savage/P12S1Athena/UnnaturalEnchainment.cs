// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P12S1Athena;

// TODO: consider using envcontrols instead
sealed class UnnaturalEnchainment(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.Sample)
{
    private readonly List<AOEInstance> _aoes = [];

    private static readonly AOEShapeRect _shape = new(5f, 10f, 5f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID == (uint)TetherID.UnnaturalEnchainment)
            _aoes.Add(new(_shape, source.Position.Quantized(), default, World.FutureTime(8.2d)));
    }
}
