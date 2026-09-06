// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

using Minerva.Endwalker.Trial.T02Hydaelyn;

namespace Minerva.Endwalker.Extreme.Ex2Hydaelyn;

class HerosSundering(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.HerosSundering, new AOEShapeCone(40f, 45f.Degrees()));
class Aureole(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.LateralAureole1AOE, (uint)AID.LateralAureole2AOE,
(uint)AID.Aureole1AOE, (uint)AID.Aureole2AOE], new AOEShapeCone(40f, 75f.Degrees()));
class MousaScorn(ModuleBase module) : Components.CastSharedTankbuster(module, (uint)AID.MousaScorn, 4f);

// cast counter for pre-intermission AOE
class PureCrystal(ModuleBase module) : Components.CastCounter(module, (uint)AID.PureCrystal);

// cast counter for post-intermission AOE
class Exodus(ModuleBase module) : Components.CastCounter(module, (uint)AID.Exodus);

[ModuleInfo(CFCID = 791u, NameID = 10453u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Ex2Hydaelyn(WorldState ws, Actor primary) : ModuleBase(ws, primary, T02Hydaelyn.ArenaCenter, T02Hydaelyn.ArenaBounds);
