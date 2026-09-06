// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Trial.T02ZoraalJaP2;

sealed class DawnOfAnAgeArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x20)
        {
            switch (state)
            {
                case 0x00020001u:
                    var center = Center;
                    var angle = 45f.Degrees();
                    var shape = new AOEShapeCustom(center, [new Square(center, 20f, angle)], [new Square(center, 10f, angle)]);
                    _aoe = [new(shape, center, default, World.FutureTime(8d), shapeDistance: shape.Distance(center, default))];
                    break;
                case 0x00080004u:
                    _aoe = [];
                    Bounds = new ArenaBoundsSquare(10f, 45f.Degrees());
                    break;
            }
        }
        else if (index == 0x1B && state == 0x00080004u)
        {
            Bounds = T02ZoraalJa.ZoraalJa.GetDefaultBounds();
        }
    }
}
