// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P8S1Hephaistos;

class VolcanicTorches(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TorchFlame, new AOEShapeRect(10, 5));
class AbyssalFires(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AbyssalFires, 15); // TODO: verify falloff

[ModuleInfo(CFCID = 884u, NameID = 11399u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class P8S1(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100, 100), new ArenaBoundsSquare(20));
