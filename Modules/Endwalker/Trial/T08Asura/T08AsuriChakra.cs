// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Trial.T08Asura;

class AsuriChakra(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AsuriChakra, 5f);
class Chakra1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Chakra1, new AOEShapeDonut(6f, 8f));
class Chakra2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Chakra2, new AOEShapeDonut(9f, 11f));
class Chakra3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Chakra3, new AOEShapeDonut(12f, 14f));
class Chakra4(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Chakra4, new AOEShapeDonut(15f, 17f));
class Chakra5(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Chakra4, new AOEShapeDonut(18f, 20f));
