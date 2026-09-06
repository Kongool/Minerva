// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A21Nophica;

sealed class ArenaBounds(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeDonut donut = new(28f, 34f);
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x39)
        {
            switch (state)
            {
                case 0x02000200u:
                    _aoe = [new(donut, Center, default, World.FutureTime(5.8d))];
                    break;
                case 0x00200010u:
                case 0x00020001u:
                    Bounds = A21Nophica.SmallerBounds;
                    _aoe = [];
                    break;
                case 0x00080004u:
                case 0x00400004u:
                    Bounds = A21Nophica.DefaultBounds;
                    break;
            }
        }
    }
}

sealed class FloralHaze(ModuleBase module) : Components.StatusDrivenForcedMarch(module, 2, (uint)SID.ForwardMarch, (uint)SID.AboutFace, (uint)SID.LeftFace, (uint)SID.RightFace, activationLimit: 8);
sealed class SummerShade(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SummerShade, new AOEShapeDonut(12f, 40f));
sealed class SpringFlowers(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SpringFlowers, 12f);
sealed class ReapersGale(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ReapersGaleAOE, new AOEShapeRect(72f, 4f), 9);
sealed class Landwaker(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LandwakerAOE, 10f);
sealed class Furrow(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.Furrow, 6f, 8);
sealed class HeavensEarth(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.HeavensEarthAOE, 5f);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 911u, CFCID = 911u, NameID = 12065u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public sealed class A21Nophica(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(default, -238f), DefaultBounds)
{
    public static readonly ArenaBoundsCircle DefaultBounds = new(34f);
    public static readonly ArenaBoundsCircle SmallerBounds = new(28f);
}