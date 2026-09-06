// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Modules.Dawntrail.Raid.M09NVampFatale;

sealed class KillerVoice(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.KillerVoice);

sealed class HalfMoon(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.HalfMoon1, (uint)AID.HalfMoon2, (uint)AID.HalfMoon3, (uint)AID.HalfMoon4, (uint)AID.HalfMoon5, (uint)AID.HalfMoon6, (uint)AID.HalfMoon7, (uint)AID.HalfMoon8], new AOEShapeCone(64f, 90f.Degrees()), 1, 2);

sealed class VampStomp(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.VampStomp1, 10f);

sealed class Hardcore(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.Hardcore1, 6f);

sealed class Hardcore2(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.Hardcore2, 15f);

sealed class FlayingFry(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.FlayingFry1, 5f);

sealed class CoffinFiller(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.Coffinfiller, (uint)AID.Coffinfiller2, (uint)AID.Coffinfiller3], new AOEShapeRect(32f, 2.5f), 2);

sealed class BlastBeat(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BlastBeat, 8f); // Needs rework to give warning based on static spawns

sealed class DeadWake(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DeadWake1, new AOEShapeRect(10f, 10f));

sealed class PenetratingPitch(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.PenetratingPitch1, 5f, 2, 4);

sealed class BrutalRain(ModuleBase module) : Components.StackWithIcon(module, (uint)IconID.BrutalRain, (uint)AID.BrutalRain, 6f, 0d);  // Can't seem to make this wait for the BrutalRain1 casts and then vanish, so erring on the side of at least getting the stack in place for the first hit and hoping people will stay.

sealed class CrowdKill(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.CrowdKill); // Doesn't seem to like this, might need to do after yell?

sealed class PulpingPulse(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PulpingPulse, 5f);

sealed class AetherlettingHit(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.Aetherletting1, 6f);

sealed class AetherlettingCross(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Aetherletting2, new AOEShapeCross(40f, 3f));

sealed class InsatiableThirst(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.InsatiableThirst);

sealed class Plummet(ModuleBase module) : Components.CastTowers(module, (uint)AID.Plummet, 3f);

sealed class NeckBiter(ModuleBase module) : Components.Voidzone(module, 3f, GetNeckbiters, 2f)
{
    private static Actor[] GetNeckbiters(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.Neckbiter);
        var count = enemies.Count;
        var index = 0;
        var neckbiters = new Actor[count];
        for (var i = 0; i < count; i++)
        {
            var z = enemies[i];
            if (z.EventState != 7)
            {
                neckbiters[index++] = z;
            }
        }
        return neckbiters;

    }
}

sealed class CoffinMaker(ModuleBase module) : Components.Voidzone(module, 3f, GetCoffinMakers, 7f)
{
    private static Actor[] GetCoffinMakers(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.Coffinmaker1);
        var count = enemies.Count;
        var index = 0;
        var coffinmakers = new Actor[count];
        for (var i = 0; i < count; i++)
        {
            var z = enemies[i];
            if (z.EventState != 7)
            {
                coffinmakers[index++] = z;
            }
        }
        return coffinmakers;
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1068u, CFCID = 1068u, NameID = 14300u, PrimaryActorOID = (uint)OID.VampFatale, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "HerStolenLight (ported from BMR)")]
[SkipLocalsInit]
public sealed class VampFatale(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsSquare(20f));
