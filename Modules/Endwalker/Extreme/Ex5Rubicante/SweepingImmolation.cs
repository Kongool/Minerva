// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex5Rubicante;

class SweepingImmolation(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.SweepingImmolationSpread, (uint)AID.SweepingImmolationStack], new AOEShapeCone(20f, 90f.Degrees()));
class PartialTotalImmolation(ModuleBase module) : Components.CastStackSpread(module, (uint)AID.TotalImmolation, (uint)AID.PartialImmolation, 6f, 5f, 8, 8, true);
class ScaldingSignal(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ScaldingSignal, 10f);
class ScaldingRing(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ScaldingRing, new AOEShapeDonut(10f, 20f));
class ScaldingFleetFirst(ModuleBase module) : Components.BaitAwayEveryone(module, module.PrimaryActor, new AOEShapeRect(40f, 3f), (uint)AID.ScaldingFleetFirst);

// note: it seems to have incorrect target, but acts like self-targeted
class ScaldingFleetSecond(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ScaldingFleetSecond, new AOEShapeRect(60f, 3f));
