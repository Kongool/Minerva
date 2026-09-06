// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A13Azeyma;

sealed class WildfireWard(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.IlluminatingGlimpse, 15f, false, 1, kind: Kind.DirLeft)
{
    private RelSimplifiedComplexPolygon poly;
    private bool polyInit;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
        {
            ref readonly var c = ref Casters.Ref(0);
            var act = c.Activation;
            if (!IsImmune(slot, act))
            {
                if (!polyInit)
                {
                    poly = Bounds.Shape.Offset(-1f); // shrink polygon by 1 yalm for less suspect kb
                    polyInit = true;
                }
                hints.AddForbiddenZone(new SDKnockbackInComplexPolygonFixedDirection(Center, 15f * (c.Direction + 90f.Degrees()).ToDirection(), poly), c.Activation);
            }
        }
    }
}

sealed class ArenaChanges(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x1C)
        {
            switch (state)
            {
                case 0x00020001u:
                    var center = Center;
                    var c2 = new WPos(-750f, -750f);
                    var shape = new AOEShapeCustom(Center, [new Square(c2, 29.5f)], [new Polygon(c2, 13.279f, 3, 180f.Degrees())]);
                    _aoe = [new(shape, center, default, World.FutureTime(5.7d), shapeDistance: shape.Distance(center, default))];
                    break;
                case 0x00200010u:
                    _aoe = [];
                    var arena = new ArenaBoundsCustom([new Polygon(new(-750f, -750f), 13.279f, 3, 180f.Degrees())]);
                    Bounds = arena;
                    Center = arena.Center;
                    break;
                case 0x00080004u:

                    (Center, Bounds) = A13Azeyma.BuildArena();
                    break;
            }
        }
    }
}
