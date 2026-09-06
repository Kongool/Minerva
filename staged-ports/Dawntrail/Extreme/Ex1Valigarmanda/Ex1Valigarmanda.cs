// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex1Valigarmanda;

sealed class SkyruinFire(ModuleBase module) : Components.CastCounter(module, (uint)AID.SkyruinFireAOE);
sealed class SkyruinIce(ModuleBase module) : Components.CastCounter(module, (uint)AID.SkyruinIceAOE);
sealed class SkyruinThunder(ModuleBase module) : Components.CastCounter(module, (uint)AID.SkyruinThunderAOE);
sealed class DisasterZoneFire(ModuleBase module) : Components.CastCounter(module, (uint)AID.DisasterZoneFireAOE);
sealed class DisasterZoneIce(ModuleBase module) : Components.CastCounter(module, (uint)AID.DisasterZoneIceAOE);
sealed class DisasterZoneThunder(ModuleBase module) : Components.CastCounter(module, (uint)AID.DisasterZoneThunderAOE);
sealed class Tulidisaster1(ModuleBase module) : Components.CastCounter(module, (uint)AID.TulidisasterAOE1);
sealed class Tulidisaster2(ModuleBase module) : Components.CastCounter(module, (uint)AID.TulidisasterAOE2);
sealed class Tulidisaster3(ModuleBase module) : Components.CastCounter(module, (uint)AID.TulidisasterAOE3);
sealed class IceTalon(ModuleBase module) : Components.BaitAwayIcon(module, 6f, (uint)IconID.IceTalon, (uint)AID.IceTalonAOE, 5.1d, tankbuster: true);
sealed class WrathUnfurled(ModuleBase module) : Components.CastCounter(module, (uint)AID.WrathUnfurledAOE);
sealed class TulidisasterEnrage1(ModuleBase module) : Components.CastCounter(module, (uint)AID.TulidisasterEnrageAOE1);
sealed class TulidisasterEnrage2(ModuleBase module) : Components.CastCounter(module, (uint)AID.TulidisasterEnrageAOE2);
sealed class TulidisasterEnrage3(ModuleBase module) : Components.CastCounter(module, (uint)AID.TulidisasterEnrageAOE3);

// TODO: investigate how exactly are omens drawn for northern cross & susurrant breath
[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 833u, CFCID = 833u, NameID = 12854u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Ex1Valigarmanda(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsRect(20f, 15f))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.IceBoulderJail));
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.IceBoulderJail => 2,
                (uint)OID.Boss => 1,
                _ => 0
            };
        }
    }
}

