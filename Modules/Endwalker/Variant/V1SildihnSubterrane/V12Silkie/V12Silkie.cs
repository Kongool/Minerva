// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V1SildihnSubterrane.V12Silkie;

sealed class CarpetBeater(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.CarpetBeater);
sealed class TotalWashDustBluster(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.TotalWash, (uint)AID.DustBluster]);

sealed class BracingDuster(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.BracingDuster1, (uint)AID.BracingDuster2, (uint)AID.BracingDuster3], new AOEShapeDonut(5f, 60f));
sealed class ChillingDuster(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.ChillingDuster1, (uint)AID.ChillingDuster2, (uint)AID.ChillingDuster3], new AOEShapeCross(60f, 5f));

sealed class SlipperySoap(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.SlipperySoap, 5f);

sealed class SpotRemover(ModuleBase module) : Components.VoidzoneAtCastTarget(module, 5f, (uint)AID.SpotRemover, GetVoidzones, 0.8d)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.WaterVoidzone);
        var count = enemies.Count;
        if (count == 0)
        {
            return [];
        }
        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (z.EventState != 7)
            {
                voidzones[index++] = z;
            }
        }
        return voidzones[..index];
    }
}

sealed class PuffAndTumble(ModuleBase module) : Components.SimpleExaflare(module, 4f, (uint)AID.PuffAndTumbleFirst, (uint)AID.PuffAndTumbleRest, 10f, 2.2d, 5, 5);

sealed class SqueakyCleanConeSmall(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.SqueakyClean1E, (uint)AID.SqueakyClean2E,
(uint)AID.SqueakyClean1W, (uint)AID.SqueakyClean2W], new AOEShapeCone(60f, 45f.Degrees()));
sealed class SqueakyCleanConeBig(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.SqueakyClean3E, (uint)AID.SqueakyClean3W], new AOEShapeCone(60f, 112.5f.Degrees()));

[ModuleInfo(CFCID = 868u, NameID = 11369u, PrimaryActorOID = (uint)OID.Silkie, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public class V12Silkie(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-335f, -155f), new ArenaBoundsSquare(29.5f));