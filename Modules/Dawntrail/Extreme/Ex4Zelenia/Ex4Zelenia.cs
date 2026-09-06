// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex4Zelenia;

sealed class SpecterOfTheLost(ModuleBase module) : Components.TankbusterTether(module, (uint)AID.SpecterOfTheLost, (uint)TetherID.SpecterOfTheLost, new AOEShapeCone(48f, 22.5f.Degrees()), 7.8d);
sealed class AlexandrianThunderIIISpread(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.AlexandrianThunderIIISpread, 4f);
sealed class AlexandrianBanishII(ModuleBase module) : Components.StackWithIcon(module, (uint)IconID.AlexandrianBanishII, (uint)AID.AlexandrianBanishII, 4f, 5.8f, 4, 4);
sealed class AlexandrianThunderIIIAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AlexandrianThunderIIIAOE, 4f);
sealed class HolyHazard(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HolyHazard, new AOEShapeCone(24f, 60f.Degrees()), 2);
sealed class PowerBreak(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.PowerBreak1, (uint)AID.PowerBreak2], new AOEShapeRect(24f, 32f));
sealed class ThunderSlash : Components.SimpleAOEs
{
    public ThunderSlash(ModuleBase module) : base(module, (uint)AID.ThunderSlash, new AOEShapeCone(24f, 30f.Degrees()), 2)
    {
        MaxDangerColor = 2;
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1031u, CFCID = 1031u, NameID = 13861u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class Ex4Zelenia(WorldState ws, Actor primary) : ModuleBase(ws, primary, arenaCenter, DefaultArena)
{
    private static readonly WPos arenaCenter = new(100f, 100f);
    public static readonly ArenaBoundsCustom DefaultArena = new([new Polygon(arenaCenter, 16f, 64)]);
    public static readonly ArenaBoundsCustom DonutArena = new([new DonutV(arenaCenter, 2f, 16f, 64)]);
}