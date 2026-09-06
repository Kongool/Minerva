// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Trial.T04Zelenia;

sealed class PowerBreak(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.PowerBreak1, (uint)AID.PowerBreak2], new AOEShapeRect(24f, 32f));

sealed class HolyHazard(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HolyHazard, new AOEShapeCone(24f, 60f.Degrees()), 2);

sealed class RosebloodBloom(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.RosebloodBloom, 10f, true)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
        {
            ref readonly var c = ref Casters.Ref(0);
            hints.AddForbiddenZone(new SDInvertedCircle(c.Origin, 6f), c.Activation);
        }
    }
}

sealed class ThunderSlash : Components.SimpleAOEs
{
    public ThunderSlash(ModuleBase module) : base(module, (uint)AID.ThunderSlash, new AOEShapeCone(24f, 30f.Degrees()), 4)
    {
        MaxDangerColor = 2;
    }
}

sealed class PerfumedQuietus(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.RosebloodBloom); // using the knockback here, since after knockback player is stunned for a cutscene and can't heal up
sealed class ThornedCatharsis(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ThornedCatharsis);
sealed class SpecterOfTheLost(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.SpecterOfTheLost, new AOEShapeCone(50f, 22.5f.Degrees()), tankbuster: true);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1030u, CFCID = 1030u, NameID = 13861u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class T04Zelenia(WorldState ws, Actor primary) : ModuleBase(ws, primary, arenaCenter, DefaultArena)
{
    private static readonly WPos arenaCenter = new(100f, 100f);
    public static readonly ArenaBoundsCustom DefaultArena = new([new Polygon(arenaCenter, 16f, 64)]);
    public static readonly ArenaBoundsCustom DonutArena = new([new DonutV(arenaCenter, 4f, 16f, 64)]);
}
