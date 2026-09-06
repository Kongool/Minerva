// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M12S2Lindwurm;

sealed class ArcadiaAflame(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ArcadiaAflame);
sealed class IdyllicDreamRaidwide(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.IdyllicDream);
sealed class LindwurmsMeteor(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.LindwurmsMeteor);
sealed class ArcadianHell5x(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ArcadianHell4x);
sealed class ArcadianHell9x(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ArcadianHell8x);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1075u, CFCID = 1075u, NameID = 14379u, PrimaryActorOID = (uint)OID.Boss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "BossMod Team, ported by Topas (ported from BMR)")]
public sealed class M12S2TheLindwurm : ModuleBase
{
    public M12S2TheLindwurm(WorldState ws, Actor primary) : this(ws, primary, BuildArena()) { }

    private M12S2TheLindwurm(WorldState ws, Actor primary, (WPos center, ArenaBoundsCustom arena) a) : base(ws, primary, a.center, a.arena) { }

    public static (WPos center, ArenaBoundsCustom arena) BuildArena()
    {
        var arena = new ArenaBoundsCustom([new Polygon(new(100f, 100f), 20f, 60)]);
        return (arena.Center, arena);
    }
}
