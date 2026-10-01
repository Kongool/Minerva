// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Quest.MSQ.ARequiemForHeroes;

public enum OID : uint
{
    BossP1 = 0x268A,
    BossP2 = 0x268C,

    AmeNoHabakiri = 0x2692, // R3.0
    TheStorm = 0x2760, // R3.0
    TheSwell = 0x275F, // R3.0
    DarkAether = 0x2694, // R1.2
    Helper = 0x233C
}

public enum AID : uint
{
    FloodOfDarkness = 14808, // Helper->self, 3.5s cast, range 6 circle
    VeinSplitter = 14839, // Boss->self, 4.0s cast, range 10 circle
    LightlessSpark = 14838, // Boss->self, 4.0s cast, range 40+R 90-degree cone
    LightlessSparkAdds = 14824, // 268D->self, 4.0s cast, range 40+R 90-degree cone
    ArtOfTheSwell = 14812, // Boss->self, 4.0s cast, range 33 circle
    TheSwellUnbound = 14813, // Helper->self, 8.0s cast, range 8-20 donut
    ArtOfTheSword1 = 14819, // Helper->self, 4.0s cast, range 40+R width 6 rect
    ArtOfTheSword2 = 14818, // Helper->self, 6.0s cast, range 40+R width 6 rect
    ArtOfTheSword3 = 14820, // Helper->self, 2.0s cast, range 40+R width 6 rect
    ArtOfTheStorm = 14814, // Boss->self, 4.0s cast, range 8 circle
    TheStormUnboundCast = 14815, // Helper->self, 3.0s cast, range 5 circle
    TheStormUnboundRepeat = 14816, // Helper->self, no cast, range 5 circle
    EntropicFlame = 14833, // Helper->self, 4.0s cast, range 50+R width 8 rect

    // from the recording (2026-09-30, Zenos P2) and the Action sheet; BossmodReborn's module has none of these
    TheSwordUnbound = 14821, // Helper->self, 5.7s cast, range 20 circle from the arena centre: the whole floor
    UnmovingTroikaFirst = 14829, // Boss->self, no cast, range 9+R 120-degree cone
    UnmovingTroikaSecond = 14830, // Helper->self, 1.4s cast, range 9+R 120-degree cone
    UnmovingTroikaLast = 14831, // Helper->self, 1.8s cast, range 9+R 120-degree cone
    Concentrativity = 14834, // Boss->self, range larger than the arena: raidwide

    // Phase 1, played as Hien: its own ids, none of which P2's components answer to. From the recording (2026-09-30,
    // newtoon2) and the Action sheet; BossmodReborn's P1 has none of them.
    ConcentrativityP1 = 14795, // BossP1->self, 3.7s cast, range 40 circle: raidwide
    ArtOfTheSwellP1 = 14796, // BossP1->self, 5.7s cast, range 33 circle, knockback
    UnmovingTroikaP1Second = 14792, // Helper->self, 1.4s cast, range 9+R 120-degree cone
    UnmovingTroikaP1Last = 14793, // Helper->self, 1.8s cast, range 9+R 120-degree cone
    VeinSplitterP1 = 14418, // BossP1->self, 5.7s cast, range 10 circle
    ThunderousForce = 14587, // 268F (The Storm)->self, 4.7s cast, range 8 circle
    ArtOfTheSwordP1A = 14857, // Helper->self at a Specter of Zenos, 37.7s cast, range 40 width 6 rect
    ArtOfTheSwordP1B = 14800, // Helper->self at a Specter of Zenos, 38.2s cast, range 40 width 6 rect
    DarknessP1 = 14805, // Helper->self, 2.7s cast, range 100 circle: raidwide
}
