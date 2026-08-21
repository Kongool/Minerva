// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py.
//
// Replaces Minerva's earlier recording-derived draft of this fight (Modules/Dawntrail/Dungeon/TrenoCatoblepas/),
// which predated BMR having a module for it. Facts carried over from that draft, verified against a recording:
//   - Dawntrail dungeon CFC 1064, zone 1314. Boss 'Treno Catoblepas' 0x4841, single instance, 12.2M HP,
//     NameID 14270 — a lightning/petrify beast.
//   - Trust NPC casts are NOT player dangers: a Duty Support recording of this fight yields ~17 ally casts from
//     Y'shtola / Alisaie / Thancred's Avatars (Jolt III, Verstone, Verthunder II, the *OfTheSeventhDawn spells).
//     "Ignore other players" filters real players but not Trust NPCs, so extractor drafts of this duty need them
//     stripped by hand.
//   - OIDs 0x4851/0x4842/0x4843 are add mobs (188K HP, multiple instances), not voidzone puddles.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Dungeon.D12Mistwake.D121TrenoCatoblepas;

public enum OID : uint
{
    TrenoCatoblepas = 0x4841, // R4.5
    BigRock = 0x4851, // R2.0
    MediumRock = 0x4843, // R1.5
    SmallRock = 0x4842, // R1.0
    Helper = 0x233C
}

public enum AID : uint
{
    Thunder = 43328, // TrenoCatoblepas->player, no cast, single-target
    Earthquake = 43327, // TrenoCatoblepas->self, 5.0s cast, range 30 circle, raidwide
    ThunderIIVisual = 43331, // TrenoCatoblepas->self, 3.5+1,5s cast, single-target
    ThunderIIAOE = 43332, // Helper->location, 5.0s cast, range 5 circle
    ThunderIISpread = 43333, // Helper->players, 5.0s cast, range 5 circle
    BedevilingLight = 43330, // TrenoCatoblepas->self, 7.0s cast, range 30 circle
    ThunderIII = 43329, // TrenoCatoblepas->player, 5.0s cast, range 4 circle
    RayOfLightningVisual = 44825, // TrenoCatoblepas->player, 6.0s cast, single-target
    RayOfLightning = 43334, // TrenoCatoblepas->self, no cast, range 50 width 5 rect
    Petribreath = 43335 // TrenoCatoblepas->self, 5.0s cast, range 30 120-degree cone
}

public enum IconID : uint
{
    RayOfLightning = 524 // TrenoCatoblepas->player
}

[SkipLocalsInit]
sealed class ArenaChanges(ModuleBase module) : ModuleComponent(module)
{
    public readonly List<Actor> rocksActors = module.Enemies([(uint)OID.SmallRock, (uint)OID.MediumRock, (uint)OID.BigRock]);

    public override void OnActorDestroyed(Actor actor)
    {
        if (actor.OID is (uint)OID.SmallRock or (uint)OID.MediumRock or (uint)OID.BigRock)
        {
            rocksActors.Remove(actor);
        }
    }

    private readonly List<Polygon> rocks =
    [
            new(new(72.00001f, 357.50009f), 2.5f, 16, 95.096f.Degrees()),
            new(new(84.0001f, 359f), 2f, 16, -4.837f.Degrees()),
            new(new(85.99999f, 359f), 1.5f, 16, 40f.Degrees()),
            new(new(94.00008f, 354.99979f), 2f, 16, 140.886f.Degrees()),
            new(new(97.00001f, 363f), 1.5f, 16, -60f.Degrees()),
            new(new(71.50004f, 370.00009f), 2f, 16, -69.402f.Degrees()),
            new(new(74.49998f, 368.00009f), 2.5f, 16, -39.322f.Degrees()),
            new(new(90.5f, 370f), 1.5f, 16, 60f.Degrees()),
            new(new(93.00002f, 372f), 2f, 16, -70f.Degrees()),
            new(new(74.99999f, 378f), 2.5f, 16, -128.896f.Degrees()),
            new(new(89.50005f, 380f), 2f, 16, 22.477f.Degrees()),
            new(new(71.5f, 383f), 2f, 16, 80f.Degrees()),
            new(new(96.99998f, 383f), 2.5f, 16, 175.832f.Degrees())
        ];

