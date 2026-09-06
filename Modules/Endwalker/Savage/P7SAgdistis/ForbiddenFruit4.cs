// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P7SAgdistis;

class ForbiddenFruit4(ModuleBase module) : ForbiddenFruitCommon(module, (uint)AID.BullishSwipeAOE)
{
    private int _bullPlatform;

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        base.DrawArenaForeground(pcSlot, pc);
        if (NumAssignedTethers > 0 && !MinotaursBaited && TetherSources[pcSlot] == null)
        {
            Arena.ZoneCircleOutline(Center - 2 * PlatformDirection(_bullPlatform).ToDirection(), 2, Colors.Safe);
        }
    }

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        var slot = TryAssignTether(source, tether);
        if (slot < 0)
            return;
        switch (tether.ID)
        {
            case (uint)TetherID.Bull:
                SafePlatforms[slot].Set(_bullPlatform);
                break;
            case (uint)TetherID.MinotaurFar:
            case (uint)TetherID.MinotaurClose:
                var safePlatforms = ValidPlatformsMask;
                safePlatforms.Clear(_bullPlatform);
                safePlatforms.Clear(PlatformIDFromOffset(source.Position - Center));
                SafePlatforms[slot] = safePlatforms;
                break;
        }
    }

    protected override DateTime? PredictUntetheredCastStart(Actor fruit)
    {
        if (fruit.OID == (uint)OID.ForbiddenFruitBull)
            _bullPlatform = PlatformIDFromOffset(fruit.Position - Center);
        return null;
    }
}
