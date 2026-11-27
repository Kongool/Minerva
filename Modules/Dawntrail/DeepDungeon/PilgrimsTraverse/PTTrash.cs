// Pilgrim's Traverse trash: the casts whose shape the game's sheet gets wrong, drawn right. Everything else on the trash
// floors is still the cast-bar guess (AutoHints), which reads shapes off the sheet and is right for most of them.
//
// Checked 2026-10-17 against Splatoon's Pilgrim's Traverse presets (Leathen-94 and Ksirashi) and BossmodReborn's
// PTFloorModule, cast by cast through `Minerva.Validate --action`, and 2026-11-24 against Nagumo's Splatoon presets
// (Bellows, Claw and Tail's second hit). Seen in a recording: the tentacles and Forgiven Riot's shockwaves (2026-11-22),
// Passions' Heat and Incinerating Lahar (2026-11-24); nothing else here has been.
//  - Side and rear cleaves: a cast's own rotation already points where it hits, so a cast is drawn straight down it
//    with the sheet's width. Seen on both recorded families, and it is what BossmodReborn does (it draws these casts as
//    the sheet says and adds only the second hit). The October table turned each one a further way to its side, which
//    drew it where it does not hit: Korha in Left Tentacle, Saar and Rosa in Left-sided Shockwave, all told they were
//    safe. A hit after the cast has no cast of its own and is drawn from the mob's facing, so it keeps its side.
//    Splatoon's angles, where those came from, turn clockwise and Minerva's anticlockwise: its +90 (the mob's right)
//    is -90 here.
//  - Second hits with no cast bar of their own, and the true safe holes of rings: BossmodReborn's, with Splatoon's
//    timing where BossmodReborn has none.
//  - Lithic Breath and Heavy Smash: Splatoon's sizes, larger than the sheet's.
//  - Traverse Monitor's Smoldering Scales: spikes that hurt whoever hits it. It is not attacked while they are up.
//  - Rolling Barrage (Forgiven Corruption, 71-80) and Incinerating Lahar (Invoked Baal, 81-90): 16s casts over 45 and
//    46y that a wall stops. Drawn less the ground a wall hides from the caster (the game is asked, live only), so the
//    dodge walks behind a wall or out of reach. Lahar seen 2026-11-24; BossmodReborn only runs out of reach.
//  - Invoked Succubus's Passions' Heat: a 10y circle on one toon that gives Pyretic to everyone in it. The others
//    leave the circle; the toon it is on stops moving and acting before it lands and until the Pyretic is gone.
//    BossmodReborn has it as a to-do. Four deaths in three casts on 2026-11-24, before this was written.
// Not done:
//  - The line-of-sight cast (Forgiven Grudge), and the Talos and Stone Eater leaps, which have no cast to key on.
//  - Two circles Splatoon draws larger than the sheet: Tempestuous Temper (Amemet, 12 against 8) and Lightning Bolt
//    (Cerberus, 10 against 8). These target a place, so the size may be Splatoon's margin; they stay at the sheet's.
//    (Moatmaker's 15 was a second hit, now drawn.)
//  - BossmodReborn's deep dungeon assistant (traps, chests, pomanders, kiting the Troubadour turtles): out of scope.
using System;
using System.Collections.Generic;
using Minerva;
using Minerva.Automation;

namespace Minerva.Dawntrail.DeepDungeon.PilgrimsTraverse.Trash;

