// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A21FaithboundKirin;

sealed class ArenaChanges(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnMapEffect(byte index, uint state)
    {
        switch (index)
        {
            case 0x3E: // byakko
                switch (state)
                {
                    case 0x00020001u:
                        var a45 = 45f.Degrees();
                        var a90 = 89.98f.Degrees();
                        ArenaBoundsCustom arena = new([new Polygon(new(-850f, 780f), 30f, 60)], [new Rectangle(new(-850f, 752.5f), 5f, 2.5f),
                        new Rectangle(new(-830.55426f, 760.55426f), 5f, 2.5f, -a45),
                        new Rectangle(new(-822.5f, 780f), 5f, 2.5f, -a90), new Rectangle(new(-830.55426f, 799.44574f), 2.5f, 5f, -a45),
                        new Rectangle(new(-850f, 807.5f), 5f, 2.5f), new Rectangle(new(-869.44574f, 799.44574f), 2.5f, 5f, a45), new Rectangle(new(-877.5f, 780f), 5f, 2.5f, a90),
                        new Rectangle(new(-869.44574f, 760.55426f), 5f, 2.5f, a45)], AdjustForHitboxInwards: true);
                        Bounds = arena;
                        Center = arena.Center;
                        break;
                    case 0x00080004u:
                        (Center, Bounds) = A21FaithboundKirin.BuildArena();
                        break;
                }
                break;
            case 0x46: // mighty grip
                switch (state)
                {
                    case 0x00200010u:
                        var center = Center;
                        AOEShapeCustom aoeshape = new(center, [new Square(center, 29.5f)], [new Rectangle(new(-850f, 785f), 12.5f, 15f)]); // we can use a square as base here because it gets clipped with arena bounds anyway
                        _aoe = [new(aoeshape, center, default, World.FutureTime(11.1d), shapeDistance: aoeshape.Distance(center, default))];
                        break;
                    case 0x00020001u:
                        Bounds = new ArenaBoundsRect(12.5f, 15f);
                        Center = new(-850f, 785f);
                        _aoe = [];
                        break;
                    case 0x00080004u:
                        (Center, Bounds) = A21FaithboundKirin.BuildArena();
                        break;
                }
                break;
            case 0x4B: // suzaku
                switch (state)
                {
                    case 0x00020001u:
                        Bounds = new ArenaBoundsSquare(20f);
                        break;
                    case 0x00080004u:
                        (Center, Bounds) = A21FaithboundKirin.BuildArena();
                        break;
                }
                break;
        }
    }
}
