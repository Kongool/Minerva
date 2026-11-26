// Eureka Pagos: what the cast-bar guess (AutoHints) gets wrong. Everything else in the zone is still the guess, which
// reads shapes off the game's sheet. BossmodReborn has no Pagos fights (its Pagos.cs is the NM farm list only).
// Corrections rather than a boss module: a boss module in charge turns the guess off for every other enemy, and a Pagos
// NM is fought in open field among trash.
//  - Voretooth (NM, FATE 1367, R3.8). None of its cones has an omen, so the guess draws each at 90 degrees. From the
//    hit lists against every player's spot at the end of the cast, all eight recordings of 2026-11-17 20:59-21:02:
//    - Buffet: 120 degrees. Hit at 56 and 59 degrees off its facing (Korha at 5.7y, at the guess's edge), missed at 70.
//      Range 8 + R: hit at 11.2y, missed at 13.8y. The Snowcloak Yeti's Buffet is the same 120-degree cone.
//    - Heavy Toss: 120 degrees. Hit at 50-52 degrees (Miso at 13.8y), missed at 62 and over. CastType 13, so 15 from the
//      centre (BossmodReborn's reading of 13): hit at 13.4-13.9y, missed at 19.9-20.6y straight ahead.
//    - Frozen Mist: puts Deep Freeze (3479, a stun with damage over time) on whoever it hits. Hit at 0 and 5 degrees,
//      missed at 47 and over, so 90 degrees, the guess's own angle, is the most it can be. One miss at 43.3y, 19 degrees
//      off, would make it narrower or shorter than 40 + R; kept at the wider reading until a hit says otherwise.
//    - Heavy Scrapline (11886, an 11y circle on Voretooth) has no cast bar and nothing announces it, but it keeps its
//      own clock: the five in those recordings came 32.5, 33.1, 33.6 and 33.0s apart, whatever Voretooth was casting in
//      between. Hit at up to 9.8y, missed from 11.2y. It took 8.8-9.0k from a 34k toon each time (Saar and Xia four of
//      five, more than their whole HP over the two pulls), so it is the fight's main damage and is drawn from the
//      clock: the circle is forbidden from ScraplineAhead before the earliest it can come until it lands. The first of
//      a pull cannot be known and is taken. A melee toon is out of reach for about 3 of every 33 seconds for it.
using System;
using System.Collections.Generic;
using Minerva;
using Minerva.Automation;

namespace Minerva.Stormblood.Foray.Pagos;

public enum AID : uint
{
    FrozenMist = 11883, // Voretooth->self, 3.7s cast, range 40+R cone, Deep Freeze
    Buffet = 11884, // Voretooth->self, 3.7s cast, range 8+R 120-degree cone
    HeavyToss = 11885, // Voretooth->self, 3.7s cast, range 15 120-degree cone
    HeavyScrapline = 11886, // Voretooth->self, no cast, range 11 circle
}

[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 581)]
public sealed class PagosField : ZoneModule
{
    private static readonly IReadOnlyDictionary<uint, CastCorrections.Entry> Table = new Dictionary<uint, CastCorrections.Entry>
    {
        [(uint)AID.FrozenMist] = new(a => new AOEShapeCone(40f + a.HitboxRadius, 45f.Degrees())),
        [(uint)AID.Buffet] = new(a => new AOEShapeCone(8f + a.HitboxRadius, 60f.Degrees())),
        [(uint)AID.HeavyToss] = new(_ => new AOEShapeCone(15f, 60f.Degrees())),
    };

    private const float ScraplineRadius = 11f;
    private const double ScraplineEvery = 32.5d; // the shortest gap seen
    private const double ScraplineAhead = 2.5d; // 7y from Voretooth's edge to the rim is 1.2s on foot
    private const double ScraplineLate = 3d; // the longest gap seen was 1.2s over the shortest; past this it is not coming

    private static readonly AOEShapeCircle ScraplineShape = new(ScraplineRadius);

    private readonly CastCorrections casts;
    private readonly EventSubscriptions subscriptions;
    private readonly Dictionary<ulong, DateTime> scraplineDue = []; // by caster: the earliest the next one can land

    public PagosField(WorldState ws) : base(ws)
    {
        this.casts = new CastCorrections(ws, Table);
        this.subscriptions = new(
            ws.Actors.CastEvent.Subscribe((actor, ev) =>
            {
                if (ev.Action.ID == (uint)AID.HeavyScrapline)
                    this.scraplineDue[actor.InstanceID] = ws.FutureTime(ScraplineEvery);
            }),
            ws.Actors.Removed.Subscribe(actor => this.scraplineDue.Remove(actor.InstanceID)));
    }

    protected override void Dispose(bool disposing)
    {
        this.subscriptions.Dispose();
        this.casts.Dispose();
        base.Dispose(disposing);
    }

    public override bool ReplacesGuess(uint action) => this.casts.Replaces(action);

    public override void CalculateAIHints(int playerSlot, Actor player, AIHints hints)
    {
        this.casts.AddForbiddenZones(hints);
        var now = this.World.CurrentTime;
        foreach (var (id, due) in this.scraplineDue)
        {
            if (now < due.AddSeconds(-ScraplineAhead) || now > due.AddSeconds(ScraplineLate))
                continue;
            // Not for whoever Voretooth is on: it follows them, and the circle with it, onto the toons that did step out.
            if (this.World.Actors.Find(id) is { IsDead: false } caster && caster.TargetID != player.InstanceID)
                hints.AddForbiddenZone(ScraplineShape, caster.Position, default, due);
        }
    }
}
