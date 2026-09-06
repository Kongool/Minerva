// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Dungeon.D11LapisManalis.D113Cagnazzo;

public enum OID : uint
{
    Cagnazzo = 0x3AE2, //R=8.0
    FearsomeFlotsam = 0x3AE3, //R=2.4
    Helper2 = 0x3E97, // R2.7
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 870, // Cagnazzo->player, no cast, single-target
    Teleport = 31131, // Cagnazzo->location, no cast, single-target, Cagnazzo teleports 

    StygianDeluge = 31139, // Cagnazzo->self, 5.0s cast, range 80 circle
    AntediluvianVisual = 31119, // Cagnazzo->self, 5.0s cast, single-target
    Antediluvian = 31120, // Helper->self, 6.5s cast, range 15 circle
    BodySlamVisual = 31121, // Cagnazzo->location, 6.5s cast, single-target
    BodySlamKB = 31122, // Helper->self, 7.5s cast, range 60 circle, knockback 10, away from source
    BodySlam = 31123, // Helper->self, 7.5s cast, range 8 circle

    HydrobombTelegraph = 32695, // Helper->location, 2.0s cast, range 4 circle
    HydraulicRamTelegraph = 32693, // Helper->location, 2.0s cast, width 8 rect charge
    HydraulicRamVisual = 32692, // Cagnazzo->self, 6.0s cast, single-target
    HydraulicRam = 32694, // Cagnazzo->location, no cast, width 8 rect charge
    Hydrobomb = 32696, // Helper->location, no cast, range 4 circle
    StartHydrofall = 31126, // Cagnazzo->self, no cast, single-target
    Hydrofall = 31375, // Cagnazzo->self, 5.0s cast, single-target
    Hydrofall2 = 31376, // Helper->players, 5.5s cast, range 6 circle
    CursedTide = 31130, // Cagnazzo->self, 5.0s cast, single-target
    StartLimitbreakPhase = 31132, // Cagnazzo->self, no cast, single-target
    NeapTide = 31134, // Helper->player, no cast, range 6 circle
    Hydrovent = 31136, // Helper->location, 5.0s cast, range 6 circle
    SpringTide = 31135, // Helper->players, no cast, range 6 circle
    Tsunami = 31137, // Helper->self, no cast, range 80 width 60 rect
    TsunamiEnrage = 31138, // Helper->self, no cast, range 80 width 60 rect
    VoidcleaverVisual = 31110, // Cagnazzo->self, 4.0s cast, single-target
    Voidcleaver = 31111, // Helper->self, no cast, range 100 circle
    VoidMiasma = 32691, // Helper->self, 3.0s cast, range 50 30-degree cone
    LifescleaverVisual = 31112, // Cagnazzo->self, 4.0s cast, single-target
    Lifescleaver = 31113, // Helper->self, 5.0s cast, range 50 30-degree cone
    VoidTorrent = 31118 // Cagnazzo->self/player, 5.0s cast, range 60 width 8 rect
}

public enum IconID : uint
{
    Stackmarker = 161, // player
    Spreadmarker = 139 // player
}

public enum TetherID : uint
{
    BaitAway = 1 // Helper2->player
}

public enum NPCYell : uint
{
    LimitBreakStart = 15175
}

sealed class StygianDelugeArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.StygianDeluge && Bounds.Radius > 21f)
        {
            var center = Center;
            var shape = new AOEShapeCustom(center, [new Square(center, 29.5f)], [new Square(center, 20f)]);
            _aoe = [new(shape, center, default, Module.CastFinishAt(spell, 0.7d), shapeDistance: shape.Distance(center, default))];
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x00 && state == 0x00020001u)
        {
            Bounds = new ArenaBoundsSquare(20f);
            _aoe = [];
        }
    }
}

sealed class VoidTorrent(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.VoidTorrent, new AOEShapeRect(60f, 4f), tankbuster: true);

sealed class Voidcleaver(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Voidcleaver);
sealed class VoidMiasmaBait(ModuleBase module) : Components.BaitAwayTethers(module, new AOEShapeCone(50f, 15f.Degrees()), (uint)TetherID.BaitAway);

sealed class LifescleaverVoidMiasma(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.VoidMiasma, (uint)AID.Lifescleaver], new AOEShapeCone(50f, 15f.Degrees()));

