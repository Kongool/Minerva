// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A31Thaliak;

sealed class Katarraktes(ModuleBase module) : Components.CastCounter(module, (uint)AID.KatarraktesAOE);
sealed class Thlipsis(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.ThlipsisAOE, 6f, 8);
sealed class Hydroptosis(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.HydroptosisAOE, 6f);
sealed class Rhyton(ModuleBase module) : Components.BaitAwayIcon(module, new AOEShapeRect(70f, 3f), (uint)IconID.Rhyton, (uint)AID.RhytonAOE, 6f);
sealed class Bank(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.LeftBank, (uint)AID.RightBank, (uint)AID.HieroglyphikaLeftBank,
(uint)AID.HieroglyphikaRightBank], new AOEShapeCone(60f, 90f.Degrees()));

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 962u, CFCID = 962u, NameID = 11298u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus, LTS (ported from BMR)")]
public sealed class A31Thaliak(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-945f, 945f), new ArenaBoundsSquare(24f));
