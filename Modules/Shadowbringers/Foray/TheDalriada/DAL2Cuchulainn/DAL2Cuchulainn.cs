// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.TheDalriada.DAL2Cuchulainn;

sealed class PutrifiedSoulBurgeoningDreadGhastlyAura(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.PutrifiedSoul1, (uint)AID.PutrifiedSoul1, (uint)AID.BurgeoningDread1, (uint)AID.BurgeoningDread2,
(uint)AID.GhastlyAura1, (uint)AID.GhastlyAura2]);
sealed class MightOfMalice(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.MightOfMalice);
sealed class NecroticBillow(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.NecroticBillow, 8f);
sealed class AmbientPulsation : Components.SimpleAOEs
{
    public AmbientPulsation(ModuleBase module) : base(module, (uint)AID.AmbientPulsation, 12f, 6)
    {
        MaxDangerColor = 3;
    }
}

sealed class GhastlyAura(ModuleBase module) : Components.TemporaryMisdirection(module, (uint)AID.GhastlyAura1);
sealed class FellFlowAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FellFlow, new AOEShapeCone(50f, 60f.Degrees()));
sealed class FellFlowBait(ModuleBase module) : Components.BaitAwayIcon(module, new AOEShapeCone(50f, 15f.Degrees()), (uint)IconID.FellFlow, (uint)AID.FellFlowBait, 5.2d);

[ModuleInfo(Group = ModuleGroup.TheDalriada, GroupID = 778u, CFCID = 778u, NameID = 10004u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class DAL2Cuchulainn : ModuleBase
{
    public DAL2Cuchulainn(WorldState ws, Actor primary) : this(ws, primary, BuildArena()) { }

    private DAL2Cuchulainn(WorldState ws, Actor primary, (WPos center, ArenaBoundsCustom arena) a) : base(ws, primary, a.center, a.arena) { }

    public static (WPos center, ArenaBoundsCustom arena) BuildArena()
    {
        var arena = new ArenaBoundsCustom([new Polygon(new(650f, -187.4f), 25.199f, 48)], [new Rectangle(new(650f, -162f), 20f, 1.25f), new Rectangle(new(650f, -213f), 20f, 1.25f)]);
        return (arena.Center, arena);
    }
}