public enum AID : uint
{
    LeftTentacle1 = 41780, // 180-degree half on the mob's left, aimed by the cast itself (see the table); the sheet says range 60
    RightTentacle1 = 41782,
    LeftTentacle2 = 44690,
    RightTentacle2 = 44691,
    EarthenAuger = 42091, // Traverse Sand Serpent: range 3-30, 270 degrees round its front
    MasterOfLevin = 44732, // Traverse Howler: range 5-30 donut
    WhorlOfHurt = 44729, // the bird of 51-60: donut, 8-28 by Splatoon, 25 by the sheet
    RightSidedShockwave = 42214, // Forgiven Riot: right half, then its left half
    RightSideHit = 42215, // the right half with no cast: Left-sided Shockwave's second hit (seen 2026-11-22)
    LeftSidedShockwave = 42216,
    LeftSideHit = 42217, // and the left half, Right-sided's
    GrowingCirclesOfAblution = 42578, // Forgiven Spite: a 10y circle, then a 10-40 donut
    GrowingCirclesOfAblutionSecond = 42749,
    ShrinkingCirclesOfAblution = 42748, // the donut first, then the circle
    ShrinkingCirclesOfAblutionSecond = 42746,
    HailOfHeels = 44759, // Forgiven Arrogance: the cast, then three more of the same cone
    HailOfHeelsRepeat = 44760,
    PeripheralLasers = 44758, // Forgiven Vanity: range 5-60 donut
    Accelerate = 42516, // leaps to a place (6y circle), then a 5-10 donut round where it landed
    Subduction = 42517,
    SmolderingScales = 42212, // Traverse Monitor: spikes
    ClawAndTail = 43132, // Traverse Cama: a line in front, then its tail behind
    ClawAndTailSecond = 43131,
    Moatmaker = 40558, // the stone soldier of 36-39: 9y round it, then 15y two seconds later
    Bellows = 44293, // Invoked Humbaba: 120 degrees by Splatoon; the sheet has no angle and the guess drew 90
    TailBlow = 44786, // Invoked Cerberus: 90 degrees behind
    LithicBreath = 44508, // Invoked Dahak: 120 degrees, 40y by Splatoon; the sheet says 90 and 28
    HoneyedLeft = 44357, // Invoked Sawtooth: 180 degrees centred 135 to the mob's left
    HoneyedRight = 44358, // and to its right
    RightSmite = 44345, // Invoked Gusion: its right half, then its left; the sheet calls it an 80y line
    LeftSmite = 44347,
    HeavySmash = 44342, // Traverse Warg: 10y by Splatoon, 6 by the sheet
    AuthoritysEdgeRight = 44509, // Invoked Acheron: its right half; the sheet's cone is 90 degrees
    AuthoritysEdgeLeft = 44510,
    PassionsHeat = 43322, // Invoked Succubus: a 10y circle on one player; everyone in it gets Pyretic
    RollingBarrage = 42523, // Forgiven Corruption: a 16s cast, 45y round it, stopped by walls
    IncineratingLahar = 43133, // Invoked Baal: the same over 46y; kills
}

public enum SID : uint
{
    BlazeSpikes = 4579,
    Pyretic = 960,
}

public abstract class PTTrash : ZoneModule
{
    /// <summary>How long Smoldering Scales' spikes are trusted to last if their status is never seen to go.</summary>
    private const double SpikesSeconds = 10d;

    // Invoked Dahak (floors 93-95) swings its tail at whoever is behind it: Tail Drive (44507), no cast bar, a 30y cone
    // on the sheet. Korha's recording 2026-11-26 18:26: 6.4s into the pull Saar, 13.0y from it and 178 degrees off its
    // facing, took about 638,000 and died; the three toons in front were not touched. The sheet's cone is 90 degrees,
    // as it is for Lithic Breath, which Splatoon measures at 120: the wider is drawn. The community script draws 10.5y,
    // which the 13y hit rules out, so the sheet's reach from the mob's edge (3.5y) stands.
    // It was pulled by Korha from 23y and ran at him straight past Saar, who had gone to meet it: 2.3y in front of it
    // at 5.0s, 4.1y behind at 5.4s, and no walk leaves a cone whose point is running away at 8y a second. So the cone
    // is also drawn from the toon it is running at, back along its way, until it has arrived.
    private const uint InvokedDahak = 0x49A6;
    private const float TailDriveArrived = 5f;
    private static readonly AOEShapeCone TailDrive = new(33.5f, 60f.Degrees());

