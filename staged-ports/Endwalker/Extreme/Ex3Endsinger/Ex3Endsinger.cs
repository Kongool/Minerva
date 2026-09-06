// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex3Endsigner;

// raidwide is slightly delayed
class Elegeia(ModuleBase module) : Components.CastCounter(module, (uint)AID.Elegeia);

class Telomania(ModuleBase module) : Components.CastCounter(module, (uint)AID.TelomaniaLast);

class UltimateFate(ModuleBase module) : Components.CastCounter(module, (uint)AID.EnrageAOE);

// TODO: proper tankbuster component...
class Hubris(ModuleBase module) : Components.CastCounter(module, (uint)AID.HubrisAOE);

// TODO: proper stacks component
class Eironeia(ModuleBase module) : Components.CastCounter(module, (uint)AID.EironeiaAOE);

[ModuleInfo(CFCID = 846u, NameID = 10448u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Ex3Endsinger(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100, 100), new ArenaBoundsCircle(20));