    public override void OnMapEffect(byte index, uint state)
    {
        var pos = index switch
        {
            0x07 => new(72f, 357.5f),
            0x08 => new(84f, 359f),
            0x09 => new(86f, 359f),
            0x0A => new(94f, 355f),
            0x0B => new(97f, 363f),
            0x0C => new(71.5f, 370f),
            0x0D => new(74.5f, 368f),
            0x0E => new(90.5f, 370f),
            0x0F => new(93f, 372f),
            0x10 => new(75f, 378f),
            0x11 => new(89.5f, 380f),
            0x12 => new(71.5f, 383f),
            0x13 => new(97.5f, 383f),
            _ => (WPos)default
        };
        if (pos != default)
        {
            var count = rocks.Count;
            for (var i = 0; i < count; ++i)
            {
                if (rocks[i].Center.AlmostEqual(pos, 1f))
                {
                    rocks.RemoveAt(i);
                    break;
                }
            }
            Module.Bounds = new ArenaBoundsCustom([new Square(D121TrenoCatoblepas.ArenaCenter, 19.5f)], [.. rocks]);
        }
    }
}

[SkipLocalsInit]
// blockersImpassable: the rocks are real collision, not decoration. BossmodReborn leaves this false, but
// across nine recordings and ~20,000 player position samples not one sample falls inside a live rock --
// only inside ground where a rock had already crumbled. Declaring it lets the dodge route around them
// instead of steering through the very thing whose shadow it is heading for.
sealed class BedevilingLight(ModuleBase module) : Components.CastLineOfSightAOE(module, (uint)AID.BedevilingLight, 30f, blockersImpassable: true)
{
    private readonly ArenaChanges arena = module.FindComponent<ArenaChanges>()!;
    public override ReadOnlySpan<Actor> BlockerActors() => CollectionsMarshal.AsSpan(arena.rocksActors);
}

[SkipLocalsInit]
sealed class Earthquake(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Earthquake);

