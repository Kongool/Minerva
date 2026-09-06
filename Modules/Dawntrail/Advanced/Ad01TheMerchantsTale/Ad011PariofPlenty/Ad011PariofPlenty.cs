// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Modules.Dawntrail.Advanced.Ad01TheMerchantsTale.Ad011PariofPlenty;

sealed class HeatBurst(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.HeatBurst);

sealed class BurningGleam(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.BurningGleam, (uint)AID.BurningGleam1, (uint)AID.BurningGleam2], new AOEShapeCross(40f, 5f));

sealed class CharmedChains(ModuleBase module) : Components.Chains(module, (uint)TetherID.CharmedChain);

sealed class SimpleFableFlight(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.LeftFableflight, (uint)AID.RightFableflight], new AOEShapeCone(60f, 90f.Degrees()));

sealed class FireOfVictory(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.FireOfVictory, 4f);

sealed class FellSpark(ModuleBase module) : Components.InterceptTetherStatus(module, (uint)AID.FellSpark, (uint)TetherID.FellSpark, (uint)SID.DarkResistanceDown);

sealed class CurseOfCompanionshipSolitude(ModuleBase module) : Components.StatusStackSpread(module, (uint)SID.CurseOfCompanionship, (uint)SID.CurseOfSolitude, 15f, 15f);

sealed class SpurningFlames(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.SpurningFlames);
sealed class ImpassionedSpark(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ImpassionedSparks3, 8f);
sealed class BurningPillar(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BurningPillar, 10f);
sealed class SparkPuddle(ModuleBase module) : Components.Voidzone(module, 10f, GetPuddles)
{
    private static Actor[] GetPuddles(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.SparkPuddle);
        var count = enemies.Count;
        var index = 0;
        var puddles = new Actor[count];
        for (var i = 0; i < count; i++)
        {
            var z = enemies[i];
            if (z.EventState != 7)
            {
                puddles[index++] = z;
            }
        }
        return puddles[..index];
    }
}

sealed class FireWell(ModuleBase module) : Components.StackWithIcon(module, (uint)IconID.Stack, (uint)AID.FireWell, 6f, 3d);

sealed class ScouringScorn(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ScouringScorn);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1079u, CFCID = 1084u, NameID = 14274u, PrimaryActorOID = (uint)OID.PariOfPlenty, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "HerStolenLight (ported from BMR)")]
[SkipLocalsInit]
public sealed class PariOfPlenty(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-760f, -805f), new ArenaBoundsSquare(20f));
