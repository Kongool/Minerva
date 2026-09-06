// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P5SProtoCarbuncle;

class ToxicCrunch(ModuleBase module) : Components.CastCounter(module, (uint)AID.ToxicCrunchAOE); // TODO: improve component?
class DoubleRush(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.DoubleRush, 50);
class DoubleRushReturn(ModuleBase module) : Components.CastCounter(module, (uint)AID.DoubleRushReturn); // TODO: show knockback?
class SonicShatter(ModuleBase module) : Components.CastCounter(module, (uint)AID.SonicShatterRest);
class DevourBait(ModuleBase module) : Components.CastCounter(module, (uint)AID.DevourBait);

[ModuleInfo(CFCID = 873u, NameID = 11440u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class P5S(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100, 100), new ArenaBoundsSquare(15));
