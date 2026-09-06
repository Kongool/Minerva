// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Unreal.Un3Sophia;

class ThunderDonut(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThunderDonut, new AOEShapeDonut(5, 20));
class ExecuteDonut(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ExecuteDonut, new AOEShapeDonut(5, 20));
class Aero(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Aero, 10);
class ExecuteAero(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ExecuteAero, 10);
class ThunderCone(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThunderCone, new AOEShapeCone(20, 45.Degrees()));
class ExecuteCone(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ExecuteCone, new AOEShapeCone(20, 45.Degrees()));
class LightDewShort(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LightDewShort, new AOEShapeRect(55, 9));
class LightDewLong(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LightDewLong, new AOEShapeRect(55, 9));
class Onrush(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Onrush, new AOEShapeRect(55, 8));
class Gnosis(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.Gnosis, 25);
class Cintamani(ModuleBase module) : Components.CastCounter(module, (uint)AID.Cintamani); // note: ~4.2s before first cast boss gets model state 5
class QuasarProximity1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.QuasarProximity1, 15);
class QuasarProximity2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.QuasarProximity2, 15); // TODO: reconsider distance

[ModuleInfo(Group = ModuleGroup.RemovedUnreal, CFCID = 926u, NameID = 5199u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Un3Sophia(WorldState ws, Actor primary) : ModuleBase(ws, primary, default, new ArenaBoundsRect(20, 15));
