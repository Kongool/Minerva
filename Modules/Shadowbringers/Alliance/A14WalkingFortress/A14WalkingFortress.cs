// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

// namespace Minerva.Shadowbringers.Alliance.A14WalkingFortress;

// [ModuleInfo(CFCID = 700u, NameID = 9153u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
// public class A14WalkingFortress(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(900, 425), new ArenaBoundsCircle(30));