    private static readonly IReadOnlyDictionary<uint, CastCorrections.Entry> Table = new Dictionary<uint, CastCorrections.Entry>
    {
        // The cast's own rotation already points at the side it hits: the mob turns to its target and the cast faces
        // 90 off that. Forgiven Petulance, Korha 2026-11-22: Left Tentacle cast at -19 by a mob that ended facing -109;
        // it hit Korha at -54 and missed three toons at -133 to -158. Turned a further 90 here, it was drawn behind
        // Korha and he stood in it. Right Tentacle the same (160 on a mob facing -109). The first pair has not been
        // recorded; it is the same cast family, and BossmodReborn aims its own tentacles straight down the cast.
        [(uint)AID.LeftTentacle1] = new(Cone(60f, 90f)),
        [(uint)AID.LeftTentacle2] = new(Cone(60f, 90f)),
        [(uint)AID.RightTentacle1] = new(Cone(60f, 90f)),
        [(uint)AID.RightTentacle2] = new(Cone(60f, 90f)),
        [(uint)AID.EarthenAuger] = new(_ => new AOEShapeDonutSector(3f, 30f, 135f.Degrees())),
        [(uint)AID.MasterOfLevin] = new(Donut(5f, 30f)),
        // the sheet's outer edge is 25 and Splatoon's 28, its hole 8 and the guess's 6: the larger danger of each
        [(uint)AID.WhorlOfHurt] = new(Donut(6f, 28f)),
        // Forgiven Riot, Korha 2026-11-22: Left-sided cast at -53 by a mob facing -143; Saar (-91) and Rosa (-113) hit,
        // Xia and Korha on the half's edges (87 and 90 off) clear. Its second hit, 42215, landed 2.6s later on the other half.
        [(uint)AID.RightSidedShockwave] = new(Cone(30f, 90f), new CastCorrections.FollowUp(Cone(30f, 90f, 90f), 4d, (uint)AID.LeftSideHit)),
        [(uint)AID.LeftSidedShockwave] = new(Cone(30f, 90f), new CastCorrections.FollowUp(Cone(30f, 90f, -90f), 4d, (uint)AID.RightSideHit)),
        [(uint)AID.GrowingCirclesOfAblution] = new(Circle(10f), new CastCorrections.FollowUp(Donut(10f, 40f), 5d, (uint)AID.GrowingCirclesOfAblutionSecond)),
        [(uint)AID.ShrinkingCirclesOfAblution] = new(Donut(10f, 40f), new CastCorrections.FollowUp(Circle(10f), 5d, (uint)AID.ShrinkingCirclesOfAblutionSecond)),
        [(uint)AID.HailOfHeels] = new(Cone(8f, 90f), new CastCorrections.FollowUp(Cone(8f, 90f), 8d, (uint)AID.HailOfHeelsRepeat, 3)),
        [(uint)AID.PeripheralLasers] = new(Donut(5f, 60f)),
        [(uint)AID.Accelerate] = new(Circle(6f), new CastCorrections.FollowUp(Donut(5f, 10f), 4d, (uint)AID.Subduction)),
        // The second hit is 43131, a 10y cone on the sheet. The presets of 2026-10-17 drew a line behind for two seconds;
        // Nagumo's preset and script (2026-11-24) a 120-degree tail sweep behind, landing about 3s after the cast.
        [(uint)AID.ClawAndTail] = new(Rect(8f, 1.5f), new CastCorrections.FollowUp(Cone(10f, 60f, 180f), 3.5d, (uint)AID.ClawAndTailSecond)),
        // Nagumo's script: a second, wider circle about 2s after the cast lands. Three presets draw it at 15y.
        [(uint)AID.Moatmaker] = new(a => new AOEShapeCircle(9f + a.HitboxRadius), new CastCorrections.FollowUp(Circle(15f), 2.5d)),
        [(uint)AID.Bellows] = new(Cone(25f, 60f)),
        [(uint)AID.TailBlow] = new(Cone(19f, 45f)),
        [(uint)AID.LithicBreath] = new(Cone(40f, 60f)),
        [(uint)AID.HoneyedLeft] = new(Cone(30f, 90f)),
        [(uint)AID.HoneyedRight] = new(Cone(30f, 90f)),
        [(uint)AID.RightSmite] = new(Cone(80f, 90f), new CastCorrections.FollowUp(Cone(80f, 90f, 90f), 2.5d)),
        [(uint)AID.LeftSmite] = new(Cone(80f, 90f), new CastCorrections.FollowUp(Cone(80f, 90f, -90f), 2.5d)),
        [(uint)AID.HeavySmash] = new(Circle(10f)),
        [(uint)AID.AuthoritysEdgeRight] = new(Cone(40f, 90f)),
        [(uint)AID.AuthoritysEdgeLeft] = new(Cone(40f, 90f)),
    };

