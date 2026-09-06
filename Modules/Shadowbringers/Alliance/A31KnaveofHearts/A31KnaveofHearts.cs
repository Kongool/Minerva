// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A31KnaveofHearts;

sealed class Roar(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Roar);

sealed class ColossalImpact(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.ColossalImpact1, (uint)AID.ColossalImpact2, (uint)AID.ColossalImpact3,
(uint)AID.ColossalImpact4, (uint)AID.ColossalImpact5, (uint)AID.ColossalImpact6], new AOEShapeRect(61f, 10f))
{
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = Casters.Count;
        if (count == 0)
        {
            return [];
        }
        var aoes = CollectionsMarshal.AsSpan(Casters);
        ref readonly var aoe0 = ref aoes[0];
        var rot = aoe0.Rotation + 180f.Degrees();

        var index = 0;
        while (index < count)
        {
            ref var aoe = ref aoes[index];
            if (aoe.Rotation.AlmostEqual(rot, Angle.DegToRad))
            {
                break;
            }
            ++index;
        }
        return aoes[..index];
    }
}

sealed class MagicArtilleryBeta(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.MagicArtilleryBeta, 3f, tankbuster: true);
sealed class MagicArtilleryAlpha(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.MagicArtilleryAlpha, 5f);
sealed class LightLeap(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LightLeap, 28f);
sealed class MagicBarrage(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MagicBarrage, new AOEShapeRect(61, 2.5f), 6);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 779u, CFCID = 779u, NameID = 9955u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class A31KnaveofHearts(WorldState ws, Actor primary) : ModuleBase(ws, primary, arenaCenter, new ArenaBoundsSquare(30f))
{
    private static readonly WPos arenaCenter = new(-800f, -724.40625f);
    public static readonly Square[] BaseSquare = [new Square(arenaCenter, 30.5f)];
    public static readonly ArenaBoundsSquare DefaultArena = new(30f);
}
