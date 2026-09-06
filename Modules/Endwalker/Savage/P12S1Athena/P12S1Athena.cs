// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P12S1Athena;

sealed class RayOfLight(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RayOfLight, new AOEShapeRect(60f, 5f));
sealed class UltimaBlade(ModuleBase module) : Components.CastCounter(module, (uint)AID.UltimaBladeAOE);
sealed class Parthenos(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Parthenos, new AOEShapeRect(120f, 8f));

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 943u, CFCID = 943u, NameID = 12377u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public sealed class P12S1Athena(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsSquare(20f));
