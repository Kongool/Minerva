// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A24Ealdnarche;

sealed class UranosCascade(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.UranosCascade, 6f, tankbuster: true);
sealed class CronosSlingRect(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.CronosSlingRect1, (uint)AID.CronosSlingRect2], new AOEShapeRect(70f, 68f));
sealed class CronosSlingCircle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CronosSlingCircle, 9f);
sealed class CronosSlingDonut(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CronosSlingDonut, new AOEShapeDonut(6f, 70f));
sealed class EmpyrealVortexOmegaJavelin(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.EmpyrealVortexAOE, (uint)AID.OmegaJavelinAOE], 6f);
sealed class TornadoFlareBurst(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.TornadoAOE, (uint)AID.FlareCircle, (uint)AID.Burst], 5f);
sealed class EmpyrealVortexSpread(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.EmpyrealVortexSpread, 5f);
sealed class EmpyrealVortexRW(ModuleBase module) : Components.RaidwideCastDelay(module, (uint)AID.EmpyrealVortexVisual1, (uint)AID.EmpyrealVortexRW, 1d, "Raidwide x5")
{
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == WatchedAction)
        {
            if (++NumCasts == 5)
            {
                NumCasts = 0;
                Activation = default;
            }
        }
    }
}
sealed class OmegaJavelinSpread(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.OmegaJavelin, (uint)AID.OmegaJavelinSpread, 6f, 5.1d);

// not sure how this stack works, the sheets say radius 24 which doesn't make sense given that it is the whole arena radius
// maybe it's a raidwide and a stack combined, radius 5 is guessed, but a typical stack radius
sealed class StellarBurst(ModuleBase module) : Components.StackWithIcon(module, (uint)IconID.StellarBurst, (uint)AID.StellarBurst, 5f, 5.1d, PartyState.MaxAllianceSize, PartyState.MaxAllianceSize);

sealed class FlareRect(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FlareRect, new AOEShapeRect(70f, 3f));
sealed class QuakeFreeze(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.Quake, (uint)AID.Freeze], new AOEShapeRect(16f, 24f));

sealed class Tornado(ModuleBase module) : Components.Voidzone(module, 3f, GetTornado, 5f)
{
    private static Actor[] GetTornado(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.OrbitalWind);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (z.Renderflags == 0)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }
}

sealed class OrbitalLevin(ModuleBase module) : Components.StretchTetherSingle(module, (uint)TetherID.OrbitalLevin, 8f, needToKite: true);
sealed class Paralysis(ModuleBase module) : Components.CleansableDebuff(module, (uint)SID.Paralysis, "Paralysis", "paralyzed");

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1058u, CFCID = 1058u, NameID = 14086u, PrimaryActorOID = (uint)OID.Ealdnarche, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class A24Ealdnarche(WorldState ws, Actor primary) : ModuleBase(ws, primary, ArenaCenter, new ArenaBoundsSquare(24f))
{
    public static readonly WPos ArenaCenter = new(800f, -800f);
}
