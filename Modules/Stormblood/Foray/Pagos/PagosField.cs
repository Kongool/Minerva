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
//    - Heavy Scrapline (11886, an 11y circle on Voretooth) has no cast bar; it landed every 32.5-33s and is not drawn.
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

    private readonly CastCorrections casts;

    public PagosField(WorldState ws) : base(ws) => this.casts = new CastCorrections(ws, Table);

    protected override void Dispose(bool disposing)
    {
        this.casts.Dispose();
        base.Dispose(disposing);
    }

    public override bool ReplacesGuess(uint action) => this.casts.Replaces(action);

    public override void CalculateAIHints(int playerSlot, Actor player, AIHints hints) => this.casts.AddForbiddenZones(hints);
}
