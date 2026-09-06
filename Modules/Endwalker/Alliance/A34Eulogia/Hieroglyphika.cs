// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A34Eulogia;

// see A31 for details; apparently there is only 1 pattern here (rotated CW or CCW)
// unlike A31, origins are not cell centers, but south sides
sealed class Hieroglyphika(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.HieroglyphikaAOE)
{
    public bool BindsAssigned;
    public readonly List<AOEInstance> AOEs = [];

    private static readonly AOEShapeRect _shape = new(12f, 6f);
    private static readonly WDir[] _canonicalSafespots = [new(-18f, 18f), new(18f, -6f)];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(AOEs);

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.Bind)
            BindsAssigned = true;
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        WDir dir = iconID switch
        {
            (uint)IconID.HieroglyphikaCW => new(-1f, 0f),
            (uint)IconID.HieroglyphikaCCW => new(1f, 0f),
            _ => default
        };
        if (dir == default)
        {
            return;
        }

        var safespots = new WDir[2];
        for (var i = 0; i < 2; ++i)
        {
            safespots[i] = _canonicalSafespots[i].Rotate(dir);
        }
        var activation = World.FutureTime(17.1d);
        for (var z = -3; z <= 3; z += 2)
        {
            for (var x = -3; x <= 3; x += 2)
            {
                var cellOffset = new WDir(x * 6f, z * 6f);
                for (var i = 0; i < 2; ++i)
                {
                    if (safespots[i] == cellOffset)
                    {
                        goto next;
                    }
                }
                AOEs.Add(new(_shape, (Center + cellOffset + new WDir(default, 6f)).Quantized(), 180f.Degrees(), activation));
            next:
                ;
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == WatchedAction)
        {
            ++NumCasts;
            AOEs.Clear();
        }
    }
}
