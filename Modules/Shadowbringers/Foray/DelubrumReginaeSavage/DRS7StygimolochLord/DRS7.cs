// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS7StygimolochLord;

sealed class FoeSplitter(ModuleBase module) : Components.Cleave(module, (uint)AID.FoeSplitter, new AOEShapeCone(9f, 45f.Degrees())); // TODO: verify angle
sealed class ThunderousDischarge(ModuleBase module) : Components.CastCounter(module, (uint)AID.ThunderousDischargeAOE);
sealed class ThousandTonzeSwing(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThousandTonzeSwing, 20f);
sealed class Whack(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WhackAOE, new AOEShapeCone(40f, 30f.Degrees()));
sealed class DevastatingBoltOuter(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DevastatingBoltOuter, new AOEShapeDonut(25f, 30f));
sealed class DevastatingBoltInner(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DevastatingBoltInner, new AOEShapeDonut(12f, 17f));
sealed class Electrocution(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Electrocution, 3f);

// TODO: ManaFlame component - show reflect hints
[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 761u, CFCID = 761u, NameID = 9759u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class DRS7StygimolochLord : ModuleBase
{
    public DRS7StygimolochLord(WorldState ws, Actor primary) : this(ws, primary, BuildArena()) { }

    private DRS7StygimolochLord(WorldState ws, Actor primary, (WPos center, ArenaBoundsCustom arena) a) : base(ws, primary, a.center, a.arena) { }

    public static (WPos center, ArenaBoundsCustom arena) BuildArena()
    {
        var arena = new ArenaBoundsCustom([new Polygon(new(-416f, -184f), 34.5f, 48)], [new Rectangle(new(-416f, -219f), 20f, 1.4f), new Rectangle(new(-416f, -149.014f), 20f, 1.25f)]);
        return (arena.Center, arena);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        base.DrawEnemies(pcSlot, pc);
        Arena.Actors(Enemies((uint)OID.StygimolochMonk));
        Arena.Actors(Enemies((uint)OID.BallOfEarth), Colors.Object);
        Arena.Actors(Enemies((uint)OID.BallOfFire), Colors.Object);
    }
}
