// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A23Halone;

sealed class RainOfSpearsFirst(ModuleBase module) : Components.CastCounter(module, (uint)AID.RainOfSpearsFirst);
sealed class RainOfSpearsRest(ModuleBase module) : Components.CastCounter(module, (uint)AID.RainOfSpearsRest);
sealed class SpearsThree(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.SpearsThreeAOE, 5f);
sealed class WrathOfHalone(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WrathOfHaloneAOE, 25f); // TODO: verify falloff
sealed class GlacialSpearSmall(ModuleBase module) : Components.Adds(module, (uint)OID.GlacialSpearSmall);
sealed class GlacialSpearLarge(ModuleBase module) : Components.Adds(module, (uint)OID.GlacialSpearLarge);
sealed class IceDart(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.IceDart, 6f);
sealed class IceRondel(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.IceRondel, 6f, 8, 8);
sealed class Niphas(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Niphas, 9f);
sealed class FurysAegis(ModuleBase module) : Components.CastCounterMulti(module, [(uint)AID.Shockwave, (uint)AID.FurysAegisAOE1,
(uint)AID.FurysAegisAOE2, (uint)AID.FurysAegisAOE3, (uint)AID.FurysAegisAOE4, (uint)AID.FurysAegisAOE5,
(uint)AID.FurysAegisAOE6]);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 911u, CFCID = 911u, NameID = 12064u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class A23Halone(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-700f, 600f), new ArenaBoundsCircle(29.5f));
