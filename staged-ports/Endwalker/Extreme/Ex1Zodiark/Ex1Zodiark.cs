// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex1Zodiark;

// simple component tracking raidwide cast at the end of intermission
public class Apomnemoneumata(ModuleBase module) : Components.CastCounter(module, (uint)AID.ApomnemoneumataNormal);

public class Phlegethon(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PhlegetonAOE, 5);

[ModuleInfo(CFCID = 803u, NameID = 10456u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Ex1Zodiark(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100, 100), new ArenaBoundsSquare(20));
