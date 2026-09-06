// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M02NHoneyBLovely;

sealed class CallMeHoney(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.CallMeHoney);
sealed class TemptingTwist(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.TemptingTwist1, (uint)AID.TemptingTwist2], new AOEShapeDonut(7f, 30f));
sealed class HoneyBeeline(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.HoneyBeeline1, (uint)AID.HoneyBeeline2], new AOEShapeRect(60f, 7f));

sealed class HoneyedBreeze(ModuleBase module) : Components.BaitAwayIcon(module, new AOEShapeCone(40f, 15f.Degrees()), (uint)IconID.HoneyedBreezeTB, (uint)AID.HoneyedBreeze, 5f, tankbuster: true);

sealed class HoneyBLive(ModuleBase module) : Components.RaidwideCastDelay(module, (uint)AID.HoneyBLiveVisual, (uint)AID.HoneyBLive, 8.3f);
sealed class Heartsore(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.Heartsore, 6f);
sealed class Heartsick(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.Heartsick, 6f, 4, 4);
sealed class Loveseeker(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Loveseeker, 10f);
sealed class BlowKiss(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BlowKiss, new AOEShapeCone(40f, 60f.Degrees()));
sealed class HoneyBFinale(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.HoneyBFinale);
sealed class DropOfVenom(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.DropOfVenom, 6f, 8, 8);
sealed class SplashOfVenom(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.SplashOfVenom, 6f);

sealed class BlindingLove1 : Components.SimpleAOEs
{
    public BlindingLove1(ModuleBase module) : base(module, (uint)AID.BlindingLove1, new AOEShapeRect(50f, 4f)) { MaxDangerColor = 2; }
}
sealed class BlindingLove2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BlindingLove2, new AOEShapeRect(50f, 4f));
sealed class HeartStruck1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HeartStruck1, 4f);
sealed class HeartStruck2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HeartStruck2, 6f);
sealed class HeartStruck3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HeartStruck3, 10f, maxCasts: 8);

sealed class Fracture(ModuleBase module) : Components.CastTowers(module, (uint)AID.Fracture, 4f)
{
    public override void Update()
    {
        var count = Towers.Count;
        if (count == 0)
            return;
        var party = Raid.WithoutSlot(false, true, true);
        var len = party.Length;
        BitMask forbidden = default;
        for (var i = 0; i < len; ++i)
        {
            ref readonly var statuses = ref party[i].Statuses;
            var lenStatuses = statuses.Length;
            for (var j = 0; j < lenStatuses; ++j)
            {
                if (statuses[j].ID is ((uint)SID.HeadOverHeels) or ((uint)SID.HopelessDevotion))
                {
                    forbidden[i] = true;
                }
            }
        }
        var towers = CollectionsMarshal.AsSpan(Towers);
        for (var i = 0; i < count; ++i)
        {
            ref var t = ref towers[i];
            t.ForbiddenSoakers = forbidden;
        }
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 987u, CFCID = 987u, NameID = 12685u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class M02NHoneyBLovely(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsCircle(20f));
