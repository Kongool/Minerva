// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Dungeon.D17PharosSirius.D174Siren;

public enum OID : uint
{
    Siren = 0x8EF,
    Helper = 0x233C,
    ZombieStormPrivate = 0x8F0,
    ZombieStormSergeant = 0x8F1
}

public enum AID : uint
{
    AutoAttack1 = 1482, // Siren->player, no cast, range 7+R ?-degree cone
    AutoAttack2 = 872, // ZombieStormPrivate->player, no cast, single-target

    DeathlyVerse = 1483, // Siren->player, 1.0s cast, single-target
    SongOfTorment = 1486, // Siren->player, 1.5s cast, single-target

    DeathlyCadenza = 1487, // Siren->self, 3.0s cast, range 50+R circle
    FeralLunge = 1484, // Siren->self, 3.0s cast, range 50+R width 12 rect
    LunaticVoice = 1485, // Siren->self, 4.0s cast, range 50+R circle
    Wallop = 1658, // ZombieStormPrivate->self, 2.5s cast, range 3+R width 3 rect
    DeathThroes = 1539, // ZombieStormSergeant->player, no cast, single-target
    Zombify = 1675, // 1B2->player, no cast, single-target : Maybe this is when player receives confused status?
}

public enum SID : uint
{
    SirenSong = 370, // Siren->player, extra=0x0
    Confused = 11, // 1B2->player, extra=0x0
    Bleeding = 273, // Siren->player, extra=0x0
    DeathThroes = 378, // 8F1->player, extra=0x0
}

// Cleave angle is an estimate.
sealed class AutoCleave(ModuleBase module)
    : Components.Cleave(module, (uint)AID.AutoAttack1, new AOEShapeCone(7f, 60f.Degrees()), [(uint)OID.Siren]);

// Donut aoe. Stand in center to to avoid 'siren song' debuff.
sealed class DeathlyCadenza(ModuleBase module)
    : Components.SimpleAOEs(module, (uint)AID.DeathlyCadenza, new AOEShapeDonut(4f, 30f));

sealed class FeralLunge(ModuleBase module)
    : Components.SimpleAOEs(module, (uint)AID.FeralLunge, new AOEShapeRect(50f, 6f));

sealed class DeathlyVerse(ModuleBase module) : Components.CastHint(module, (uint)AID.DeathlyVerse,
    "Heal Siren Song Status to full or player will get confuse status.");

sealed class SongOfTorment(ModuleBase module)
    : Components.CastHint(module, (uint)AID.SongOfTorment, "Esuna bleeding debuff from tank");

sealed class LunaticVoice(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.LunaticVoice,
    "Reduced Immunity status can be removed with Esuna");

sealed class ZombiePrivate(ModuleBase module) : Components.Adds(module, (uint)OID.ZombieStormPrivate);

sealed class Wallop(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Wallop, new AOEShapeRect(3f, 1.5f));

sealed class ZombieSergeant(ModuleBase module) : Components.Adds(module, (uint)OID.ZombieStormSergeant);

[SkipLocalsInit]
sealed class D174SirenStates : StateMachineBuilder
{
    public D174SirenStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<AutoCleave>()
            .ActivateOnEnter<DeathlyCadenza>()
            .ActivateOnEnter<FeralLunge>()
            .ActivateOnEnter<DeathlyVerse>()
            .ActivateOnEnter<SongOfTorment>()
            .ActivateOnEnter<LunaticVoice>()
            .ActivateOnEnter<ZombiePrivate>()
            .ActivateOnEnter<Wallop>()
            .ActivateOnEnter<ZombieSergeant>();
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 17u, CFCID = 17u, NameID = 2265u, PrimaryActorOID = (uint)OID.Siren, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "wen (ported from BMR)")]
[SkipLocalsInit]
public sealed class D174Siren : ModuleBase
{
    public D174Siren(WorldState ws, Actor primary) : this(ws, primary, BuildArena()) { }

    // Constructor so we can build arena
    private D174Siren(WorldState ws, Actor primary, (WPos center, ArenaBoundsCustom arena) a) : base(ws, primary, a.center, a.arena) { }

    public static readonly WPos ArenaCenter = new(0f, 0f);

    private static (WPos center, ArenaBoundsCustom arena) BuildArena()
    {
        List<Shape> unionShapes =
        [
            new Circle(new WPos(0.00f, 0.00f), 25.00f),
            new Rectangle(new WPos(24.10f, -0.00f), 0.75f, 5.00f, 0.0f.Degrees())
        ];
        List<Shape> diffShapes =
        [
            new Rectangle(new WPos(17.54f, -17.54f), 1.20f, 0.80f, 134.220f.Degrees()),
            new Rectangle(new WPos(6.42f, -23.95f), 1.20f, 0.80f, 166.600f.Degrees()),
            new Rectangle(new WPos(-6.42f, -23.95f), 1.20f, 0.80f, -164.510f.Degrees()),
            new Rectangle(new WPos(-17.54f, -17.54f), 1.20f, 0.80f, -134.220f.Degrees()),
            new Rectangle(new WPos(-23.95f, -6.42f), 1.20f, 0.80f, -104.970f.Degrees()),
            new Rectangle(new WPos(-23.95f, 6.42f), 1.20f, 0.80f, -75.030f.Degrees()),
            new Rectangle(new WPos(-17.54f, 17.54f), 1.20f, 0.80f, -45.090f.Degrees()),
            new Rectangle(new WPos(-6.42f, 23.95f), 1.20f, 0.80f, -15.750f.Degrees()),
            new Rectangle(new WPos(6.42f, 23.95f), 1.20f, 0.80f, 15.000f.Degrees()),
            new Rectangle(new WPos(17.54f, 17.54f), 1.20f, 0.80f, 45.640f.Degrees()),
            new Rectangle(new WPos(25.38f, -5.63f), 0.75f, 0.75f, 0.000f.Degrees()),
            new Rectangle(new WPos(25.38f, 5.63f), 0.75f, 0.75f, 0.000f.Degrees())
        ];

        ArenaBoundsCustom arena = new([..unionShapes],
        [..diffShapes]);

        return (ArenaCenter, arena);
    }
}