sealed class Tsunami(ModuleBase module) : Components.RaidwideAfterNPCYell(module, (uint)AID.Tsunami, (uint)NPCYell.LimitBreakStart, 4.5d);
sealed class StygianDeluge(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.StygianDeluge);
sealed class Antediluvian(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Antediluvian, 15f)
{
    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        base.OnCastFinished(caster, spell);
        if (NumCasts == 6 && spell.Action.ID == (uint)AID.Antediluvian)
        {
            NumCasts = 0;
        }
    }
}

sealed class BodySlam(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BodySlam, 8f);
sealed class BodySlamKB(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.BodySlamKB, 10f, true)
{
    private readonly Antediluvian _aoe = module.FindComponent<Antediluvian>()!;

    public override bool DestinationUnsafe(int slot, Actor actor, WPos pos)
    {
        var aoes = _aoe.ActiveAOEs(slot, actor);
        var len = aoes.Length;
        for (var i = 0; i < len; ++i)
        {
            ref readonly var aoe = ref aoes[i];
            if (aoe.Check(pos))
            {
                return true;
            }
        }
        return !InBounds(pos);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0 && _aoe.NumCasts >= 4)
        {
            ref readonly var c = ref Casters.Ref(0);
            hints.AddForbiddenZone(new SDInvertedCircle(c.Origin, 10f), c.Activation);
        }
    }
}

sealed class HydraulicRam(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void Update()
    {
        if (_aoes.Count > 1)
        {
            ref var aoe0 = ref _aoes.Ref(0);
            aoe0.Color = Colors.Danger;
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.HydraulicRamTelegraph)
        {
            var dir = spell.LocXZ - caster.Position;
            _aoes.Add(new(new AOEShapeRect(dir.Length(), 4f), caster.Position.Quantized(), Angle.FromDirection(dir), Module.CastFinishAt(spell, 5.7d)));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (_aoes.Count != 0 && spell.Action.ID == (uint)AID.HydraulicRam)
            _aoes.RemoveAt(0);
    }
}

sealed class Hydrobomb(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShapeCircle circle = new(4f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void Update()
    {
        if (_aoes.Count > 2)
        {
            var aoes = CollectionsMarshal.AsSpan(_aoes);
            for (var i = 0; i < 2; ++i)
            {
                ref var aoe = ref aoes[i];
                aoe.Color = Colors.Danger;
            }
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.HydrobombTelegraph)
        {
            _aoes.Add(new(circle, spell.LocXZ, default, Module.CastFinishAt(spell, 6.1d)));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (_aoes.Count != 0 && spell.Action.ID == (uint)AID.Hydrobomb)
        {
            _aoes.RemoveAt(0);
        }
    }
}

sealed class Hydrovent(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Hydrovent, 6f);
sealed class NeapTide(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.Spreadmarker, (uint)AID.NeapTide, 6f, 5d);

sealed class SpringTideHydroFall(ModuleBase module) : Components.UniformStackSpread(module, 6f, default, 4) // both use the same icon
{
    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.Stackmarker)
        {
            AddStack(actor, World.FutureTime(5d));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.SpringTide or (uint)AID.Hydrofall2)
        {
            Stacks.Clear();
        }
    }
}

sealed class D113CagnazzoStates : StateMachineBuilder
{
    public D113CagnazzoStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<StygianDelugeArenaChange>()
            .ActivateOnEnter<Voidcleaver>()
            .ActivateOnEnter<LifescleaverVoidMiasma>()
            .ActivateOnEnter<VoidMiasmaBait>()
            .ActivateOnEnter<Antediluvian>()
            .ActivateOnEnter<BodySlam>()
            .ActivateOnEnter<BodySlamKB>()
            .ActivateOnEnter<HydraulicRam>()
            .ActivateOnEnter<Hydrobomb>()
            .ActivateOnEnter<SpringTideHydroFall>()
            .ActivateOnEnter<NeapTide>()
            .ActivateOnEnter<StygianDeluge>()
            .ActivateOnEnter<Hydrovent>()
            .ActivateOnEnter<VoidTorrent>()
            .ActivateOnEnter<Tsunami>();
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 896u, CFCID = 896u, NameID = 11995u, PrimaryActorOID = (uint)OID.Cagnazzo, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class D113Cagnazzo(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-250f, 130f), new ArenaBoundsSquare(29.5f))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.FearsomeFlotsam));
    }
}
