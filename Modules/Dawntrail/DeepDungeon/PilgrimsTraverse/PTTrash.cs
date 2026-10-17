// Pilgrim's Traverse trash: the casts whose shape the game's sheet gets wrong, drawn right. Everything else on the trash
// floors is still the cast-bar guess (AutoHints), which reads shapes off the sheet and is right for most of them.
//
// Checked 2026-10-17 against Splatoon's Pilgrim's Traverse presets (Leathen-94 and Ksirashi) and BossmodReborn's
// PTFloorModule, cast by cast through `Minerva.Validate --action`. Nothing here has been seen in a recording yet.
//  - Side and rear cleaves: the sheet's cone has no side, so the guess drew them in front of the mob. Sides and angles
//    are Splatoon's. Its angles turn clockwise and Minerva's anticlockwise, so its +90 (the mob's right) is -90 here.
//  - Second hits with no cast bar of their own, and the true safe holes of rings: BossmodReborn's, with Splatoon's
//    timing where BossmodReborn has none.
//  - Lithic Breath and Heavy Smash: Splatoon's sizes, larger than the sheet's.
//  - Traverse Monitor's Smoldering Scales: spikes that hurt whoever hits it. It is not attacked while they are up.
// Not done:
//  - Rolling Barrage and Incinerating Lahar (45 and 46y circles, 16s casts) stay unavoidable, as in the guess. BossmodReborn
//    runs from them, Splatoon only draws them. A 30y trash arena has no ground that far away.
//  - The line-of-sight casts (Forgiven Grudge, Invoked Succubus), and the Talos and Stone Eater leaps, which have no cast
//    to key on.
//  - Three circles Splatoon draws larger than the sheet: Tempestuous Temper (Amemet, 12 against 8), Lightning Bolt
//    (Cerberus, 10 against 8), Moatmaker (15 against 9). These target a place, so the size may be Splatoon's margin;
//    they stay at the sheet's.
//  - BossmodReborn's deep dungeon assistant (traps, chests, pomanders, kiting the Troubadour turtles): out of scope.
using System;
using System.Collections.Generic;
using Minerva;
using Minerva.Automation;

namespace Minerva.Dawntrail.DeepDungeon.PilgrimsTraverse.Trash;

public enum AID : uint
{
    LeftTentacle1 = 41780, // 180-degree half on the mob's left; the sheet says range 60
    RightTentacle1 = 41782,
    LeftTentacle2 = 44690,
    RightTentacle2 = 44691,
    EarthenAuger = 42091, // Traverse Sand Serpent: range 3-30, 270 degrees round its front
    MasterOfLevin = 44732, // Traverse Howler: range 5-30 donut
    WhorlOfHurt = 44729, // the bird of 51-60: donut, 8-28 by Splatoon, 25 by the sheet
    RightSidedShockwave = 42214, // Forgiven Riot: right half, then its left half
    RightSidedShockwaveSecond = 42215,
    LeftSidedShockwave = 42216,
    LeftSidedShockwaveSecond = 42217,
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
    ClawAndTail = 43132, // Traverse Cama: a line in front, then one behind
    TailBlow = 44786, // Invoked Cerberus: 90 degrees behind
    LithicBreath = 44508, // Invoked Dahak: 120 degrees, 40y by Splatoon; the sheet says 90 and 28
    HoneyedLeft = 44357, // Invoked Sawtooth: 180 degrees centred 135 to the mob's left
    HoneyedRight = 44358, // and to its right
    RightSmite = 44345, // Invoked Gusion: its right half, then its left; the sheet calls it an 80y line
    LeftSmite = 44347,
    HeavySmash = 44342, // Traverse Warg: 10y by Splatoon, 6 by the sheet
    AuthoritysEdgeRight = 44509, // Invoked Acheron: its right half; the sheet's cone is 90 degrees
    AuthoritysEdgeLeft = 44510,
}

public enum SID : uint
{
    BlazeSpikes = 4579,
}

public abstract class PTTrash : ZoneModule
{
    /// <summary>How long Smoldering Scales' spikes are trusted to last if their status is never seen to go.</summary>
    private const double SpikesSeconds = 10d;

