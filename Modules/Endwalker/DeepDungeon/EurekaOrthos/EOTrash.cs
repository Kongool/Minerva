// Eureka Orthos trash: the casts a wall stops, and a few shapes the cast-bar guess (AutoHints) draws too small.
// Everything else on the trash floors is still the guess, which reads shapes off the game's sheet.
//
// Too small: the sheet gives no angle for three gaze cones and no hole for four rings, and the guess says 90 degrees
// and 8y. Nagumo's Splatoon presets say 100-120 degrees and holes of 6-7.5y; drawn at theirs, with their wider outer
// edge where they have one. A too-generous hole on the 31-39 chimera's Dragon's Voice has killed a toon before.
//
// These are long casts over a circle too wide to walk out of, which the guess leaves alone as raidwides. Each is drawn as
// its circle less the ground a wall hides from the caster (the game is asked, live only), so the dodge walks behind a
// wall or out of reach, whichever is nearer. Which casts: BossmodReborn's EOFloorModule (its AddLOS list) and Nagumo's
// Splatoon presets (the ones labelled "hide behind a wall"), 2026-11-24, with the sheet's range for each. The user
// confirms the same of Pilgrim's Traverse's Lahar, where it was seen. Nothing here has been seen in a recording.
// Not done:
//  - Interrupts and stuns (Quake, High Voltage, Winds of Winter can be stopped; Minerva presses no buttons).
//  - BossmodReborn's gazes not named like gazes (Allagan Fear, Catharsis as a 40y circle to leave), Electric Whorl as
//    an 8-60 ring, and the spikes of Gelid Charge and Smoldering Scales.
//  - Second hits with no cast bar (the Killing Paw's swipe, Sewer Water's other half, Headspin), and the 93-98
//    chimera's breaths and the 86-89 Sweeps, which the presets draw off-centre.
//  - Beastly Roar and Unholy (floors 91-98): 30y circles cast out of combat, by the presets. The guess drops them as
//    raidwides. Their cast times are not known, and whether a wall stops them is not either.
using System.Collections.Generic;
using Minerva;
using Minerva.Automation;

namespace Minerva.Endwalker.DeepDungeon.EurekaOrthos.Trash;

public enum AID : uint
{
    EclipticMeteor = 33043, // floors 1-9: 12s cast, 50y
    SelfDetonate = 32410, // 21-29: 10s cast, 40y
    Explosion = 32452, // 41-49: 7s cast, 60y
    Quake = 32470, // 41-49: 5s cast, 30y, can be interrupted
    AbyssalCry = 32467, // 41-49: 6s cast, 30y
    AquaSpear = 32500, // 5.5s cast, a charge 5y wide; BossmodReborn hides from it within 50y
    WindsOfWinter1 = 32785, // 71-79: 40y; a wall or a stun, by the presets
    WindsOfWinter2 = 33194,
    RipeBanana = 33195, // 76-79: a 2s buff, then Chest Thump
    ChestThump = 32680, // 2s cast, 50y
    HighVoltage = 32878, // 91-98: 7.5s cast, 30y, can be interrupted
    Genesis1 = 32708, // one of the three special mobs: 80y
    Genesis2 = 32709,
    Genesis3 = 32710,
    ColdGaze = 32427, // 21-29: 100-degree cone by Splatoon
    CursedGaze = 32448, // 31-39: 120
    StoneGaze = 32967, // 61-69: 120
    TheDragonsVoice = 32444, // 31-39: a 7-37 ring by Splatoon, 8-30 by BossmodReborn
    TheDragonsVoice2 = 32910, // 93-98: 6-31
    RingingBurst = 32775, // 86-89: 6-30
    RingCannon = 32876, // 91-98: hole 7.5; the sheet's edge is 40
}

public abstract class EOTrash : ZoneModule
{
    private static readonly IReadOnlyDictionary<uint, SightCasts.Entry> BehindWalls = new Dictionary<uint, SightCasts.Entry>
    {
        [(uint)AID.EclipticMeteor] = new(50f),
        [(uint)AID.SelfDetonate] = new(40f),
        [(uint)AID.Explosion] = new(60f),
        [(uint)AID.Quake] = new(30f),
        [(uint)AID.AbyssalCry] = new(30f),
        [(uint)AID.AquaSpear] = new(50f),
        [(uint)AID.WindsOfWinter1] = new(40f),
        [(uint)AID.WindsOfWinter2] = new(40f),
        // Chest Thump's own cast is two seconds: the banana before it is the time to go
        [(uint)AID.RipeBanana] = new(50f),
        [(uint)AID.ChestThump] = new(50f),
        [(uint)AID.HighVoltage] = new(30f),
        [(uint)AID.Genesis1] = new(80f),
        [(uint)AID.Genesis2] = new(80f),
        [(uint)AID.Genesis3] = new(80f),
    };

    private static readonly IReadOnlyDictionary<uint, CastCorrections.Entry> Table = new Dictionary<uint, CastCorrections.Entry>
    {
        [(uint)AID.ColdGaze] = new(a => new AOEShapeCone(12f + a.HitboxRadius, 50f.Degrees())),
        [(uint)AID.CursedGaze] = new(a => new AOEShapeCone(8f + a.HitboxRadius, 60f.Degrees())),
        [(uint)AID.StoneGaze] = new(a => new AOEShapeCone(10f + a.HitboxRadius, 60f.Degrees())),
        [(uint)AID.TheDragonsVoice] = new(_ => new AOEShapeDonut(7f, 37f)),
        [(uint)AID.TheDragonsVoice2] = new(_ => new AOEShapeDonut(6f, 31f)),
        [(uint)AID.RingingBurst] = new(_ => new AOEShapeDonut(6f, 30f)),
        [(uint)AID.RingCannon] = new(_ => new AOEShapeDonut(7.5f, 40f)),
    };

    private readonly SightCasts sight;
    private readonly CastCorrections casts;

    protected EOTrash(WorldState ws) : base(ws)
    {
        this.sight = new SightCasts(ws, BehindWalls);
        this.casts = new CastCorrections(ws, Table);
    }

    protected override void Dispose(bool disposing)
    {
        this.sight.Dispose();
        this.casts.Dispose();
        base.Dispose(disposing);
    }

    public override bool ReplacesGuess(uint action) => this.sight.Replaces(action) || this.casts.Replaces(action);

    public override void CalculateAIHints(int playerSlot, Actor player, AIHints hints)
    {
        this.sight.AddForbiddenZones(player, hints);
        this.casts.AddForbiddenZones(hints);
    }
}

[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 897)]
public sealed class EO10Trash(WorldState ws) : EOTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 898)]
public sealed class EO20Trash(WorldState ws) : EOTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 899)]
public sealed class EO30Trash(WorldState ws) : EOTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 900)]
public sealed class EO40Trash(WorldState ws) : EOTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 901)]
public sealed class EO50Trash(WorldState ws) : EOTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 902)]
public sealed class EO60Trash(WorldState ws) : EOTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 903)]
public sealed class EO70Trash(WorldState ws) : EOTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 904)]
public sealed class EO80Trash(WorldState ws) : EOTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 905)]
public sealed class EO90Trash(WorldState ws) : EOTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 906)]
public sealed class EO100Trash(WorldState ws) : EOTrash(ws);
