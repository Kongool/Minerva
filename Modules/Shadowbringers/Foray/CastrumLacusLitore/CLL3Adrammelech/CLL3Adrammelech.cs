// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.CastrumLacusLitore.CLL3Adrammelech;

sealed class HolyIV(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.HolyIV);
sealed class Flare(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Flare);

abstract class WaterIV(ModuleBase module, uint aid) : Components.SimpleKnockbacks(module, aid, 12f)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
        {
            ref readonly var c = ref Casters.Ref(0);
            hints.AddForbiddenZone(new SDInvertedCircle(c.Origin, 13f), c.Activation);
        }
    }
}
sealed class WaterIV1(ModuleBase module) : WaterIV(module, (uint)AID.WaterIV1); // same time as WaterIV2
sealed class WaterIV3(ModuleBase module) : WaterIV(module, (uint)AID.WaterIV3); // same time as WaterIV4

sealed class BurstIITornado(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.BurstII, (uint)AID.Tornado], 6f);
sealed class Shock(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Shock, 35f);
sealed class AeroIV(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.AeroIV1, (uint)AID.AeroIV2], new AOEShapeDonut(15f, 30f));
sealed class ThunderIV(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.ThunderIV1, (uint)AID.ThunderIV2], 18f);
sealed class WarpedLight(ModuleBase module) : Components.SimpleChargeAOEGroups(module, [(uint)AID.WarpedLight1, (uint)AID.WarpedLight2,(uint)AID.WarpedLight3,
(uint)AID.WarpedLight4, (uint)AID.WarpedLight5, (uint)AID.WarpedLight6], 1.5f, riskyWithSecondsLeft: 1d);

sealed class Twister(ModuleBase module) : Components.Voidzone(module, 6.5f, GetTwister, 5f)
{
    private static List<Actor> GetTwister(ModuleBase module) => module.Enemies((uint)OID.Twister);
}

[ModuleInfo(Group = ModuleGroup.CastrumLacusLitore, GroupID = 735u, CFCID = 735u, NameID = 9442u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class CLL3Adrammelech(WorldState ws, Actor primary) : ModuleBase(ws, primary, startingArena.Center, startingArena)
{
    private static readonly WPos arenaCenter = new(80f, -606f);
    private static readonly ArenaBoundsCustom startingArena = new([new Polygon(arenaCenter, 29.5f, 48)], [new Rectangle(new(80f, -575.788f), 20f, 1.25f),
    new Rectangle(new(80f, -636.413f), 20f, 1.25f)]);
    public static readonly ArenaBoundsCustom DefaultArena = new([new Polygon(arenaCenter, 25f, 48)]);

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.ArcaneSphere));
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.ArcaneSphere => 1,
                _ => 0
            };
        }
    }
}
