// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex5Rubicante;

class GreaterFlamesent(ModuleBase module) : Components.Adds(module, (uint)OID.GreaterFlamesent);
class FlamesentNS(ModuleBase module) : Components.Adds(module, (uint)OID.FlamesentNS);
class FlamesentSS(ModuleBase module) : Components.Adds(module, (uint)OID.FlamesentSS);
class FlamesentNC(ModuleBase module) : Components.Adds(module, (uint)OID.FlamesentNC);
class GhastlyTorch(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.GhastlyTorch);
class ShatteringHeatAdd(ModuleBase module) : Components.TankbusterTether(module, (uint)AID.ShatteringHeatAdd, (uint)TetherID.ShatteringHeatAdd, 3f);
class GhastlyWind(ModuleBase module) : Components.BaitAwayTethers(module, new AOEShapeCone(40f, 15f.Degrees()), (uint)TetherID.GhastlyWind, (uint)AID.GhastlyWind); // TODO: verify angle
class GhastlyFlame(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GhastlyFlameAOE, 5f);
