// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerBlood.FTB4Magitaur;

sealed class UnsealedAura(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.UnsealedAura);

[ModuleInfo(Group = ModuleGroup.TheForkedTowerBlood, GroupID = 1018u, CFCID = 1018u, NameID = 13947u, PrimaryActorOID = (uint)OID.Magitaur, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class FTB4Magitaur : ModuleBase
{
    public FTB4Magitaur(WorldState ws, Actor primary) : this(ws, primary, BuildArena()) { }

    private FTB4Magitaur(WorldState ws, Actor primary, (WPos center, ArenaBoundsCustom arena) a) : base(ws, primary, a.center, a.arena, true) { }

    public static (WPos center, ArenaBoundsCustom arena) BuildArena()
    {
        var arena = new ArenaBoundsCustom([new Polygon(new(700f, -674f), 31.5f, 90)], [new Rectangle(new(700f, -705.916f), 7.5f, 1.25f), new Rectangle(new(700f, -641.5f), 7.5f, 1.25f)]);
        return (arena.Center, arena);
    }

    public static WPos[] GetSquarePositions() => [new(700f, -659.504f), new(712.554f, -681.248f), new(687.443f, -681.25f)]; // starting in south, ccw order
    public static Angle[] GetSquareAngles() => [-45f.Degrees(), -15f.Degrees(), 105f.Degrees()];
    public static WDir[] GetSquareAnglesDirs() => [-45f.Degrees().ToDirection(), -15f.Degrees().ToDirection(), 105f.Degrees().ToDirection()];
    public static Square[] GetSquares() => [new Square(new(700f, -659.504f), 10f, -45f.Degrees()), new Square(new(712.554f, -681.248f), 10f, -15f.Degrees()),
    new Square(new(687.443f, -681.25f), 10f, 105f.Degrees())];
    public static AOEShapeCustom GetCircleMinusSquares(WPos center) => new(center, [new Square(new(700f, -674f), 31.5f)], GetSquares());
}
