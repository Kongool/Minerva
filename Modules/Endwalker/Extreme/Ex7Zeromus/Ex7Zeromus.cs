// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex7Zeromus;

class AbyssalEchoes(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AbyssalEchoes, 12, 5);
class BigBangPuddle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BigBangAOE, 5);
class BigBangSpread(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.BigBangSpread, 5);
class BigCrunchPuddle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BigCrunchAOE, 5);
class BigCrunchSpread(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.BigCrunchSpread, 5);

[ModuleInfo(CFCID = 965u, NameID = 12586u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Ex7Zeromus(WorldState ws, Actor primary) : ModuleBase(ws, primary, ArenaCenter, new ArenaBoundsSquare(20))
{
    public static readonly WPos ArenaCenter = new(100, 100);
}
