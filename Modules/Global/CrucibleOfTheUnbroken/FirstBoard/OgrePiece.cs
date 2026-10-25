// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.CrucibleOfTheUnbroken.FirstBoard.OgrePiece;

public enum OID : uint
{
    Actor1ec0fb = 0x1EC0FB, // R0.500, x?, EventObj type
    OgrePiece = 0x4B8D, // R2.080, x?

    MagmaPuddle = 0x1EC025, // R0.500, x?, EventObj type
    WispPiece = 0x4B8E, // R0.800, x?
    GreatWispPiece = 0x4DD4, // R1.600, x?
    BurningWardPuddle = 0x1E9927, // R0.500, x?, EventObj type
    BallOfFire = 0x4B8F, // R1.000, x?
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack_ = 49682, // OgrePiece->player, no cast, single-target
    Teleport = 46914, // OgrePiece->location, no cast, single-target

    ScorchingSmite = 46913, // OgrePiece->self, 5.0+1.0s cast, single-target
    ScorchingSmite1 = 46912, // Helper->self, 6.0s cast, range 40 120.000-degree cone
    Allfire = 46915, // OgrePiece->self, 4.0s cast, range 40 circle
    Magma = 46916, // Helper->location, 3.0s cast, range 3 circle
    Magma1 = 46917, // Helper->location, 3.0s cast, range 5 circle
    ScorchingSmite2 = 46910, // OgrePiece->self, 6.0s cast, single-target : Visual for teleport from puddle to puddle possibly.
    ScorchingSmite3 = 46911, // OgrePiece->self, no cast, single-target
    ScorchingSmite4 = 49688, // Helper->self, 9.3s cast, range 40 120.000-degree cone
    BurningWard = 46918, // OgrePiece->self, 3.0s cast, single-target : Invincibility shield for OgrePiece
    ScorchedEarth = 46919, // WispPiece->self, no cast, range 60 circle
    ScorchedEarth1 = 49728, // GreatWispPiece->self, no cast, range 60 circle
    FireCall = 46921, // OgrePiece->self, 4.0s cast, single-target : Summons BallOfFire and tethers to player
    ArmOfPurgatory = 46922, // BallOfFire->self, 1.0s cast, range 10 circle

}

public enum SID : uint
{
    BurningWard = 4175, // OgrePiece->OgrePiece, extra=0x0
}

public enum TetherID : uint
{
    FireTether = 17, // BallOfFire -> Player // TODO: When OgrePiece casts FireCall a tether and a floating ball of fire appear.
}

sealed class ScorchingSmite(ModuleBase module)
    : Components.SimpleAOEGroups(module, [(uint)AID.ScorchingSmite1, (uint)AID.ScorchingSmite4], new AOEShapeCone(40f, 60f.Degrees()));

//Magma = 46916, // Helper->location, 3.0s cast, range 3 circle
sealed class Magma(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Magma, 3f);

//Magma1 = 46917, // Helper->location, 3.0s cast, range 5 circle
sealed class Magma1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Magma1, 5f);

sealed class MagmaPuddle(ModuleBase module) : Components.Voidzone(module, 5.0f, m => m.Enemies((uint)OID.MagmaPuddle).Where(z => z.EventState != 7));

//VoidZone under OgrePiece during BurningWard Invincible shield.
sealed class BurningWardPuddle(ModuleBase module) : Components.Voidzone(module, 5.0f, m => m.Enemies((uint)OID.BurningWardPuddle).Where(z => z.EventState != 7));

sealed class WispAdds(ModuleBase module)
    : Components.AddsMulti(module, [(uint)OID.WispPiece, (uint)OID.GreatWispPiece], 1);

sealed class RaidWideFires(ModuleBase module)
    : Components.RaidwideCasts(module, [(uint)AID.Allfire, (uint)AID.ScorchedEarth, (uint)AID.ScorchedEarth1]);

