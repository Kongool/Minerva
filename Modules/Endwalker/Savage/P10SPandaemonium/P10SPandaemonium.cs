// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P10SPandaemonium;

class DividingWings(ModuleBase module) : Components.BaitAwayTethers(module, new AOEShapeCone(60f, 60f.Degrees()), (uint)TetherID.DividingWings, (uint)AID.DividingWingsAOE);
class PandaemonsHoly(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PandaemonsHoly, 36f);

// note: origin seems to be weird?
class CirclesOfPandaemonium(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CirclesOfPandaemonium, new AOEShapeDonut(12f, 40f));

class Imprisonment(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ImprisonmentAOE, 4f);
class Cannonspawn(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CannonspawnAOE, new AOEShapeDonut(3f, 8f));
class PealOfDamnation(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PealOfDamnation, new AOEShapeRect(50f, 3.5f));
class PandaemoniacPillars(ModuleBase module) : Components.CastTowers(module, (uint)AID.Bury, 2f);
class Touchdown(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TouchdownAOE, 20f);

[ModuleInfo(CFCID = 939u, NameID = 12354u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class P10SPandaemonium(WorldState ws, Actor primary) : ModuleBase(ws, primary, DefaultArena.Center, DefaultArena)
{
    private static readonly Rectangle[] union = [new(new(100f, 100f), 13f, 15f), new(new(125f, 85f), 4f, 15f), new(new(75f, 85f), 4f, 15f)];
    private static readonly Rectangle[] bridgeL = [new(new(83f, 92.5f), 4f, 1f)];
    private static readonly Rectangle[] bridgeR = [new(new(117f, 92.5f), 4f, 1f)];
    public static readonly ArenaBoundsCustom DefaultArena = new(union);
    public static readonly ArenaBoundsCustom ArenaL = new([.. union, .. bridgeL]);
    public static readonly ArenaBoundsCustom ArenaR = new([.. union, .. bridgeR]);
    public static readonly ArenaBoundsCustom ArenaLR = new([.. union, .. bridgeL, .. bridgeR]);
}