    // a cone or a line starts at a large mob's edge, as in the guess; a circle or a ring is measured from its centre
    private static Func<Actor, AOEShape> Cone(float radius, float halfAngle, float offset = 0f) => a => new AOEShapeCone(radius + a.HitboxRadius, halfAngle.Degrees(), offset.Degrees());
    private static Func<Actor, AOEShape> Rect(float length, float halfWidth, float offset = 0f) => a => new AOEShapeRect(length + a.HitboxRadius, halfWidth, 0f, offset.Degrees());
    private static Func<Actor, AOEShape> Circle(float radius) => _ => new AOEShapeCircle(radius);
    private static Func<Actor, AOEShape> Donut(float inner, float outer) => _ => new AOEShapeDonut(inner, outer);

    // Passions' Heat, 2026-11-24 (Korha, floors 81-90): cast on Saar with Korha 5.4y away, both hit and dead
    // half a second later; Rosa at 10.07y was not hit. Then Rosa and Xia the same way, then Korha alone. Every one of
    // them had acted or moved after the cast went off: Pyretic kills on the first action, and the status only shows
    // on the toon half a second after it has been decided.
    private const float HeatRadius = 10f;
    /// <summary>Drawn a yalm wider than it is: it is centred on a toon who may still be moving.</summary>
    private const float HeatDrawn = HeatRadius + 1f;
    /// <summary>Counted as caught half a yalm outside it: the server's idea of where a toon stands runs late.</summary>
    private const float HeatCaught = HeatRadius + 0.5f;
    /// <summary>The toon it is on stops this long before the cast bar's end, which is itself 0.3s before the cast goes off.</summary>
    private const double HeatLead = 0.5d;
    /// <summary>A toon walking out has to be clear this long before the cast goes off, or it stops where it is.</summary>
    private const float HeatClearBy = 0.3f;
    /// <summary>How long after the cast a toon it caught stays stopped while the status has yet to show.</summary>
    private const double HeatBridge = 1.5d;

    // Invoked Baal, 2026-11-24 (Korha, floors 81-90): a Baal nobody was fighting cast Incinerating Lahar from the next
    // room. 16s later Rosa (23.6y off), Saar (34.9y) and Korha (39.8y) took 500-680k each and died; the two melee had
    // been 46-48y away when it began and followed their mobs in. Xia, 34.4y off on another bearing, was not hit at all:
    // a wall stood between. The guess leaves a circle this wide alone as a raidwide. The sheet's radius is measured here
    // from the mob's edge: nobody has stood near the rim to say which it is.
    private static readonly IReadOnlyDictionary<uint, SightCasts.Entry> BehindWalls = new Dictionary<uint, SightCasts.Entry>
    {
        [(uint)AID.RollingBarrage] = new(45f),
        [(uint)AID.IncineratingLahar] = new(46f),
    };

    private readonly CastCorrections casts;
    private readonly SightCasts sight;
    private readonly List<Actor> heatCasters = [];
    private (WPos Centre, ulong Target, DateTime Until)? heatLanded;
    private readonly Dictionary<ulong, DateTime> spiked = [];
    private readonly EventSubscriptions subscriptions;

    protected PTTrash(WorldState ws) : base(ws)
    {
        this.casts = new CastCorrections(ws, Table);
        this.sight = new SightCasts(ws, BehindWalls);
        this.subscriptions = new(
            ws.Actors.CastStarted.Subscribe(actor =>
            {
                if (actor.CastInfo?.Action.ID == (uint)AID.PassionsHeat && !this.heatCasters.Contains(actor))
                    this.heatCasters.Add(actor);
            }),
            ws.Actors.CastFinished.Subscribe((actor, cast) =>
            {
                if (cast.Action.ID == (uint)AID.SmolderingScales)
                    this.spiked[actor.InstanceID] = ws.FutureTime(SpikesSeconds);
                this.heatCasters.Remove(actor);
            }),
            ws.Actors.CastEvent.Subscribe((actor, ev) =>
            {
                if (ev.Action.ID == (uint)AID.PassionsHeat && ws.Actors.Find(ev.MainTargetID) is { } target)
                    this.heatLanded = (target.Position, target.InstanceID, ws.FutureTime(HeatBridge));
            }),
            ws.Actors.StatusLose.Subscribe((actor, index) =>
            {
                if (actor.Statuses[index].ID == (uint)SID.BlazeSpikes)
                    this.spiked.Remove(actor.InstanceID);
            }),
            ws.Actors.Removed.Subscribe(actor =>
            {
                this.spiked.Remove(actor.InstanceID);
                this.heatCasters.Remove(actor);
            }));
    }

