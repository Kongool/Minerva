// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRN6Queen;

sealed class NorthswainsGlowPawnOff(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.NorthswainsGlowAOE, (uint)AID.PawnOffReal], 20f);
sealed class GodsSaveTheQueen(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.GodsSaveTheQueen);
sealed class CleansingSlash(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.CleansingSlash, "Tankbuster with doom");
sealed class JudgmentBlade(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.JudgmentBladeL, (uint)AID.JudgmentBladeR], new AOEShapeRect(70f, 15f));
sealed class OptimalPlaySword(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.OptimalPlaySword, 10f);
sealed class OptimalPlayShield(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.OptimalPlayShield, new AOEShapeDonut(5f, 60f));
sealed class Doom(ModuleBase module) : Components.CleansableDebuff(module, (uint)SID.Doom);

[ModuleInfo(CFCID = 760u, NameID = 9863u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class DRN6Queen(WorldState ws, Actor primary) : Queen(ws, primary);
