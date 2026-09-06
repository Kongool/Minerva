// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Dungeon.D17PharosSirius.D172Zu;

public enum OID : uint
{
    Zu = 0x8F4, // R3.600, x?
    _Gen_ = 0x8FA, // R0.500, x? : Glowing egg / threatens to hatch?
    ZuEgg = 0x8F8, // R2.000, x? : Becomes ZuPullet.
    ZuEggSpotted = 0x8F7, // R2.000, x? : This becomes ZuCockerel if we want to kill before hatching.
    ZuPullet = 0x8F6, // R0.400, x?
    ZuCockerel = 0x8F5, // R0.400, x?
}

public enum AID : uint
{
    FrontalCleave = 1488, // Zu->player, no cast, range 9+R ?-degree cone
    SonicBoom = 1497, // Zu->player, no cast, single-target
    BreathWing = 1491, // Zu->self, 2.5s cast, range 50 circle
    Hatch = 1545, // ZuEgg/ZuEgg1->self, 15.0s cast, single-target
    AutoAttack_Attack1 = 1496, // ZuPullet/ZuCockerel->player, no cast, range 9+R ?-degree cone
    BreathWing1 = 1543, // ZuPullet->self, 2.5s cast, range 50 circle
    Climb = 1493, // Zu->self, no cast, single-target
    SonicStorm = 1494, // Zu->location, 1.2s cast, range 6 circle
    Alight = 1495, // Zu->self, no cast, single-target
    CausticVomit = 1490, // ZuCockerel->player, no cast, single-target
    BroodRage = 1544, // ZuEgg1/ZuEgg (eggs)->Zu, no cast, single-target : eggs give brood rage status to zu when killed.
}

public enum SID : uint
{
    Windburn = 235,
    Hover = 412, // Zu flies up out of reach and becomes untargetable
    BroodRage = 372, // Zu rages when more than one egg is killed.
}

public enum TetherID : uint
{
    HatchlingTether = 6, // ZuCockerel -> player
}

// Cleave range and angle are estimates
sealed class FrontalCleave(ModuleBase module) : Components.Cleave(module, (uint)AID.FrontalCleave, new AOEShapeCone(9f, 60f.Degrees()));

sealed class BreathWing(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.BreathWing, (uint)AID.BreathWing1]);

sealed class SonicStorm(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SonicStorm, 6f);

sealed class ZuAdds(ModuleBase module) : Components.AddsMulti(module, [(uint)OID.ZuCockerel, (uint)OID.ZuPullet], 1);

/*
 * There are eggs around outside edge of arena. If multiple eggs are killed the boss goes into a frenzy.
 * Set them as a danger zone to show they can get an at level party killed. We follow up with NoKillEggs
 * to also avoid targeting the eggs accidentally. This is best effort. Maybe voidzones need to be larger
 * to avoid AOE clipping the eggs.
 */
sealed class EggZone(ModuleBase module) : Components.Voidzone(module, 8, m => m.Enemies([(uint)OID.ZuEgg, (uint)OID.ZuEggSpotted]));

// The idea is not to target eggs directly or get near enough to hit them with aoe.
// TODO I would like to not show the enemy arrow indicator if they are not hatched.
sealed class NoKillEggs(ModuleBase module) : Components.AddsPointless(module, (uint)OID.ZuEgg)
{
    public override void DrawArenaForeground(int pcSlot, Actor pc) { }
}

sealed class NoKillEggs1(ModuleBase module) : Components.AddsPointless(module, (uint)OID.ZuEggSpotted)
{
    public override void DrawArenaForeground(int pcSlot, Actor pc) { }
}

[SkipLocalsInit]
sealed class D172ZuStates : StateMachineBuilder
{
    public D172ZuStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<BreathWing>()
            .ActivateOnEnter<SonicStorm>()
            .ActivateOnEnter<FrontalCleave>()
            .ActivateOnEnter<ZuAdds>()
            .ActivateOnEnter<EggZone>()
            .ActivateOnEnter<NoKillEggs>()
            .ActivateOnEnter<NoKillEggs1>()
            ;
    }
}

[ModuleInfo(CFCID = 17u, NameID = 2259u, PrimaryActorOID = (uint)OID.Zu, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "wen (ported from BMR)")]
[SkipLocalsInit]
// technically arena center is (0, 90, 0) if you want to visit in hyperborea. It is an irregular shape that can go out to 22.
public sealed class D172Zu : ModuleBase
{
    public D172Zu(WorldState ws, Actor primary) : this(ws, primary, BuildArena()) { }

    // Constructor so we can build arena
    private D172Zu(WorldState ws, Actor primary, (WPos center, ArenaBoundsCustom arena) a) : base(ws, primary, a.center, a.arena) { }

    public static readonly WPos ArenaCenter = new(0f, 0f);

    private static (WPos center, ArenaBoundsCustom arena) BuildArena()
    {
        /*
         * cardinal directions have circles at plus and minus 15 degrees on outside edge for eight circle cutouts.
         * intercardinal direction for 4 rectangle cutouts.
         */
        List<Shape> _shapes = [];

        // Starting position for rotation around origin.
        WPos startPos = new(22f, 0f);

        // 8 circle conduit plus and minus fifteen degrees of cardinal directions.
        Array.ForEach(Angle.AnglesCardinals, cardinal =>
        {
            WPos minusFifteenDegrees = WPos.RotateAroundOrigin(cardinal.Deg - 15.0f, ArenaCenter, startPos);
            WPos plusFifteenDegrees = WPos.RotateAroundOrigin(cardinal.Deg + 15.0f, ArenaCenter, startPos);
            _shapes.Add(new Circle(plusFifteenDegrees, 1f));
            _shapes.Add(new Circle(minusFifteenDegrees, 1f));
        });

        // 4 rectangle lights at intercardinals
        Array.ForEach(Angle.AnglesIntercardinals, interCardinal =>
        {
            WPos pos = WPos.RotateAroundOrigin(interCardinal.Deg, ArenaCenter, startPos);
            _shapes.Add(new Square(pos, 1f, interCardinal));
            _shapes.Add(new Rectangle(pos, 1.75f, 0.35f, interCardinal));
        });

        // Add in rectangles for the gate entrance and exit.
        ArenaBoundsCustom arena = new([new Circle(ArenaCenter, 22f)],
        [
            .._shapes, new Rectangle(new(-22.0f, 0.0f), 4.45f, 0.5f, -89.5f.Degrees()),
            new Rectangle(new(22.0f, 0.0f), 4.45f, 0.5f, -89.5f.Degrees())
        ]);

        return (ArenaCenter, arena);
    }
}
