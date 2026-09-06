// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P7SAgdistis;

[SkipLocalsInit]
sealed class HemitheosHoly(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.HemitheosHolyAOE, 6f, 4, 4);
[SkipLocalsInit]
sealed class BoughOfAttisBack(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BoughOfAttisBackAOE, 25f);
[SkipLocalsInit]
sealed class BoughOfAttisFront(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BoughOfAttisFrontAOE, 19f);
[SkipLocalsInit]
sealed class BoughOfAttisSide(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BoughOfAttisSideAOE, new AOEShapeRect(50f, 12.5f));
[SkipLocalsInit]
sealed class HemitheosAeroKnockback1(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.HemitheosAeroKnockback1, 16f); // TODO: verify distance...
[SkipLocalsInit]
sealed class HemitheosAeroKnockback2(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.HemitheosAeroKnockback2, 16f);
[SkipLocalsInit]
sealed class HemitheosHolySpread(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.HemitheosHolySpread, 6f);
[SkipLocalsInit]
sealed class HemitheosTornado(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HemitheosTornado, 25f);
[SkipLocalsInit]
sealed class HemitheosGlareMine(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HemitheosGlareMine, new AOEShapeDonut(5f, 30f)); // TODO: verify inner radius

[SkipLocalsInit]

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 877u, CFCID = 877u, NameID = 11374u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public sealed class P7S(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsCustom([new Polygon(new(100f, 100f), 20f, 128)]));
