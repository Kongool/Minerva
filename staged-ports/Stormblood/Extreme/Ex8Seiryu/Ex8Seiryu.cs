// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Extreme.Ex8Seiryu;

sealed class FifthElement(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.FifthElement);
sealed class SerpentDescendingSpread(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.SerpentDescending, (uint)AID.SerpentDescendingSpread, 5f, 6.1d);
sealed class SerpentsDescendingAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SerpentDescendingAOE, 5f);
sealed class FortuneCalamityBladeSigil(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.FortuneBladeSigil, (uint)AID.CalamityBladeSigil], new AOEShapeRect(50.5f, 2f), 9, 18);
sealed class Handprint(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Handprint1, new AOEShapeCone(40f, 90f.Degrees()));

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 638u, CFCID = 638u, NameID = 7922u, PrimaryActorOID = (uint)OID.Seiryu, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class Ex8Seiryu(WorldState ws, Actor primary) : Trial.T09Seiryu.Seiryu(ws, primary);