    protected override void Dispose(bool disposing)
    {
        this.casts.Dispose();
        this.sight.Dispose();
        this.subscriptions.Dispose();
        base.Dispose(disposing);
    }

    // the guess draws Passions' Heat where its target stood when the cast began, and the circle follows the target
    public override bool ReplacesGuess(uint action) => action == (uint)AID.PassionsHeat || this.casts.Replaces(action) || this.sight.Replaces(action);

    public override void CalculateAIHints(int playerSlot, Actor player, AIHints hints)
    {
        this.casts.AddForbiddenZones(hints);
        this.sight.AddForbiddenZones(player, hints);
        this.AddPassionsHeat(player, hints);

        // Tail Drive has no cast: behind a Dahak that is fighting is never somewhere to stand. Nor is the lane between
        // it and the toon it is running at: it stops in front of that toon, and everything it ran past is then behind it.
        foreach (var a in World.Actors)
        {
            if (a.OID != InvokedDahak || !a.InCombat || a.IsDeadOrDestroyed)
                continue;
            hints.AddForbiddenZone(TailDrive, a.Position, a.Rotation + 180f.Degrees());
            if (a.TargetID != player.InstanceID && World.Actors.Find(a.TargetID) is { IsDeadOrDestroyed: false } target)
            {
                var back = a.Position - target.Position;
                if (back.LengthSq() > TailDriveArrived * TailDriveArrived)
                    hints.AddForbiddenZone(TailDrive, target.Position, Angle.FromDirection(back));
            }
        }

        // Spikes hurt whoever hits the mob: leave it alone while they are up, by the status or, if that is never seen,
        // for as long as they are known to last.
        var now = World.CurrentTime;
        foreach (var e in hints.PotentialTargets)
        {
            var a = e.Actor;
            if (a.FindStatus((uint)SID.BlazeSpikes) != null || this.spiked.TryGetValue(a.InstanceID, out var until) && now < until)
                e.Priority = AIHints.Enemy.PriorityForbidden;
        }
    }

    private void AddPassionsHeat(Actor player, AIHints hints)
    {
        var now = World.CurrentTime;
        foreach (var caster in this.heatCasters)
        {
            if (caster.IsDeadOrDestroyed || caster.CastInfo is not { } cast || cast.Action.ID != (uint)AID.PassionsHeat)
                continue;
            if (cast.TargetID == player.InstanceID)
            {
                // nothing to walk out of: it is on this toon. Stop before it lands.
                hints.AddSpecialMode(AIHints.SpecialMode.Pyretic, World.FutureTime(cast.RemainingTime - HeatLead));
            }
            else if (World.Actors.Find(cast.TargetID) is { IsDeadOrDestroyed: false } target)
            {
                hints.AddForbiddenZone(new AOEShapeCircle(HeatDrawn), target.Position, default, World.FutureTime(cast.NPCRemainingTime));
                // No time left to walk clear: being hit standing still is survivable, being hit walking is not.
                var reach = (player.Position - target.Position).Length() + ArenaPathfinder.DefaultMoveSpeed * (cast.NPCRemainingTime - HeatClearBy);
                if (reach < HeatCaught)
                    hints.AddSpecialMode(AIHints.SpecialMode.Pyretic, now);
            }
        }

        var caught = this.heatLanded is { } landed && now < landed.Until
            && (landed.Target == player.InstanceID || player.Position.InCircle(landed.Centre, HeatCaught));
        if (caught || player.FindStatus((uint)SID.Pyretic) != null)
            hints.AddSpecialMode(AIHints.SpecialMode.Pyretic, now);
    }
}

[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 1032)]
public sealed class PT10Trash(WorldState ws) : PTTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 1033)]
public sealed class PT20Trash(WorldState ws) : PTTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 1034)]
public sealed class PT30Trash(WorldState ws) : PTTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 1035)]
public sealed class PT40Trash(WorldState ws) : PTTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 1036)]
public sealed class PT50Trash(WorldState ws) : PTTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 1037)]
public sealed class PT60Trash(WorldState ws) : PTTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 1038)]
public sealed class PT70Trash(WorldState ws) : PTTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 1039)]
public sealed class PT80Trash(WorldState ws) : PTTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 1040)]
public sealed class PT90Trash(WorldState ws) : PTTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 1041)]
public sealed class PT100Trash(WorldState ws) : PTTrash(ws);
