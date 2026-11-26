// Palace of the Dead trash: what the cast-bar guess (AutoHints) gets wrong or draws too small. Everything else on the
// trash floors is still the guess, which reads shapes off the game's sheet.
//
// From Nagumo's Splatoon presets against the sheet, 2026-11-24. BossmodReborn's PalaceFloorModule covers none of these.
// Its gazes are the other half: Stone Gaze the guess already treats as one by its name, and its 8y cone form is drawn
// and left. Mysterious Light (6953, 3s, damage on whoever looks at the caster within 30y) is neither named like a gaze
// nor small enough to draw, so nothing was done about it until 2026-11-26: looked away from now, as Heaven-on-High's.
// Nothing here has been seen in a recording.
//  - The Dragon's Voice (81-89 and 181-189) is a ring the sheet calls a 30y circle, so the guess dropped it as a
//    raidwide and drew nothing. The presets put its hole at 3y past the mob's edge.
//  - Evil Eye (191-199) is a circle 8y past the mob's edge, left or looked away from; the guess drew a 90-degree cone.
//  - Cones the sheet gives no angle for, guessed at 90 degrees and 120 in the presets: Ripper Claw, Level 5 Petrify and
//    Level 5 Death, which kills.
//  - Eye of the Beholder (71-79) is a ring hugging the mob: the preset and the same author's KodakkuAssist script both
//    draw one (hole about 4y, edge 15-16y). The sheet has no shape for it and the guess drew a 90-degree cone.
// Not done:
//  - Interrupts, stuns and the spikes BossmodReborn handles (Minerva presses no buttons).
//  - The 171-179 ape's Chest Thump. By the author's script: out of combat it casts Ripe Banana (6973, 1.7s), gains a
//    damage buff (status 53) for about 15s and thumps (7063, no cast, 50y past its edge) over and over until the buff
//    ends or it is fought. Eureka Orthos's version is hidden from behind a wall; nothing is drawn for this one yet.
using System;
using System.Collections.Generic;
using Minerva;
using Minerva.Automation;

namespace Minerva.Heavensward.DeepDungeon.PalaceOfTheDead.Trash;

public enum AID : uint
{
    RipperClaw = 6394, // 41-49 and 141-149
    Level5Petrify = 7031, // 131-139
    Level5Death = 7084, // 191-199
    EvilEye = 7043, // 191-199
    TheDragonsVoice = 7079, // 81-89 and 181-189
    EyeOfTheBeholder = 6972, // 71-79
    MysteriousLight = 6953, // ->self, 3.0s cast, range 30 circle, gaze
}

public abstract class PalaceTrash : ZoneModule
{
    private static readonly IReadOnlyDictionary<uint, CastCorrections.Entry> Table = new Dictionary<uint, CastCorrections.Entry>
    {
        [(uint)AID.RipperClaw] = new(a => new AOEShapeCone(5f + a.HitboxRadius, 60f.Degrees())),
        [(uint)AID.Level5Petrify] = new(a => new AOEShapeCone(6f + a.HitboxRadius, 60f.Degrees())),
        [(uint)AID.Level5Death] = new(a => new AOEShapeCone(6f + a.HitboxRadius, 60f.Degrees())),
        [(uint)AID.EvilEye] = new(a => new AOEShapeCircle(8f + a.HitboxRadius)),
        [(uint)AID.TheDragonsVoice] = new(a => new AOEShapeDonut(3f + a.HitboxRadius, 30f + a.HitboxRadius)),
        [(uint)AID.EyeOfTheBeholder] = new(a => new AOEShapeDonut(1f + a.HitboxRadius, 16f + a.HitboxRadius)),
    };

    /// <summary>How far each gaze reaches past the caster's edge: inside it, looking at the caster when it lands is the hit.</summary>
    private static readonly Dictionary<uint, float> Gazes = new()
    {
        [(uint)AID.MysteriousLight] = 30f,
    };

    private readonly record struct GazeCast(ulong Caster, uint Action, DateTime Lands);

    private readonly CastCorrections casts;
    private readonly List<GazeCast> watched = [];
    private readonly EventSubscriptions subscriptions;

    protected PalaceTrash(WorldState ws) : base(ws)
    {
        this.casts = new CastCorrections(ws, Table);
        this.subscriptions = new(
            ws.Actors.CastStarted.Subscribe(actor =>
            {
                if (actor.CastInfo is { } cast && Gazes.ContainsKey(cast.Action.ID))
                {
                    this.watched.RemoveAll(c => c.Caster == actor.InstanceID);
                    this.watched.Add(new GazeCast(actor.InstanceID, cast.Action.ID, ws.FutureTime(cast.RemainingTime)));
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

    public override bool ReplacesGuess(uint action) => this.casts.Replaces(action) || Gazes.ContainsKey(action);

    public override void CalculateAIHints(int playerSlot, Actor player, AIHints hints)
    {
        this.casts.AddForbiddenZones(hints);
        foreach (var c in this.watched)
        {
            if (World.Actors.Find(c.Caster) is not { IsDeadOrDestroyed: false } caster)
                continue;
            var reach = Gazes[c.Action] + caster.HitboxRadius;
            var toCaster = caster.Position - player.Position;
            if (toCaster.LengthSq() <= reach * reach && toCaster.LengthSq() > 0.01f)
                hints.ForbiddenDirections.Add((Angle.FromDirection(toCaster), 45f.Degrees(), c.Lands));
        }
    }
}

[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 174)]
public sealed class Palace10Trash(WorldState ws) : PalaceTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 175)]
public sealed class Palace20Trash(WorldState ws) : PalaceTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 176)]
public sealed class Palace30Trash(WorldState ws) : PalaceTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 177)]
public sealed class Palace40Trash(WorldState ws) : PalaceTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 178)]
public sealed class Palace50Trash(WorldState ws) : PalaceTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 204)]
public sealed class Palace60Trash(WorldState ws) : PalaceTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 205)]
public sealed class Palace70Trash(WorldState ws) : PalaceTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 206)]
public sealed class Palace80Trash(WorldState ws) : PalaceTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 207)]
public sealed class Palace90Trash(WorldState ws) : PalaceTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 208)]
public sealed class Palace100Trash(WorldState ws) : PalaceTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 209)]
public sealed class Palace110Trash(WorldState ws) : PalaceTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 210)]
public sealed class Palace120Trash(WorldState ws) : PalaceTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 211)]
public sealed class Palace130Trash(WorldState ws) : PalaceTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 212)]
public sealed class Palace140Trash(WorldState ws) : PalaceTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 213)]
public sealed class Palace150Trash(WorldState ws) : PalaceTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 214)]
public sealed class Palace160Trash(WorldState ws) : PalaceTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 215)]
public sealed class Palace170Trash(WorldState ws) : PalaceTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 216)]
public sealed class Palace180Trash(WorldState ws) : PalaceTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 217)]
public sealed class Palace190Trash(WorldState ws) : PalaceTrash(ws);
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 218)]
public sealed class Palace200Trash(WorldState ws) : PalaceTrash(ws);
