// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex7Doomtrain;

[SkipLocalsInit]
sealed class UnlimitedExpress(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.UnlimitedExpress);
[SkipLocalsInit]
sealed class ElectrayLong(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.Electray1, (uint)AID.Electray4], new AOEShapeRect(25f, 2.5f));
[SkipLocalsInit]
sealed class ElectrayMedium(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Electray2, new AOEShapeRect(20f, 2.5f));
[SkipLocalsInit]
sealed class ElectrayShort(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Electray3, new AOEShapeRect(5f, 2.5f));
[SkipLocalsInit]
sealed class LightningBurst(ModuleBase module) : Components.BaitAwayIcon(module, 5f, (uint)IconID.LightningBurst, (uint)AID.LightningBurst, 5.6f, tankbuster: true);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1077u, CFCID = 1077u, NameID = 14284u, PrimaryActorOID = (uint)OID.Doomtrain, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus, Xaenalt (ported from BMR)")]
[SkipLocalsInit]
public sealed class Ex7Doomtrain : ModuleBase
{
    public Ex7Doomtrain(WorldState ws, Actor primary) : this(ws, primary, BuildArena()) { }

    private Ex7Doomtrain(WorldState ws, Actor primary, (WPos center, ArenaBoundsCustom arena) a) : base(ws, primary, a.center, a.arena) { }

    private static (WPos center, ArenaBoundsCustom arena) BuildArena()
    {
        var arena = new ArenaBoundsCustom([new Rectangle(new(100f, 100f), 10.5f, 15f)],
        [new Square(new(102.5f, 97.5f), 2.01f), new Square(new(97.5f, 107.5f), 2.01f)], AdjustForHitboxInwards: true);
        return (arena.Center, arena);
    }
}
