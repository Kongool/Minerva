// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerMagic.Extreme.FTME3Necrophobia;

public enum OID : uint
{
    Necrophobia = 0x4BE7,
    Helper = 0x233C,
    _Gen_HiddenTrap = 0x4D28, // R1.000, x1
    _Gen_SeveringHead = 0x4BE8, // R1.410, x8
    _Gen_Necrophobia = 0x4BE9, // R1.000, x1
    _Gen_Actor1e8fb8 = 0x1E8FB8, // R2.000, x2, EventObj type
    _Gen_Actor1e8f2f = 0x1E8F2F, // R0.500, x1, EventObj type
    _Gen_Actor1ebfaa = 0x1EBFAA, // R0.500, x0 (spawn during fight), EventObj type
    _Gen_Actor1ea1a1 = 0x1EA1A1, // R2.000, x0 (spawn during fight), EventObj type
}

public enum AID : uint
{
    _AutoAttack_ = 47481, // Necrophobia->player, no cast, single-target
    _Spell_HailOfHellflares = 47482, // Necrophobia->self, 5.0s cast, single-target
    _Spell_HailOfHellflares1 = 47483, // Helper->self, no cast, range 60 ???
    _Spell_HailOfHellflares2 = 48958, // Helper->self, no cast, single-target
    _Spell_HailOfHellflares3 = 48959, // Helper->self, no cast, range 60 ???
    _Ability_ = 47484, // 4BE9->self, no cast, range ?-30 donut
    _Ability_1 = 47480, // Necrophobia->location, no cast, single-target
    _Ability_Capitation = 47485, // Necrophobia->self, no cast, single-target
    _Ability_2 = 47487, // 4BE8->location, no cast, single-target
    _Ability_DeathShroud = 47486, // Necrophobia->self, 7.0s cast, single-target
    _Ability_HeadsRoll = 47488, // Necrophobia->self, 3.0s cast, single-target
    _Ability_3 = 47489, // 4BE8->location, no cast, single-target
    _Spell_AncientFireIII = 47494, // 4BE8->self, 5.0s cast, range 18 circle
    _Spell_SeveredFireIII = 47490, // Necrophobia->self, 5.0s cast, range 18 circle
    _Spell_AncientBlizzardIII = 47495, // 4BE8->self, 5.0s cast, range 45 width 15 cross
    _Spell_SeveredBlizzardIII = 47491, // Necrophobia->self, 5.0s cast, range 45 width 15 cross
    _Spell_AncientThunderIII = 47496, // 4BE8->self, 4.2+0.8s cast, single-target
    _Spell_SeveredThunderIII = 47492, // Necrophobia->self, 4.2+0.8s cast, single-target
    _Spell_AncientThunderIII1 = 47497, // Helper->self, 5.0s cast, range 60 45.000-degree cone
    _Spell_SeveredThunderIII1 = 50358, // Helper->self, 5.0s cast, range 60 45.000-degree cone
    _Ability_4 = 47498, // 4BE8->location, no cast, single-target
    _Ability_HeadsRoll1 = 47503, // Necrophobia->self, no cast, single-target
    _Weaponskill_DarkCurrent = 47499, // Necrophobia->self, 2.7+1.3s cast, single-target
    _Ability_DeathlyRay = 47504, // 4BE8->self, 4.0s cast, range 30 width 6 rect
    _Weaponskill_DarkCurrent1 = 47500, // Helper->self, 4.0s cast, range 60 width 10 rect
    _Weaponskill_DarkCurrent2 = 47501, // Helper->self, 1.0s cast, range 10 width 60 rect
    _Weaponskill_VacuumWave = 47502, // Necrophobia->self, 4.0s cast, range 30 180.000-degree cone
    _Weaponskill_CorpseMangler = 47505, // Necrophobia->player, 5.0s cast, single-target
    _Ability_DigThreeGraves = 47506, // Necrophobia->self, 3.0s cast, single-target
    _Weaponskill_SeveredDarkCurrent = 47507, // Necrophobia->self, 8.7+1.3s cast, single-target
    _Weaponskill_DarkCurrent3 = 47508, // Necrophobia->self, no cast, single-target
    _Spell_AncientThunderIII2 = 47512, // 4BE8->self, 0.7+0.8s cast, single-target
    _Spell_AncientThunderIII3 = 47513, // Helper->self, 1.5s cast, range 60 45.000-degree cone
    _Weaponskill_DarkCurrent4 = 47509, // Helper->self, 1.5s cast, range 60 width 10 rect
    _Spell_AncientFireIII1 = 47510, // 4BE8->self, 1.5s cast, range 18 circle
    _Spell_AncientBlizzardIII1 = 47511, // 4BE8->self, 1.5s cast, range 45 width 15 cross
    _Spell_FertileGround = 47514, // Necrophobia->self, 5.0s cast, single-target
    _Spell_FertileGround1 = 48960, // Helper->self, no cast, range 60 ???
    _Spell_SpellProcession = 47515, // Necrophobia->self, 5.0s cast, single-target
    _Spell_SowingFear = 47574, // 4BE8->self, no cast, single-target
    _Spell_SowingPanic = 47520, // Helper->self, no cast, range 80 width 30 rect
    _Spell_SowingDread = 47519, // Helper->self, no cast, range 80 width 30 rect
    _Spell_SowingFear1 = 47516, // 4BE8->self, no cast, single-target
    _Spell_SowingDread1 = 47517, // Helper->self, no cast, range 80 width 30 rect
    _Spell_SowingPanic1 = 47518, // Helper->self, no cast, range 80 width 30 rect
    _Spell_AncientFireIII2 = 47521, // Necrophobia->self, 5.0s cast, range 18 circle
    _Spell_AncientThunderIII4 = 47523, // Necrophobia->self, 4.2+0.8s cast, single-target
    _Spell_AncientThunderIII5 = 47493, // Helper->self, 5.0s cast, range 60 45.000-degree cone
    _Spell_AncientBlizzardIII2 = 47522, // Necrophobia->self, 5.0s cast, range 45 width 15 cross
    _Spell_Nihility = 47524, // Necrophobia->self, 10.0s cast, single-target
    _Spell_Nihility1 = 48961, // Helper->self, no cast, range 60 ???
    _Spell_Nihility2 = 47525, // Necrophobia->self, 2.0s cast, range 60 circle
}

public enum SID : uint
{
    _Gen_Unk = 4956, // none->4BE8, extra=0x2C4
    _Gen_UnkExtra = 2552, // none->Necrophobia/4BE8, extra=0x45A/0x45B/0x45C/0x45E/0x45D
    _Gen_ThriceComeRuin = 3478, // Necrophobia/4BE8/Helper->player, extra=0x1/0x2
    _Gen_Doom = 2519, // Necrophobia/Helper/4BE8->player, extra=0x0
    _Gen_DigThreeGraves = 5135, // none->Necrophobia, extra=0x0
    _Gen_GrowingDread = 5136, // Helper->player, extra=0x0
    _Gen_GrowingPanic = 5137, // Helper->player, extra=0x0
}

public enum IconID : uint
{
    _Gen_Icon_tank_lockon02k1 = 218, // player->self
}

public enum TetherID : uint
{
    _Gen_Tether_chn_m0475_mr_c1x = 401, // 4BE8->Necrophobia
    _Gen_Tether_chn_m0475_mr_c2x = 402, // 4BE8->Necrophobia
    _Gen_Tether_chn_m0475_mr_c0x = 400, // 4BE8->Necrophobia
    _Gen_Tether_chn_m0475_mr_c3x = 403, // 4BE8->Necrophobia
}
