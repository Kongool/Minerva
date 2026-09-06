// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M11STheTyrant;

sealed class CrownOfArcadia(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.CrownOfArcadia);
sealed class UltimateTrophyWeapons(ModuleBase module) : Components.CastHint(module, (uint)AID.UltimateTrophyWeapons, "Ultimate Trophy Weapons");

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1073u, CFCID = 1073u, NameID = 14305u, PrimaryActorOID = (uint)OID.Boss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Topas (ported from BMR)")]

public sealed class M11STheTyrant(WorldState ws, Actor primary) : ModuleBase(ws, primary, ArenaChanges.ArenaCenter, ArenaChanges.InitialBounds);