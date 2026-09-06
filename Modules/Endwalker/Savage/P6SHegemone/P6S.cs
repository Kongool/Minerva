// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P6SHegemone;

class UnholyDarkness(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.UnholyDarknessAOE, 6, 8, 8);
class DarkDome(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DarkDomeAOE, 5);
class DarkAshes(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.DarkAshesAOE, 6);
class DarkSphere(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.DarkSphereAOE, 10);

[ModuleInfo(CFCID = 881u, NameID = 11381u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class P6S(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100, 100), new ArenaBoundsSquare(20));
