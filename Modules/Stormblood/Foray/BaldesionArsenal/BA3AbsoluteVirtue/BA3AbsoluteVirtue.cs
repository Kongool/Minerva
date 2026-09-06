// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Foray.BaldesionArsenal.BA3AbsoluteVirtue;

sealed class Meteor(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Meteor);
sealed class MedusaJavelin(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MedusaJavelin, new AOEShapeCone(65.4f, 45f.Degrees()));
sealed class AuroralWind(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.AuroralWind, 5f, tankbuster: true);

sealed class ExplosiveImpulse(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.ExplosiveImpulse1, (uint)AID.ExplosiveImpulse2], 18f);
sealed class AernsWynavExplosion(ModuleBase module) : Components.CastHint(module, (uint)AID.ExplosionWyvern, "Aerns Wyvnav is enraging!", true);
sealed class MeteorEnrageCounter(ModuleBase module) : Components.CastCounter(module, (uint)AID.MeteorEnrageRepeat);

[ModuleInfo(Group = ModuleGroup.BaldesionArsenal, GroupID = 639u, CFCID = 639u, NameID = 7976u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class BA3AbsoluteVirtue(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(-175, 314), 29.95f, 96), new Rectangle(new(-146f, 314f), 0.8f, 5.8f), new Rectangle(new(-175f, 285f), 6f, 1.05f)],
    [new Rectangle(new(-144.4f, 314f), 0.8f, 5.8f), new Polygon(new(-144.85f, 306.75f), 1.5f, 8, 22.5f.Degrees()), new Polygon(new(-144.85f, 321.25f), 1.5f, 8, 22.5f.Degrees()),
    new Rectangle(new(-206, 314), 1.525f, 20f)]);

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.AernsWynav));
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.AernsWynav => 1,
                _ => 0
            };
        }
    }
}
