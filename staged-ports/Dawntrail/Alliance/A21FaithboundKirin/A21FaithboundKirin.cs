// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A21FaithboundKirin;

sealed class StonegaIVShatteringStomp(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.StonegaIV, (uint)AID.ShatteringStomp]);
sealed class Punishment(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Punishment);
sealed class CrimsonRiddle(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.CrimsonRiddle1, (uint)AID.CrimsonRiddle2], new AOEShapeCone(30f, 90f.Degrees()));
sealed class StonegaIII1(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.StonegaIII1, 6f);
sealed class StonegaIII2(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.StonegaIII2, 6f);
sealed class QuakeSmall(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.QuakeSmall, 6f);
sealed class QuakeBig(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.QuakeBig, 10f);
sealed class VermilionFlight(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.VermilionFlight, new AOEShapeRect(60f, 10f));
sealed class ArmOfPurgatory(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ArmOfPurgatory, 15f); // 3 + 12 from status effect
sealed class WallArenaChange(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WallArenaChange, new AOEShapeRect(5f, 8f));
sealed class GloamingGleam(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GloamingGleam, new AOEShapeRect(50f, 6f));
sealed class RazorFang(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RazorFang, 20f);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1058u, CFCID = 1058u, NameID = 14053u, PrimaryActorOID = (uint)OID.FaithboundKirin, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class A21FaithboundKirin : ModuleBase
{
    public A21FaithboundKirin(WorldState ws, Actor primary) : this(ws, primary, BuildArena()) { }

    private A21FaithboundKirin(WorldState ws, Actor primary, (WPos center, ArenaBoundsCustom arena) a) : base(ws, primary, a.center, a.arena) { }

    public static (WPos center, ArenaBoundsCustom arena) BuildArena()
    {
        var arena = new ArenaBoundsCustom([new Polygon(new(-850f, 780f), 29.5f, 60)]);
        return (arena.Center, arena);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.ChiseledArm2));
        Arena.Actors(Enemies((uint)OID.ChiseledArm3));
    }
}
