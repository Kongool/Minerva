// Eureka Anemos: what the cast-bar guess (AutoHints) gets wrong. Everything else in the zone is still the guess, which
// reads shapes off the game's sheet. BossmodReborn has no Anemos fights (its Anemos.cs is the NM farm list only).
//  - The Void Garm's Dragon's Voice is an 8-30 ring the sheet calls a 30y circle (CastType 2, no omen), so the guess
//    dropped it as unavoidable and the dodge never moved. Number, 2026-11-16, from each toon's own effect ring and HP:
//    hit at 8.5y and 16.3-16.7y, missed at 6.2, 6.7 and 6.9y; all five it hit were paralysed. The hole is between 6.9
//    and 8.5; 8 is Heaven-on-High's Dragon's Voice (HoHTrash) and every Dragon's Voice in BossmodReborn.
using System.Collections.Generic;
using Minerva;
using Minerva.Automation;

namespace Minerva.Stormblood.Foray.Anemos;

public enum AID : uint
{
    TheDragonsVoice = 10965, // Void Garm->self, 5.0s cast, range 8-30 donut
}

[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 283)]
public sealed class AnemosField : ZoneModule
{
    private static readonly IReadOnlyDictionary<uint, CastCorrections.Entry> Table = new Dictionary<uint, CastCorrections.Entry>
    {
        [(uint)AID.TheDragonsVoice] = new(_ => new AOEShapeDonut(8f, 30f)),
    };

    private readonly CastCorrections casts;

    public AnemosField(WorldState ws) : base(ws) => this.casts = new CastCorrections(ws, Table);

    protected override void Dispose(bool disposing)
    {
        this.casts.Dispose();
        base.Dispose(disposing);
    }

    public override bool ReplacesGuess(uint action) => this.casts.Replaces(action);

    public override void CalculateAIHints(int playerSlot, Actor player, AIHints hints) => this.casts.AddForbiddenZones(hints);
}
