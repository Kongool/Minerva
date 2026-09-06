// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A21AegisUnit;

sealed class AntiPersonnelLaser(ModuleBase module) : Components.BaitAwayIcon(module, 3f, (uint)IconID.AntiPersonnelLaser, (uint)AID.AntiPersonnelLaser, 4d, tankbuster: true);
sealed class FlightPath(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FlightPath, new AOEShapeRect(60f, 5f));
sealed class HighPoweredLaser(ModuleBase module) : Components.StackWithIcon(module, (uint)IconID.HighPoweredLaser, (uint)AID.HighPoweredLaser, 6f, 5.1d, 8, 8);
sealed class LifesLastSong(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LifesLastSong, new AOEShapeCone(30f, 50f.Degrees()), 3);
sealed class ManeuverDiffusionCannon(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ManeuverDiffusionCannon);
sealed class ManeuverSaturationBombing(ModuleBase module) : Components.CastHint(module, (uint)AID.ManeuverSaturationBombing, "Enrage!", true);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 736u, CFCID = 736u, NameID = 9642u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class A21AegisUnit(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(-230f, 192f), 25f, 90), new Polygon(new(-230f, 209.5f), 12.144f, 64), new Polygon(new(-214.845f, 183.25f), 12.144f, 64),
    new Polygon(new(-245.155f, 183.25f), 12.144f, 64)], [new Polygon(new(-230f, 192f), 10.5f, 90)]);

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.FlightUnit));
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.FlightUnit => 1,
                _ => 0
            };
        }
    }
}
