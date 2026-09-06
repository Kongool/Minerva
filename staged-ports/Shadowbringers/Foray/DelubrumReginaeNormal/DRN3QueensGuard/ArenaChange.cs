// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRN3QueensGuard;

sealed class ArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly AOEShapeDonut donut = new(25f, 30f);
    private AOEInstance[] _aoe = [];
    private bool startingArena = true;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void Update()
    {
        if (startingArena && _aoe.Length == 0)
        {
            var features = Module.Enemies((uint)OID.ArenaFeatures);
            var count = features.Count;
            for (var i = 0; i < count; ++i)
            {
                var f = features[i];
                if (f.EventState == default && f.Position.AlmostEqual(new(244f, -129f), 1f))
                {
                    var center = Center;
                    _aoe = [new(donut, center, default, World.FutureTime(5d), shapeDistance: donut.Distance(center, default))];
                    return;
                }
            }
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x18 && state == 0x00020001u)
        {
            var arena = QueensGuard.GetDefaultArena();
            Bounds = arena;
            Center = arena.Center;
            _aoe = [];
            startingArena = false;
        }
    }
}
