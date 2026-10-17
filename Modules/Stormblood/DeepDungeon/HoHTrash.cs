// Heaven-on-High trash: what the cast-bar guess (AutoHints) gets wrong or leaves out. Everything else on the trash floors
// is still the guess, which reads shapes off the game's sheet.
//
// From BossmodReborn's HoHFloorModule (its gazes, rings and their ranges), the game's sheet, and the wiki for the floor
// 1-10 patrols, 2026-10-17. Splatoon has no Heaven-on-High presets. Nothing here has been seen in a recording yet.
//  - Out of reach: the Heavenly Shark's Jaws (floors 1-3) and the Heavenly Amikiri's Shuck (4-6) are a hit on one player,
//    3.5s and 4s, range 25, that the wiki calls one-shots you can outrange. The sheet has no ground shape for them, so the
//    guess drew nothing. The player it is cast at gets out of range: 25 yalms past both hitboxes.
//  - Gazes the guess missed: Nightmarish Light, Shifting Light, Frond Fatale and Hex are too big to dodge and not named
//    like gazes, and Blinding Burst was drawn as a 25y circle to run out of. All five are looked away from within
//    BossmodReborn's range for them.
//  - Rings the guess drew wrong: the Dragon's Voice is an 8-30 ring the sheet calls a 30y circle (so it was dropped as
//    unavoidable), and Atropine Spore's hole is 9.5, not the guessed 8.
//  - Circles that reach past the caster's hitbox (the sheet's CastType 5) were drawn short by it: Cry, Horrisonous Blast.
//  - Blanket Thunder (Heavenly Yumemi, floors 7-9) is a 30y circle round the caster: exactly the size the guess drops as a
//    raidwide. 4s is enough to get out of it.
// Not done: interrupts and stuns (Malice, Cry, Horrisonous Blast, Northerlies; Minerva presses no buttons), and line of
// sight behind walls. The shark's dash is not on the sheet as anything but Jaws; a recording of floors 1-3 would say
// whether there is a hit with no cast bar.
using System;
using System.Collections.Generic;
using Minerva;
using Minerva.Automation;

namespace Minerva.Stormblood.DeepDungeon.HeavenOnHigh.Trash;

public enum AID : uint
{
    Jaws = 11981, // Heavenly Shark->player, 3.5s cast, single-target, range 25
    Shuck = 11982, // Heavenly Amikiri->player, 4.0s cast, single-target, range 25
    BlanketThunder = 11984, // Heavenly Yumemi->self, 4.0s cast, range 30 circle
    BlindingBurst = 12174, // ->self, 3.0s cast, range 25 circle, gaze
    NightmarishLight = 12322, // ->self, 4.0s cast, range 30+R circle, gaze
    ShiftingLight = 12357, // ->self, 3.0s cast, range 30+R circle, gaze
    Cry = 12350, // ->self, 5.0s cast, range 12+R circle
    AtropineSpore = 12415, // ->self, 4.0s cast, range 9.5-41 donut
    FrondFatale = 12416, // ->self, 3.0s cast, range 40 circle, gaze
    TheDragonsVoice = 12432, // ->self, 4.5s cast, range 8-30 donut
    Hex = 12442, // ->self, 4.0s cast, range 30 circle, gaze
    HorrisonousBlast = 12258, // ->self, 4.0s cast, range 16+R circle
}

public abstract class HoHTrash : ZoneModule
{
    /// <summary>How far each gaze reaches, from BossmodReborn: inside it, looking at the caster when it lands is the hit.</summary>
    private static readonly Dictionary<uint, float> Gazes = new()
    {
        [(uint)AID.BlindingBurst] = 25f,
        [(uint)AID.NightmarishLight] = 35f,
        [(uint)AID.ShiftingLight] = 35f,
        [(uint)AID.FrondFatale] = 40f,
        [(uint)AID.Hex] = 30f,
    };

    /// <summary>Hits on one player that miss a player out of their range, which is the sheet's range past both hitboxes.</summary>
    private static readonly Dictionary<uint, float> OutOfReach = new()
    {
        [(uint)AID.Jaws] = 25f,
        [(uint)AID.Shuck] = 25f,
    };

