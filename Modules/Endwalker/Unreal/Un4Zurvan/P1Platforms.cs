// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Unreal.Un4Zurvan;

class P1Platforms(ModuleBase module) : Components.GenericAOEs(module)
{
    public List<AOEInstance> ForbiddenPlatforms = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(ForbiddenPlatforms);

    public override void OnActorEAnim(Actor actor, uint state)
    {
        var dir = actor.OID switch
        {
            (uint)OID.PlatformE => 90f.Degrees(),
            (uint)OID.PlatformN => 180f.Degrees(),
            (uint)OID.PlatformW => -90f.Degrees(),
            _ => default
        };
        if (dir == default)
            return;

        switch (state)
        {
            case 0x00040008:
                ForbiddenPlatforms.Add(new(new AOEShapeCone(20f, 45f.Degrees()), Center, dir, World.FutureTime(5d)));
                break;
            case 0x00100020:
                ++NumCasts;
                break;
        }
    }
}
