// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Unreal.Un4Zurvan;

class P2ExecratedWill(ModuleBase module) : Components.Adds(module, (uint)OID.ExecratedWill); // hard-hitting add
class P2ExecratedWit(ModuleBase module) : Components.Adds(module, (uint)OID.ExecratedWit); // high-priority add (casts comets and meteor)
class P2ExecratedWile(ModuleBase module) : Components.Adds(module, (uint)OID.ExecratedWile); // low-priority add (casts fear, then magical autos)
class P2ExecratedThew(ModuleBase module) : Components.Adds(module, (uint)OID.ExecratedThew); // small add

class P2Comet(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Comet, 4);
class P2MeracydianFear(ModuleBase module) : Components.CastGaze(module, (uint)AID.MeracydianFear);
