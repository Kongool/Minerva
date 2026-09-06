// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRN4Phantom;

sealed class ArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnActorEAnim(Actor actor, uint state)
    {
        if (actor.OID == (uint)OID.ArenaFeatures)
        {
            if (state == 0x00010002u)
            {
                var shape = Phantom.GetArenaChangeAOE();
                var center = Center;
                _aoe = [new(shape, center, default, World.FutureTime(4d), shapeDistance: shape.Distance(center, default))];
            }
            else if (state == 0x00080010u)
            {
                _aoe = [];
                Bounds = new ArenaBoundsRect(23.5f, 24f);
                Center = new(202f, -370f);
            }
        }
    }
}
