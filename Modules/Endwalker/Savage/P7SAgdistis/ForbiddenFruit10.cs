// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P7SAgdistis;

// TODO: implement!
class ForbiddenFruit10(ModuleBase module) : ForbiddenFruitCommon(module, (uint)AID.BronzeBellows)
{
    private BitMask _minotaurPlaforms = ValidPlatformsMask;
    private BitMask _bullPlatforms;

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        base.DrawArenaBackground(pcSlot, pc);
        foreach (var (slot, target) in Raid.WithSlot(true, true, true))
        {
            var source = TetherSources[slot];
            if (source != null)
            {
                AOEShape shape = source.OID == (uint)OID.ImmatureMinotaur ? ShapeMinotaurTethered : ShapeRect;
                shape.Draw(Arena, source.Position, Angle.FromDirection(target.Position - source.Position));
            }
        }
    }

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        var slot = TryAssignTether(source, tether);
        if (slot < 0)
            return;
        var safe = tether.ID switch
        {
            (uint)TetherID.Bull => _bullPlatforms,
            (uint)TetherID.Bird => ValidPlatformsMask & ~(_minotaurPlaforms | _bullPlatforms),
            _ => _minotaurPlaforms
        };
        SafePlatforms[slot] = safe;
    }

    protected override DateTime? PredictUntetheredCastStart(Actor fruit)
    {
        switch (fruit.OID)
        {
            case (uint)OID.ForbiddenFruitMinotaur:
                // minotaurs spawn on bridges, minotaur platform is adjacent => their opposite platform is never minotaur one
                _minotaurPlaforms[PlatformIDFromOffset(Center - fruit.Position)] = false;
                break;
            case (uint)OID.ForbiddenFruitBull:
                _bullPlatforms[PlatformIDFromOffset(fruit.Position - Center)] = true;
                break;
        }
        return null;
    }
}
