// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Quest.MSQ.Endwalker;

sealed class Megaflare(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Megaflare, 6f);
sealed class Puddles(ModuleBase module) : Components.PersistentInvertibleVoidzoneByCast(module, 5f, GetVoidzones, (uint)AID.Hellfire)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.Puddles);
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

sealed class JudgementBolt(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.JudgementBoltVisual);
sealed class Hellfire(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.HellfireVisual);
sealed class StarBeyondStars(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.StarBeyondStarsHelper, new AOEShapeCone(50f, 15f.Degrees()), 6);
sealed class TheEdgeUnbound(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TheEdgeUnbound, 10f);
sealed class WyrmsTongue(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WyrmsTongueHelper, new AOEShapeCone(40f, 30f.Degrees()));

sealed class NineNightsAvatar : Components.SimpleAOEs
{
    public NineNightsAvatar(ModuleBase module) : base(module, (uint)AID.NineNightsAvatar, 10f) { Color = Colors.Danger; }
}

sealed class NineNightsHelpers : Components.SimpleAOEs
{
    public NineNightsHelpers(ModuleBase module) : base(module, (uint)AID.NineNightsHelpers, 10f, 6) { MaxDangerColor = 2; }
}
sealed class VeilAsunder(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.VeilAsunderHelper, 6f);
sealed class MortalCoil(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MortalCoilVisual, new AOEShapeDonut(8f, 20f));
sealed class DiamondDust(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.DiamondDustVisual, "Raidwide. Turns floor to ice.");
sealed class DeadGaze(ModuleBase module) : Components.CastGaze(module, (uint)AID.DeadGazeVisual);
sealed class TidalWave2(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.TidalWaveVisual2, 25f, kind: Kind.DirForward, stopAtWall: true);
sealed class SwiftAsShadow(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.SwiftAsShadow, 1f);
sealed class Extinguishment(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ExtinguishmentVisual, new AOEShapeDonut(10f, 30f));
sealed class TheEdgeUnbound2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TheEdgeUnbound2, 10f);

sealed class UnmovingDvenadkatik : Components.SimpleAOEs
{
    public UnmovingDvenadkatik(ModuleBase module) : base(module, (uint)AID.UnmovingDvenadkatikVisual, new AOEShapeCone(50f, 15f.Degrees()), 6) { MaxDangerColor = 2; }
}

[ModuleInfo(Group = ModuleGroup.Quest, GroupID = 70000u, CFCID = 70000u, NameID = 10393u, PrimaryActorOID = (uint)OID.ZenosP1, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "croizat, Malediktus (ported from BMR)")]
public sealed class Endwalker(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsSquare(19.5f))
{
    public Actor? ZenosP2;

    protected override void UpdateModule()
    {
        ZenosP2 ??= GetActor((uint)OID.ZenosP2);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actor(ZenosP2);
    }
}
