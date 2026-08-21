// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Dungeon.D07TenderValley.D074GreatestSerpentOfTural;

public enum OID : uint
{
    Boss = 0x4164, // R4.5
    LesserSerpentOfTural = 0x41DE, // R2.812
    GreatSerpentOfTural = 0x41E0, // R1.152-3.84
    SludgeVoidzone1 = 0x1EBA86, // R0.5
    SludgeVoidzone2 = 0x1EBA87, // R0.5
    SludgeVoidzone3 = 0x1EBA88, // R0.5
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target
    Teleport = 36747, // Boss->location, no cast, single-target

    DubiousTulidisaster = 36748, // Boss->self, 5.0s cast, range 40 circle

    BouncyCouncil = 36746, // Boss->self, 3.0s cast, single-target, spawns clones

    MisplacedMystery = 36750, // LesserSerpentOfTural->self, 7.0s cast, range 52 width 5 rect
    ExaltedWobble = 36749, // LesserSerpentOfTural->self, 7.0s cast, range 9 circle

    ScreesOfFuryVisual = 36744, // Boss->self, 4.5+0.5s cast, single-target, AOE tankbuster
    ScreesOfFury = 36757, // Helper->player, no cast, range 3 circle 

    GreatestLabyrinth = 36745, // Boss->self, 4.0s cast, range 40 circle

    MoistSummoning = 36743, // Boss->self, 3.0s cast, single-target, spawns great serpent of tural
    MightyBlorpVisual1 = 36753, // GreatSerpentOfTural->self, 4.5+0.5s cast, single-target, stack
    MightyBlorpVisual2 = 36752, // GreatSerpentOfTural->self, 4.5+0.5s cast, single-target, stack
    MightyBlorpVisual3 = 36751, // GreatSerpentOfTural->self, 4.5+0.5s cast, single-target, stack
    MightyBlorp1 = 39983, // GreatSerpentOfTural->players, no cast, range 6 circle
    MightyBlorp2 = 39982, // GreatSerpentOfTural->players, no cast, range 5 circle
    MightyBlorp3 = 39981, // GreatSerpentOfTural->players, no cast, range 4 circle

    GreatestFloodVisual = 36742, // Boss->self, 5.0s cast, single-target
    GreatestFlood = 36756, // Helper->self, 6.0s cast, range 40 circle, knockback 15, away from source

    GreatTorrentVisual = 36741, // Boss->self, 3.0s cast, single-target
    GreatTorrentAOE = 36754, // Helper->location, 6.0s cast, range 6 circle 
    GreatTorrentSpread = 36755 // Helper->player, no cast, range 6 circle
}

public enum IconID : uint
{
    ScreesOfFury = 341, // player
    MightyBlorp1 = 62, // player
    MightyBlorp2 = 542, // player
    MightyBlorp3 = 543, // player
    GreatTorrent = 139 // player
}

sealed class DubiousTulidisasterArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCustom square = new([new Square(D074GreatestSerpentOfTural.ArenaCenter, 15f)], [new Square(D074GreatestSerpentOfTural.ArenaCenter, 12f)]);
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.DubiousTulidisaster && Module.Bounds.Radius > 12f)
        {
            _aoe = [new(square, Module.Center, default, Module.CastFinishAt(spell, 4.8d))];
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x00 && state == 0x00020001u)
        {
            Module.Bounds = new ArenaBoundsSquare(12f);
            _aoe = [];
        }
    }
}

sealed class ScreesOfFury(ModuleBase module) : Components.BaitAwayIcon(module, 3f, (uint)IconID.ScreesOfFury, (uint)AID.ScreesOfFury, 5.3d, tankbuster: true);

sealed class GreatestFlood(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.GreatestFlood, 15f)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
        {
            ref readonly var c = ref Casters.Ref(0);
            var act = c.Activation;
            if (!IsImmune(slot, act))
            {
                hints.AddForbiddenZone(new SDKnockbackInAABBSquareAwayFromOrigin(Module.Center, c.Origin, 15f, 11f), act);
            }
        }
    }
}

