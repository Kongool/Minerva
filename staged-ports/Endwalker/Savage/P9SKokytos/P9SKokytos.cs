// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P9SKokytos;

class GluttonysAugur(ModuleBase module) : Components.CastCounter(module, (uint)AID.GluttonysAugurAOE);
class SoulSurge(ModuleBase module) : Components.CastCounter(module, (uint)AID.SoulSurge);
class BeastlyFury(ModuleBase module) : Components.CastCounter(module, (uint)AID.BeastlyFuryAOE);

[ModuleInfo(CFCID = 937u, NameID = 12369u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class P9SKokytos(WorldState ws, Actor primary) : ModuleBase(ws, primary, center, arena)
{
    public static readonly WPos center = new(100, 100);
    private const int rectWidth = 8;
    private const int rectHeight = 2;

    private static readonly Circle[] union = [new(center, 20)];
    private static readonly Rectangle[] difference0 = [new(new(100, 119.5f), rectWidth, rectHeight), new(new(80.5f, 100), rectHeight, rectWidth),
    new(new(119.5f, 100), rectHeight, rectWidth), new(new(100, 80.5f), rectWidth, rectHeight)];
    private static readonly Rectangle[] difference45 = [RotatedRectangle(new WPos(100, 119.5f), -45.Degrees()),
    RotatedRectangle(new WPos(80.5f, 100), -135.Degrees()), RotatedRectangle(new WPos(119.5f, 100), 45.Degrees()),
    RotatedRectangle(new WPos(100, 80.5f), 135.Degrees())];
    public static readonly ArenaBounds arena = new ArenaBoundsCustom(union);
    public static readonly ArenaBounds arenaUplift0 = new ArenaBoundsCustom(union, difference0);
    public static readonly ArenaBounds arenaUplift45 = new ArenaBoundsCustom(union, difference45);
    private static Rectangle RotatedRectangle(WPos position, Angle rotation)
    {
        var rotatedPosition = WPos.RotateAroundOrigin(45, center, position);
        return new(rotatedPosition, rectWidth, rectHeight, rotation);
    }
}
