// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P4S1Hesperos;

// state related to elegant evisceration mechanic (dual hit tankbuster)
// TODO: consider showing some tank swap / invul hint...
public class ElegantEvisceration(ModuleBase module) : Components.CastCounter(module, (uint)AID.ElegantEviscerationSecond);

[ModuleInfo(CFCID = 801u, NameID = 10744u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class P4S1(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100, 100), new ArenaBoundsSquare(20));
