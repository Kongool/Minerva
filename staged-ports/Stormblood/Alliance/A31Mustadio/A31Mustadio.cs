// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Alliance.A31Mustadio;

class EnergyBurst(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.EnergyBurst);
class ArmShot(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.ArmShot);
class LegShot(ModuleBase module) : Components.Voidzone(module, 3f, GetVoidzones)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.LegShotVoidzone);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (z.EventState != 7)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }
}

abstract class Handgonne(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(30f, 105f.Degrees()));
class LeftHandgonne(ModuleBase module) : Handgonne(module, (uint)AID.LeftHandgonne);
class RightHandgonne(ModuleBase module) : Handgonne(module, (uint)AID.RightHandgonne);

class SatelliteBeam(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SatelliteBeam, new AOEShapeRect(30, 15)); // Satellite Beam and Compress can both be shown earleir through tether
class Compress(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Compress, new AOEShapeRect(100, 7.5f));

class BallisticSpread(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.BallisticImpact1, 6);

[ModuleInfo(CFCID = 636u, NameID = 7915u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")] // 7919 
public class A31Mustadio(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(600, 290), new ArenaBoundsSquare(30, 45.Degrees()));
