// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P7SAgdistis;

class ForbiddenFruit8(ModuleBase module) : ForbiddenFruitCommon(module, (uint)AID.StymphalianStrike)
{
    private BitMask _noBirdsPlatforms = ValidPlatformsMask;

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        var slot = TryAssignTether(source, tether);
        if (slot < 0)
            return;
        var safe = ValidPlatformsMask & ~_noBirdsPlatforms;
        safe.Clear(PlatformIDFromOffset(source.Position - Center));
        SafePlatforms[slot] = safe;
    }

    protected override DateTime? PredictUntetheredCastStart(Actor fruit)
    {
        if (fruit.OID != (uint)OID.ForbiddenFruitBird)
            return null;

        _noBirdsPlatforms.Clear(PlatformIDFromOffset(fruit.Position - Center));
        Array.Fill(SafePlatforms, _noBirdsPlatforms);
        return World.FutureTime(12.5f);
    }
}
