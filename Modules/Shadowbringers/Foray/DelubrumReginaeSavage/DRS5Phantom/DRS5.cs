// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS5Phantom;

sealed class MaledictionOfAgony(ModuleBase module) : Components.CastCounter(module, (uint)AID.MaledictionOfAgonyAOE);
sealed class BloodyWraith(ModuleBase module) : Components.Adds(module, (uint)OID.BloodyWraith);
sealed class MistyWraith(ModuleBase module) : Components.Adds(module, (uint)OID.MistyWraith);

[ModuleInfo(CFCID = 761u, NameID = 9755u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class DRS5Phantom(WorldState ws, Actor primary) : Phantom(ws, primary);