    private static readonly IReadOnlyDictionary<uint, CastCorrections.Entry> Table = new Dictionary<uint, CastCorrections.Entry>
    {
        [(uint)AID.BlanketThunder] = new(_ => new AOEShapeCircle(30f)),
        [(uint)AID.Cry] = new(a => new AOEShapeCircle(12f + a.HitboxRadius)),
        [(uint)AID.HorrisonousBlast] = new(a => new AOEShapeCircle(16f + a.HitboxRadius)),
        [(uint)AID.AtropineSpore] = new(_ => new AOEShapeDonut(9.5f, 41f)),
        [(uint)AID.TheDragonsVoice] = new(_ => new AOEShapeDonut(8f, 30f)),
    };

    private readonly record struct Cast(ulong Caster, ulong Target, uint Action, DateTime Lands);

    private readonly CastCorrections casts;
    private readonly List<Cast> watched = [];
    private readonly EventSubscriptions subscriptions;

    protected HoHTrash(WorldState ws) : base(ws)
    {
        this.casts = new CastCorrections(ws, Table);
        this.subscriptions = new(
            ws.Actors.CastStarted.Subscribe(actor =>
            {
                if (actor.CastInfo is { } cast && (Gazes.ContainsKey(cast.Action.ID) || OutOfReach.ContainsKey(cast.Action.ID)))
                {
                    this.watched.RemoveAll(c => c.Caster == actor.InstanceID);
                    this.watched.Add(new Cast(actor.InstanceID, cast.TargetID, cast.Action.ID, ws.FutureTime(cast.RemainingTime)));
                }
            }),
            ws.Actors.CastFinished.Subscribe((actor, _) => this.watched.RemoveAll(c => c.Caster == actor.InstanceID)),
            ws.Actors.Removed.Subscribe(actor => this.watched.RemoveAll(c => c.Caster == actor.InstanceID)));
    }

    protected override void Dispose(bool disposing)
    {
        this.casts.Dispose();
        this.subscriptions.Dispose();
        base.Dispose(disposing);
    }

    // Blinding Burst is the one gaze the guess draws (a 25y circle): leaving it means running 25 yalms instead of turning
    public override bool ReplacesGuess(uint action) => this.casts.Replaces(action) || action == (uint)AID.BlindingBurst;

    public override void CalculateAIHints(int playerSlot, Actor player, AIHints hints)
    {
        this.casts.AddForbiddenZones(hints);
        foreach (var c in this.watched)
        {
            if (World.Actors.Find(c.Caster) is not { IsDeadOrDestroyed: false } caster)
                continue;
            if (OutOfReach.TryGetValue(c.Action, out var range))
            {
                if (c.Target == player.InstanceID)
                    hints.AddForbiddenZone(new AOEShapeCircle(range + caster.HitboxRadius + player.HitboxRadius), caster.Position, default, c.Lands);
            }
            else if (Gazes.TryGetValue(c.Action, out var reach))
            {
                var toCaster = caster.Position - player.Position;
                if (toCaster.LengthSq() <= reach * reach && toCaster.LengthSq() > 0.01f)
                    hints.ForbiddenDirections.Add((Angle.FromDirection(toCaster), 45f.Degrees(), c.Lands));
            }
        }
    }
}

[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 540)]
public sealed class HoH10Trash(WorldState ws) : HoHTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 541)]
public sealed class HoH20Trash(WorldState ws) : HoHTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 542)]
public sealed class HoH30Trash(WorldState ws) : HoHTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 543)]
public sealed class HoH40Trash(WorldState ws) : HoHTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 544)]
public sealed class HoH50Trash(WorldState ws) : HoHTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 545)]
public sealed class HoH60Trash(WorldState ws) : HoHTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 546)]
public sealed class HoH70Trash(WorldState ws) : HoHTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 547)]
public sealed class HoH80Trash(WorldState ws) : HoHTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 548)]
public sealed class HoH90Trash(WorldState ws) : HoHTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 549)]
public sealed class HoH100Trash(WorldState ws) : HoHTrash(ws);
