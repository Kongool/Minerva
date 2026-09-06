// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A22Glasya;

class Aura(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Aura, 8);
class VileUtterance(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.VileUtterance, new AOEShapeCone(30, 22.5f.Degrees()));

[ModuleInfo(CFCID = 102u, NameID = 2815u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A22Glasya(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(0, -200), new ArenaBoundsCircle(35));
