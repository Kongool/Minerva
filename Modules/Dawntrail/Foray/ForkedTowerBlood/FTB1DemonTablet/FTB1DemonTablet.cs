// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerBlood.FTB1DemonTablet;

sealed class DemonicDarkII(ModuleBase module) : Components.RaidwideCastDelay(module, (uint)AID.DemonicDarkIIVisual, (uint)AID.DemonicDarkII, 0.8d);
sealed class OccultChisel(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.OccultChisel, 5f, tankbuster: true);
sealed class RotationBig(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Rotation1, new AOEShapeCone(37f, 45f.Degrees()));
sealed class RotationSmall(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.Rotation2, (uint)AID.Rotation3], new AOEShapeRect(33f, 1.5f));
sealed class Summon(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Summon, new AOEShapeRect(36f, 15f));
sealed class DarkDefenses(ModuleBase module) : Components.Dispel(module, (uint)SID.DarkDefenses);
sealed class SummonedDemons(ModuleBase module) : Components.AddsMulti(module, [(uint)OID.SummonedArchDemon, (uint)OID.SummonedDemon], 1);

[ModuleInfo(Group = ModuleGroup.TheForkedTowerBlood, GroupID = 1018u, CFCID = 1018u, NameID = 13760u, PrimaryActorOID = (uint)OID.DemonTablet, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class FTB1DemonTablet(WorldState ws, Actor primary) : ModuleBase(ws, primary, arenaCenter, DefaultArena)
{
    private static readonly WPos arenaCenter = new(700f, 379f);
    public static readonly ArenaBoundsCustom DefaultArena = new([new Rectangle(arenaCenter, 15f, 33f)], [new Rectangle(arenaCenter, 15f, 3.5f)]);
    public static readonly ArenaBoundsCustom RotationArena = new([new Rectangle(arenaCenter, 15.5f, 33.5f)], [new Rectangle(arenaCenter, 15f, 3f, -89.98f.Degrees())], AdjustForHitboxInwards: true); // collision is slightly rotated
    public static readonly ArenaBoundsRect CompleteArena = new(15f, 33f);
}
