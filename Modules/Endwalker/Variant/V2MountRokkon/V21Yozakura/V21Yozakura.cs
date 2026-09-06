// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V2MountRokkon.V21Yozakura;

sealed class GloryNeverlasting(ModuleBase module) : Components.SingleTargetDelayableCast(module, (uint)AID.GloryNeverlasting);
sealed class ArtOfTheFireblossom(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ArtOfTheFireblossom, 9f);
sealed class ArtOfTheWindblossom(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ArtOfTheWindblossom, new AOEShapeDonut(5f, 60f));
sealed class KugeRantsuiOkaRanman(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.KugeRantsui, (uint)AID.OkaRanman]);
sealed class LevinblossomStrike(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LevinblossomStrike, 3f);

sealed class DriftingPetals(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.DriftingPetals, 15f, ignoreImmunes: true)
{
    private readonly Mudrain _aoe1 = module.FindComponent<Mudrain>()!;
    private readonly Witherwind _aoe2 = module.FindComponent<Witherwind>()!;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
        {
            ref readonly var c = ref Casters.Ref(0);
            var aoes = _aoe1.ActiveAOEs(slot, actor);
            var origin = c.Origin;
            var a20 = 20f.Degrees();
            var len = aoes.Length;
            var forbidden = new ShapeDistance[len + 1];
            forbidden[len] = new SDInvertedCircle(origin, 5f);

            for (var i = 0; i < len; ++i)
            {
                ref readonly var aoe = ref aoes[i];
                forbidden[i] = new SDCone(origin, 20f, Angle.FromDirection(aoe.Origin - origin), a20);
            }
            hints.AddForbiddenZone(new SDUnion(forbidden), c.Activation);
        }
    }

    public override bool DestinationUnsafe(int slot, Actor actor, WPos pos)
    {
        var aoes = _aoe1.ActiveAOEs(slot, actor);
        var len = aoes.Length;
        for (var i = 0; i < len; ++i)
        {
            if (aoes[i].Check(pos))
            {
                return true;
            }
        }
        var aoes2 = _aoe2.ActiveAOEs(slot, actor);
        var len2 = aoes2.Length;
        for (var i = 0; i < len2; ++i)
        {
            if (aoes2[i].Check(pos))
            {
                return true;
            }
        }
        return !InBounds(pos);
    }
}

sealed class Mudrain(ModuleBase module) : Components.VoidzoneAtCastTarget(module, 5f, (uint)AID.Mudrain, GetVoidzones, 0.7d)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.MudVoidzone);
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
sealed class Icebloom(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Icebloom, 6);
sealed class Shadowflight(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Shadowflight, new AOEShapeRect(10f, 3f));
sealed class MudPie(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MudPie, new AOEShapeRect(60f, 3f));
sealed class FireblossomFlare(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FireblossomFlare, 6f);
sealed class ArtOfTheFluff(ModuleBase module) : Components.CastGazes(module, [(uint)AID.ArtOfTheFluff1, (uint)AID.ArtOfTheFluff2]);
sealed class TatamiGaeshi(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TatamiGaeshi, new AOEShapeRect(40f, 5f));
sealed class AccursedSeedling(ModuleBase module) : Components.Voidzone(module, 4f, GetSeedlings)
{
    private static List<Actor> GetSeedlings(ModuleBase module) => module.Enemies((uint)OID.AccursedSeedling);
}

sealed class RootArrangement(ModuleBase module) : Components.StandardChasingAOEs(module, 4f, (uint)AID.RockRootArrangementFirst, (uint)AID.RockRootArrangementRest, 4, 1, 4, true, (uint)IconID.RootArrangement)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        if (TargetsMask[slot])
        {
            hints.AddForbiddenZone(new SDRect(Center + new WDir(19f, default), Center + new WDir(-19f, default), 20f), Activation);
        }
    }
}

sealed class Witherwind(ModuleBase module) : Components.Voidzone(module, 3f, GetWhirlwind, 20f)
{
    private static List<Actor> GetWhirlwind(ModuleBase module) => module.Enemies((uint)OID.AutumnalTempest);
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 945u, CFCID = 945u, NameID = 12325u, PrimaryActorOID = (uint)OID.Yozakura, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class V21Yozakura(WorldState ws, Actor primary) : ModuleBase(ws, primary, primary.PosRot.X is var X && X < -700f ? new(-775f, 16f) : X > 700f ? new(737f, 220f) : new(47f, 93f), X < -700f ? new ArenaBoundsSquare(22.5f) : X > 700f ? new ArenaBoundsSquare(19.5f) : new ArenaBoundsSquare(22.5f))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.LivingGaol));
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.LivingGaol => 1,
                _ => 0
            };
        }
    }
}