    private static readonly IReadOnlyDictionary<uint, CastCorrections.Entry> Table = new Dictionary<uint, CastCorrections.Entry>
    {
        [(uint)AID.LeftTentacle1] = new(Cone(60f, 90f, 90f)),
        [(uint)AID.LeftTentacle2] = new(Cone(60f, 90f, 90f)),
        [(uint)AID.RightTentacle1] = new(Cone(60f, 90f, -90f)),
        [(uint)AID.RightTentacle2] = new(Cone(60f, 90f, -90f)),
        [(uint)AID.EarthenAuger] = new(_ => new AOEShapeDonutSector(3f, 30f, 135f.Degrees())),
        [(uint)AID.MasterOfLevin] = new(Donut(5f, 30f)),
        // the sheet's outer edge is 25 and Splatoon's 28, its hole 8 and the guess's 6: the larger danger of each
        [(uint)AID.WhorlOfHurt] = new(Donut(6f, 28f)),
        [(uint)AID.RightSidedShockwave] = new(Cone(30f, 90f, -90f), new CastCorrections.FollowUp(Cone(30f, 90f, 90f), 4d, (uint)AID.RightSidedShockwaveSecond)),
        [(uint)AID.LeftSidedShockwave] = new(Cone(30f, 90f, 90f), new CastCorrections.FollowUp(Cone(30f, 90f, -90f), 4d, (uint)AID.LeftSidedShockwaveSecond)),
        [(uint)AID.GrowingCirclesOfAblution] = new(Circle(10f), new CastCorrections.FollowUp(Donut(10f, 40f), 5d, (uint)AID.GrowingCirclesOfAblutionSecond)),
        [(uint)AID.ShrinkingCirclesOfAblution] = new(Donut(10f, 40f), new CastCorrections.FollowUp(Circle(10f), 5d, (uint)AID.ShrinkingCirclesOfAblutionSecond)),
        [(uint)AID.HailOfHeels] = new(Cone(8f, 90f), new CastCorrections.FollowUp(Cone(8f, 90f), 8d, (uint)AID.HailOfHeelsRepeat, 3)),
        [(uint)AID.PeripheralLasers] = new(Donut(5f, 60f)),
        [(uint)AID.Accelerate] = new(Circle(6f), new CastCorrections.FollowUp(Donut(5f, 10f), 4d, (uint)AID.Subduction)),
        // Splatoon's second line shows for the two seconds after the cast; its own action is not known
        [(uint)AID.ClawAndTail] = new(Rect(8f, 1.5f), new CastCorrections.FollowUp(Rect(10f, 1.5f, 180f), 2.5d)),
        [(uint)AID.TailBlow] = new(Cone(19f, 45f, 180f)),
        [(uint)AID.LithicBreath] = new(Cone(40f, 60f)),
        [(uint)AID.HoneyedLeft] = new(Cone(30f, 90f, 135f)),
        [(uint)AID.HoneyedRight] = new(Cone(30f, 90f, -135f)),
        [(uint)AID.RightSmite] = new(Cone(80f, 90f, -90f), new CastCorrections.FollowUp(Cone(80f, 90f, 90f), 2.5d)),
        [(uint)AID.LeftSmite] = new(Cone(80f, 90f, 90f), new CastCorrections.FollowUp(Cone(80f, 90f, -90f), 2.5d)),
        [(uint)AID.HeavySmash] = new(Circle(10f)),
        [(uint)AID.AuthoritysEdgeRight] = new(Cone(40f, 90f, -90f)),
        [(uint)AID.AuthoritysEdgeLeft] = new(Cone(40f, 90f, 90f)),
    };

    // a cone or a line starts at a large mob's edge, as in the guess; a circle or a ring is measured from its centre
    private static Func<Actor, AOEShape> Cone(float radius, float halfAngle, float offset = 0f) => a => new AOEShapeCone(radius + a.HitboxRadius, halfAngle.Degrees(), offset.Degrees());
    private static Func<Actor, AOEShape> Rect(float length, float halfWidth, float offset = 0f) => a => new AOEShapeRect(length + a.HitboxRadius, halfWidth, 0f, offset.Degrees());
    private static Func<Actor, AOEShape> Circle(float radius) => _ => new AOEShapeCircle(radius);
    private static Func<Actor, AOEShape> Donut(float inner, float outer) => _ => new AOEShapeDonut(inner, outer);

    private readonly CastCorrections casts;
    private readonly Dictionary<ulong, DateTime> spiked = [];
    private readonly EventSubscriptions subscriptions;

    protected PTTrash(WorldState ws) : base(ws)
    {
        this.casts = new CastCorrections(ws, Table);
        this.subscriptions = new(
            ws.Actors.CastFinished.Subscribe((actor, cast) =>
            {
                if (cast.Action.ID == (uint)AID.SmolderingScales)
                    this.spiked[actor.InstanceID] = ws.FutureTime(SpikesSeconds);
            }),
            ws.Actors.StatusLose.Subscribe((actor, index) =>
            {
                if (actor.Statuses[index].ID == (uint)SID.BlazeSpikes)
                    this.spiked.Remove(actor.InstanceID);
            }),
            ws.Actors.Removed.Subscribe(actor => this.spiked.Remove(actor.InstanceID)));
    }

    protected override void Dispose(bool disposing)
    {
        this.casts.Dispose();
        this.subscriptions.Dispose();
        base.Dispose(disposing);
    }

    public override bool ReplacesGuess(uint action) => this.casts.Replaces(action);

    public override void CalculateAIHints(int playerSlot, Actor player, AIHints hints)
    {
        this.casts.AddForbiddenZones(hints);

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