[SkipLocalsInit]
sealed class ThunderIII(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.ThunderIII, 4f, tankbuster: true)
{
    private readonly ArenaChanges arena = module.FindComponent<ArenaChanges>()!;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        if (IsBaitTarget(actor))
        {
            var rocks = arena.rocksActors;
            var count = rocks.Count;
            var act = CurrentBaits.Ref(0).Activation;
            for (var i = 0; i < count; ++i)
            {
                var r = rocks[i];
                hints.AddForbiddenZone(new SDCircle(r.Position, r.HitboxRadius + 4f), act);
            }
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        base.DrawArenaForeground(pcSlot, pc);
        if (!IsBaitTarget(pc))
        {
            return;
        }
        var rocks = arena.rocksActors;
        var count = rocks.Count;
        for (var i = 0; i < count; ++i)
        {
            var a = rocks[i];
            Arena.ZoneCircleOutline(a.Position, a.HitboxRadius);
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (IsBaitTarget(actor))
        {
            hints.Add("Avoid intersecting rock hitboxes!");
        }
    }
}

[SkipLocalsInit]
sealed class RayOfLightning(ModuleBase module) : Components.LineStack(module, iconID: (uint)IconID.RayOfLightning, (uint)AID.RayOfLightning, 6.2d, 50f, 2.5f, 4, 4)
{
    private readonly ArenaChanges arena = module.FindComponent<ArenaChanges>()!;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        if (IsBaitTarget(actor))
        {
            var rocks = arena.rocksActors;
            var count = rocks.Count;
            ref var b = ref CurrentBaits.Ref(0);
            for (var i = 0; i < count; ++i)
            {
                var r = rocks[i];
                hints.AddForbiddenZone(new SDCone(b.Source.Position, 100f, b.Source.AngleTo(r), Angle.Asin((2.5f + r.HitboxRadius) / (r.Position - b.Source.Position).Length())), b.Activation);
            }

            // Forbidding the rock cones leaves several legal lanes, and the dodge will take whichever is
            // nearest — which scatters the stack around the boss between pulls and can walk it into the next
            // boulder as the field changes. Bias south so the bait lands somewhere predictable; this only
            // scores positions, so if south is genuinely blocked the forbidden zones still win.
            var southOfBoss = b.Source.Position.Z;
            hints.GoalZones.Add(p => p.Z > southOfBoss ? 1f : 0f);
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        base.DrawArenaForeground(pcSlot, pc);
        if (!IsBaitTarget(pc))
        {
            return;
        }
        var rocks = arena.rocksActors;
        var count = rocks.Count;
        for (var i = 0; i < count; ++i)
        {
            var a = rocks[i];
            Arena.ZoneCircleOutline(a.Position, a.HitboxRadius);
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (IsBaitTarget(actor))
        {
            hints.Add("Avoid intersecting rock hitboxes!");
        }
    }
}

[SkipLocalsInit]
sealed class ThunderIIAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThunderIIAOE, 5f);

[SkipLocalsInit]
sealed class ThunderIISpread(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.ThunderIISpread, 5f)
{
    private readonly ThunderIIAOE aoe = module.FindComponent<ThunderIIAOE>()!;
    private readonly ArenaChanges arena = module.FindComponent<ArenaChanges>()!;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        if (IsSpreadTarget(actor))
        {
            var rocks = arena.rocksActors;
            var count = rocks.Count;
            var act = Spreads.Ref(0).Activation;
            var aoes = aoe.ActiveAOEs(slot, actor);
            var len = aoes.Length;
            for (var i = 0; i < count; ++i)
            {
                var r = rocks[i];
                var pos = r.Position;
                for (var j = 0; j < len; ++j)
                {
                    if (aoes[j].Check(pos))
                    {
                        goto skip;
                    }
                }
                hints.AddForbiddenZone(new SDCircle(r.Position, r.HitboxRadius + 5f), act);
            skip:
                ;
            }
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        base.DrawArenaForeground(pcSlot, pc);
        if (!IsSpreadTarget(pc))
        {
            return;
        }
        var rocks = arena.rocksActors;
        var count = rocks.Count;
        var aoes = aoe.ActiveAOEs(pcSlot, pc);
        var len = aoes.Length;
        for (var i = 0; i < count; ++i)
        {
            var r = rocks[i];
            var pos = r.Position;
            for (var j = 0; j < len; ++j)
            {
                if (aoes[j].Check(pos))
                {
                    goto skip;
                }
            }
            Arena.ZoneCircleOutline(pos, r.HitboxRadius);
        skip:
            ;
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (IsSpreadTarget(actor))
        {
            hints.Add("Avoid intersecting safe rock hitboxes!");
        }
    }
}

[SkipLocalsInit]
sealed class Petribreath(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Petribreath, new AOEShapeCone(30f, 60f.Degrees()));

[SkipLocalsInit]
sealed class D121TrenoCatoblepasStates : StateMachineBuilder
{
    public D121TrenoCatoblepasStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChanges>()
            .ActivateOnEnter<Earthquake>()
            .ActivateOnEnter<ThunderIII>()
            .ActivateOnEnter<RayOfLightning>()
            .ActivateOnEnter<ThunderIIAOE>()
            .ActivateOnEnter<ThunderIISpread>()
            .ActivateOnEnter<Petribreath>()
            .ActivateOnEnter<BedevilingLight>();
    }
}

[ModuleInfo(CFCID = 1064u, NameID = 14270u, PrimaryActorOID = (uint)OID.TrenoCatoblepas, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
[SkipLocalsInit]
public sealed class D121TrenoCatoblepas : ModuleBase
{
    public D121TrenoCatoblepas(WorldState ws, Actor primary) : this(ws, primary, BuildArena()) { }

    public static readonly WPos ArenaCenter = new(84f, 370f);

    private D121TrenoCatoblepas(WorldState ws, Actor primary, (WPos center, ArenaBoundsCustom arena) a) : base(ws, primary, a.center, a.arena) { }

    private static (WPos center, ArenaBoundsCustom arena) BuildArena()
    {
        Polygon[] rocks =
        [
            new(new(72.00001f, 357.50009f), 2.5f, 16, 95.096f.Degrees()),
            new(new(84.0001f, 359f), 2f, 16, -4.837f.Degrees()),
            new(new(85.99999f, 359f), 1.5f, 16, 40f.Degrees()),
            new(new(94.00008f, 354.99979f), 2f, 16, 140.886f.Degrees()),
            new(new(97.00001f, 363f), 1.5f, 16, -60f.Degrees()),
            new(new(71.50004f, 370.00009f), 2f, 16, -69.402f.Degrees()),
            new(new(74.49998f, 368.00009f), 2.5f, 16, -39.322f.Degrees()),
            new(new(90.5f, 370f), 1.5f, 16, 60f.Degrees()),
            new(new(93.00002f, 372f), 2f, 16, -70f.Degrees()),
            new(new(74.99999f, 378f), 2.5f, 16, -128.896f.Degrees()),
            new(new(89.50005f, 380f), 2f, 16, 22.477f.Degrees()),
            new(new(71.5f, 383f), 2f, 16, 80f.Degrees()),
            new(new(96.99998f, 383f), 2.5f, 16, 175.832f.Degrees())
        ];
        var arena = new ArenaBoundsCustom([new Square(ArenaCenter, 19.5f)], rocks);
        return (arena.Center, arena);
    }
}
