// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A11Byregot;

sealed class ByregotWard(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.ByregotWard, new AOEShapeCone(10f, 45f.Degrees()));

[ModuleInfo(CFCID = 866u, NameID = 11281u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class A11Byregot(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(0f, 700f), new ArenaBoundsSquare(24.5f));
