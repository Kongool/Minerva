// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Trial.T03QueenEternal;

sealed class ProsecutionOfWar(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.ProsecutionOfWar);
sealed class VirtualShiftRoyalDomain(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.VirtualShift1, (uint)AID.VirtualShift2, (uint)AID.VirtualShift3, (uint)AID.RoyalDomain]);
sealed class BrutalCrown(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BrutalCrown, new AOEShapeDonut(5f, 60f));
sealed class DynasticDiadem(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DynasticDiadem, new AOEShapeDonut(6f, 70f));
sealed class RoyalBanishment(ModuleBase module) : Components.SimpleAOEGroupsByTimewindow(module, [(uint)AID.RoyalBanishment], new AOEShapeCone(100f, 15f.Degrees()));

abstract class RaidwideMulti(ModuleBase module, uint aid) : Components.RaidwideCast(module, aid, "multiple Raidwides");
sealed class RoyalBanishmentRaidwide(ModuleBase module) : RaidwideMulti(module, (uint)AID.RoyalBanishmentVisual);
sealed class AbsoluteAuthorityRaidwide(ModuleBase module) : RaidwideMulti(module, (uint)AID.AbsoluteAuthorityRaidwide1);

sealed class T03QueenEternalStates : StateMachineBuilder
{
    public T03QueenEternalStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChanges>()
            .ActivateOnEnter<Besiegement>()
            .ActivateOnEnter<LegitimateForce>()
            .ActivateOnEnter<Aethertithe>()
            .ActivateOnEnter<WaltzOfTheRegalia>()
            .ActivateOnEnter<WaltzOfTheRegaliaBait>()
            .ActivateOnEnter<RuthlessRegalia>()
            .ActivateOnEnter<ProsecutionOfWar>()
            .ActivateOnEnter<VirtualShiftRoyalDomain>()
            .ActivateOnEnter<AbsoluteAuthorityRaidwide>()
            .ActivateOnEnter<DownburstKB>()
            .ActivateOnEnter<PowerfulGustKB>()
            .ActivateOnEnter<PowerfulGustDownburstRW>()
            .ActivateOnEnter<BrutalCrown>()
            .ActivateOnEnter<AbsoluteAuthorityCircle>()
            .ActivateOnEnter<AuthoritysGaze>()
            .ActivateOnEnter<AuthoritysHold>()
            .ActivateOnEnter<AbsoluteAuthorityDorito>()
            .ActivateOnEnter<AbsoluteAuthorityFlare>()
            .ActivateOnEnter<DynasticDiadem>()
            .ActivateOnEnter<DivideAndConquer>()
            .ActivateOnEnter<RoyalBanishment>();
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 984u, CFCID = 984u, NameID = 13029u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class T03QueenEternal(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsSquare(20f))
{
    public static readonly ArenaBoundsRect FinalBounds = new(20f, 15f), SplitGravityBounds = new(12f, 8f);

    public static Shape[] GetXArenaRects() => [new Rectangle(new(100f, 82.5f), 12.5f, 2.5f), new Rectangle(new(100f, 102.5f), 12.5f, 2.5f),
    new Cross(new(100f, 92.5f), 15f, 2.5f, 45f.Degrees())];
    public static ArenaBoundsCustom GetXArena() => new(GetXArenaRects());
    public static Rectangle[] GetSplitArenaRects() => [new Rectangle(new(108f, 94f), 4f, 8f), new Rectangle(new(92f, 94f), 4f, 8f)];
    public static ArenaBoundsCustom GetSplitArena() => new(GetSplitArenaRects());
}
