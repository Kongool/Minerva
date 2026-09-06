// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Foray.BaldesionArsenal.BA2Raiden;

sealed class SpiritsOfTheFallen(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.SpiritsOfTheFallen)
{
    public override bool KeepOnPhaseChange => true;
}
sealed class Levinwhorl(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Levinwhorl);
sealed class Shingan(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Shingan)
{
    public override bool KeepOnPhaseChange => true;
}
sealed class AmeNoSakahoko(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AmeNoSakahoko, 25f)
{
    public override bool KeepOnPhaseChange => true;
}
sealed class WhirlingZantetsuken(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WhirlingZantetsuken, new AOEShapeDonut(5f, 60f))
{
    public override bool KeepOnPhaseChange => true;
}
sealed class Shock(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Shock, 8f);
sealed class ForHonor(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ForHonor, 11.4f);

sealed class LateralZantetsuken(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.LateralZantetsuken1, (uint)AID.LateralZantetsuken2], new AOEShapeRect(75.4f, 19.5f));

sealed class BitterBarbs(ModuleBase module) : Components.Chains(module, (uint)TetherID.Chains, (uint)AID.BitterBarbs);
sealed class BoomingLament(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BoomingLament, 10f);
sealed class SilentLevin(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SilentLevin, 5f);

sealed class UltimateZantetsuken(ModuleBase module) : Components.CastHint(module, (uint)AID.UltimateZantetsuken, "Enrage, kill the adds!", true);

[ModuleInfo(Group = ModuleGroup.BaldesionArsenal, CFCID = 639u, NameID = 7973u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class BA2Raiden(WorldState ws, Actor primary) : ModuleBase(ws, primary, startingArena.Center, startingArena)
{
    private static readonly WPos ArenaCenter = new(default, 458f);
    private static readonly ArenaBoundsCustom startingArena = new([new Polygon(ArenaCenter, 34.6f, 80)], [new Rectangle(new(35.3f, 458f), 0.99f, 20f), new Rectangle(new(-35.4f, 458f), 1.65f, 20f),
    new Rectangle(new(default, 493f), 20f, 0.75f)]);
    public static readonly ArenaBoundsCustom DefaultArena = new([new Polygon(ArenaCenter, 29.93f, 64)]);

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.StreakLightning));
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.StreakLightning => 1,
                _ => 0
            };
        }
    }
}