// Ball of Fire will be treated as a moving void zone in order to avoid it blowing up on pc. May need to increase size if it isn't enough room to avoid.
sealed class BallOfFireVoidZone(ModuleBase module) : Components.Voidzone(module, 6.0f, m => m.Enemies((uint)OID.BallOfFire).Where(z => z.EventState != 7));

// When Ball of Fire explodes it is a large r10 circle -- 11 in effect, the ball's own 1y counting: Korha was hit at
// 10.0-11.0y and clean at 10.5-10.9y (2026-10-22, 10-25).
sealed class ArmOfPurgatory(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ArmOfPurgatory, 11f);

// The Ball of Fire trails its tethered player at about 3y/s and, when the tether ends 12.9-14.9s after it appears, casts
// Arm of Purgatory (0.7s) where it stands. Drawn only from that cast, the circle came too late: the ball keeps 6-7y from
// her, and she was caught at 10.0-11.0y five times in two days, twice fatally (Korha, 2026-10-22 and 10-25). So from the
// ball's appearance its blast is ahead, timed from the earliest tether end seen; the dodge keeps her out of it as the ball
// follows, and a later end only means she was clear early. Carried 3y ahead along the ball's heading (it faces her and
// closes at 3y/s): from a circle where it stood, the dodge parked her 3.6y away with the ball still coming.
sealed class ArmOfPurgatoryAhead(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<(Actor Ball, DateTime Blast)> balls = [];
    private readonly List<AOEInstance> aoes = [];
    private static readonly AOEShapeCapsule Shape = new(11f, 3f);

    /// <summary>Earliest tether end seen (12.9s after the ball appears) plus the cast and its landing.</summary>
    private const double BlastAfterSeconds = 12.9d + 0.95d;

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.BallOfFire)
            balls.Add((actor, World.FutureTime(BlastAfterSeconds)));
    }

    public override void OnActorDestroyed(Actor actor) => balls.RemoveAll(b => b.Ball == actor);

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.ArmOfPurgatory)
            balls.RemoveAll(b => b.Ball == caster);
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        aoes.Clear();
        foreach (var (ball, blast) in balls)
            if (!ball.IsDeadOrDestroyed && ball.EventState != 7)
                aoes.Add(new(Shape, ball.Position, ball.Rotation, blast));
        return CollectionsMarshal.AsSpan(aoes);
    }
}

sealed class OgrePieceStates : StateMachineBuilder
{
    public OgrePieceStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ScorchingSmite>()
            .ActivateOnEnter<RaidWideFires>()
            .ActivateOnEnter<Magma>()
            .ActivateOnEnter<Magma1>()
            .ActivateOnEnter<MagmaPuddle>()
            .ActivateOnEnter<BurningWardPuddle>()
            .ActivateOnEnter<WispAdds>()
            .ActivateOnEnter<BallOfFireVoidZone>()
            .ActivateOnEnter<ArmOfPurgatory>()
            .ActivateOnEnter<ArmOfPurgatoryAhead>()
            ;
    }
}

[ModuleInfo(CFCID = 1088u, NameID = 14538u, PrimaryActorOID = (uint)OID.OgrePiece, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "wen (ported from BMR)")]

public sealed class OgrePiece(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(120f, -420f), new ArenaBoundsCircle(20f))
{
    // The Wisps walk to the Ogre and Scorched Earth when they reach it, 19.0-19.3s after they turn targetable however far
    // they start, and each one that arrives gives the beastmaster and her familiar a Vulnerability Up stack. A wave is
    // three Wisps (171 HP) and a Great Wisp (1,575). Both were one priority (and AddsMulti applies none), so the Great Wisp
    // took 11-17s of the 19 and two or three Wisps arrived a wave (Korha, 2026-10-25: 4, 5 and 5 in three pulls). The
    // Wisps first -- nearest first, which PriorityTargets orders -- then the Great Wisp.
    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        foreach (var e in hints.PotentialTargets)
            if (!e.Actor.IsDeadOrDestroyed)
                e.Priority = e.Actor.OID switch
                {
                    (uint)OID.WispPiece => 2,
                    (uint)OID.GreatWispPiece => 1,
                    _ => e.Priority,
                };
    }
}