sealed class GreatestLabyrinth(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly Square middle = new(D074GreatestSerpentOfTural.ArenaCenter, 4f);
    private const float Radius = 2f;

    private static readonly (Square correctTile, Square goalTile)[] tilePairs = [
        (new(new(-124f, -552f), Radius), new(new(-140f, -564f), Radius)),
        (new(new(-128f, -560f), Radius), new(new(-120f, -544f), Radius)),
        (new(new(-132f, -548f), Radius), new(new(-120f, -564f), Radius)),
        (new(new(-136f, -556f), Radius), new(new(-140f, -544f), Radius))];

    private static readonly Shape[] wholeArena = [new Square(D074GreatestSerpentOfTural.ArenaCenter, 12f)];

    private static readonly AOEShapeCustom[] forbiddenShapes = InitializeShapes();
    private static readonly AOEShapeCustom[] safeShapes = InitializeShapes(true);

    private static AOEShapeCustom[] InitializeShapes(bool invert = false)
    {
        var shapes = new AOEShapeCustom[4];
        for (var i = 0; i < 4; ++i)
        {
            var tp = tilePairs[i];
            shapes[i] = !invert ? new AOEShapeCustom(wholeArena, [middle, tp.correctTile, tp.goalTile]) : new AOEShapeCustom([tp.correctTile, tp.goalTile], invertForbiddenZone: true);
        }
        return shapes;
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnMapEffect(byte index, uint state)
    {
        if (index != 0x01)
        {
            return;
        }

        void AddAOEs(int index)
        {
            _aoes.Add(new(forbiddenShapes[index], Module.Center));
            _aoes.Add(new(safeShapes[index], Module.Center, default, World.FutureTime(10d), Colors.SafeFromAOE));
        }
        switch (state)
        {
            case 0x01000080u:
                AddAOEs(0);
                break;
            case 0x04000200u:
                AddAOEs(1);
                break;
            case 0x10000800u:
                AddAOEs(2);
                break;
            case 0x00020001u:
                AddAOEs(3);
                break;
            case 0x00100004u or 0x00200004u or 0x00400004u or 0x00080004u:
                _aoes.Clear();
                break;
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (_aoes.Count == 0)
            return;
        hints.Add("Walk onto safe square!", !_aoes[1].Check(actor.Position));
    }
}

abstract class MightyBlorp(ModuleBase module, uint iconID, uint aid, float radius) : Components.StackWithIcon(module, iconID, aid, radius, 4.6f, 4, 4);
sealed class MightyBlorp1(ModuleBase module) : MightyBlorp(module, (uint)IconID.MightyBlorp1, (uint)AID.MightyBlorp1, 6f);
sealed class MightyBlorp2(ModuleBase module) : MightyBlorp(module, (uint)IconID.MightyBlorp2, (uint)AID.MightyBlorp2, 5f);
sealed class MightyBlorp3(ModuleBase module) : MightyBlorp(module, (uint)IconID.MightyBlorp3, (uint)AID.MightyBlorp3, 4f);

abstract class SludgeVoidzone(ModuleBase module, float radius, uint oid) : Components.Voidzone(module, radius, m => GetVoidzones(m, oid))
{
    private static Actor[] GetVoidzones(ModuleBase module, uint oid)
    {
        var enemies = module.Enemies(oid);
        if (enemies.Count != 0 && enemies[0].EventState != 7)
            return [enemies[0]];
        return [];
    }
}

sealed class SludgeVoidzone1(ModuleBase module) : SludgeVoidzone(module, 6f, (uint)OID.SludgeVoidzone1);
sealed class SludgeVoidzone2(ModuleBase module) : SludgeVoidzone(module, 5f, (uint)OID.SludgeVoidzone2);
sealed class SludgeVoidzone3(ModuleBase module) : SludgeVoidzone(module, 4f, (uint)OID.SludgeVoidzone3);

sealed class DubiousTulidisasterGreatestLabyrinthFlood(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.DubiousTulidisaster, (uint)AID.GreatestLabyrinth, (uint)AID.GreatestFlood]);
sealed class ExaltedWobble(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ExaltedWobble, 9f);
sealed class MisplacedMystery(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MisplacedMystery, new AOEShapeRect(52f, 2.5f));
sealed class GreatTorrent(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GreatTorrentAOE, 6f, 10);
sealed class GreatTorrentSpread(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.GreatTorrent, (uint)AID.GreatTorrentSpread, 6f, 5.1d);

sealed class D074GreatestSerpentOfTuralStates : StateMachineBuilder
{
    public D074GreatestSerpentOfTuralStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<DubiousTulidisasterArenaChange>()
            .ActivateOnEnter<DubiousTulidisasterGreatestLabyrinthFlood>()
            .ActivateOnEnter<ScreesOfFury>()
            .ActivateOnEnter<MightyBlorp1>()
            .ActivateOnEnter<MightyBlorp2>()
            .ActivateOnEnter<MightyBlorp3>()
            .ActivateOnEnter<SludgeVoidzone1>()
            .ActivateOnEnter<SludgeVoidzone2>()
            .ActivateOnEnter<SludgeVoidzone3>()
            .ActivateOnEnter<GreatestFlood>()
            .ActivateOnEnter<GreatestLabyrinth>()
            .ActivateOnEnter<ExaltedWobble>()
            .ActivateOnEnter<MisplacedMystery>()
            .ActivateOnEnter<GreatTorrent>()
            .ActivateOnEnter<GreatTorrentSpread>();
    }
}

[ModuleInfo(CFCID = 834u, NameID = 12709u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class D074GreatestSerpentOfTural(WorldState ws, Actor primary) : ModuleBase(ws, primary, ArenaCenter, new ArenaBoundsSquare(14.5f))
{
    public static readonly WPos ArenaCenter = new(-130f, -554f);
}
