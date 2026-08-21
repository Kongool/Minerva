// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Dungeon.D08StrayboroughDeadwalk.D081HisRoyalHeadnessLeonoggI;

public enum OID : uint
{
    Boss = 0x4183, // R3.6
    LittleLadyNogginette = 0x41BD, // R1.0
    LittleLordNoggington = 0x41BB, // R1.0
    NobleNoggin = 0x4205, // R1.0
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target

    MaliciousMist = 36529, // Boss->self, 5.0s cast, range 50 circle, raidwide

    FallingNightmareVisual = 36526, // Boss->self, 3.0s cast, single-target
    FallingNightmare1 = 36532, // NobleNoggin->self, 2.0s cast, range 2 circle
    FallingNightmare2 = 36536, // NobleNoggin->self, 1.0s cast, range 2 circle, only happens if caught by add

    MorbidFascination = 36528, // Boss->self, no cast, single-target

    TeamSpirit = 36527, // Boss->self, 3.0s cast, single-target, summons dolls
    SpiritedChargeVisual = 36598, // Boss->self, 3.0s cast, single-target
    SpiritedChargeStart = 36533, // LittleLordNoggington/LittleLadyNogginette->self, no cast, single-target
    SpiritedCharge = 36534, // Helper->self, no cast, range 2 width 1 rect
    Overattachment = 36535, // LittleLadyNogginette/LittleLordNoggington->player, no cast, single-target

    EvilSchemeVisual = 39682, // Boss->self, 6.0s cast, single-target, exaflare
    EvilSchemeFirst = 39683, // Helper->self, 6.0s cast, range 4 circle
    EvilSchemeRest = 39684, // Helper->self, no cast, range 4 circle

    LoomingNightmareVisual = 39685, // Boss->self, 5.0s cast, single-target, chasing AOE
    LoomingNightmareFirst = 39686, // Helper->self, 2.0s cast, range 4 circle
    LoomingNightmareRest = 39687, // Helper->self, no cast, range 4 circle

    ScreamVisual1 = 36530, // Boss->self, 5.0s cast, single-target
    ScreamVisual2 = 36541, // Boss->self, no cast, single-target
    Scream = 36531 // Helper->self, 5.0s cast, range 20 60-degree cone
}

public enum IconID : uint
{
    LoomingNightmare = 197 // player
}

sealed class ArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeDonut donut = new(14f, 20f);
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.MaliciousMist && Module.Bounds.Radius > 14f)
        {
            _aoe = [new(donut, Module.Center, default, Module.CastFinishAt(spell, 0.9d))];
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x00 && state == 0x00020001u)
        {
            Module.Bounds = D081HisRoyalHeadnessLeonoggI.DefaultBounds;
            _aoe = [];
        }
    }
}

sealed class LoomingNightmare(ModuleBase module) : Components.StandardChasingAOEs(module, 4f, (uint)AID.LoomingNightmareFirst, (uint)AID.LoomingNightmareRest, 3, 1.6f, 5, true, (uint)IconID.LoomingNightmare)
{
    private int totalChasers;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        base.OnCastStarted(caster, spell);
        if (spell.Action.ID == ActionFirst)
        {
            ++totalChasers;
            if (totalChasers > 1)
            {
                MaxCasts = 4;
            }
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        if (TargetsMask[slot])
        {
            hints.AddForbiddenZone(new SDCircle(Module.Center, 13.5f), Activation);
        }
    }
}

sealed class FallingNightmare(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCircle circle = new(2);
    private readonly List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (id == 0x11D1 && actor.OID == (uint)OID.NobleNoggin)
        {
            _aoes.Add(new(circle, actor.Position, default, World.FutureTime(3d))); // can be 3 or 4 seconds depending on mechanic
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (_aoes.Count > 0 && spell.Action.ID is (uint)AID.FallingNightmare1 or (uint)AID.FallingNightmare2)
        {
            _aoes.RemoveAt(0);
        }
    }
}

sealed class SpiritedCharge(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly AOEShapeRect rect = new(6f, 1f);
    private readonly List<Actor> _charges = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _charges.Count;
        if (count == 0)
            return [];
        var aoes = new AOEInstance[count];
        for (var i = 0; i < count; ++i)
        {
            var c = _charges[i];
            aoes[i] = new(rect, c.Position, c.Rotation);
        }
        return aoes;
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.SpiritedChargeStart)
            _charges.Add(caster);
    }

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (id == 0x1E3C)
        {
            _charges.Remove(actor);
        }
    }
}

sealed class EvilScheme(ModuleBase module) : Components.Exaflare(module, 4f)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.EvilSchemeFirst)
        {
            Lines.Add(new(caster.Position, 4f * spell.Rotation.ToDirection(), Module.CastFinishAt(spell, 1.6d), 1.5d, 5, 5));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.EvilSchemeFirst or (uint)AID.EvilSchemeRest)
        {
            var count = Lines.Count;
            var pos = caster.Position;
            for (var i = 0; i < count; ++i)
            {
                var line = Lines[i];
                if (line.Next.AlmostEqual(pos, 1f))
                {
                    AdvanceLine(line, pos);
                    if (line.ExplosionsLeft == 0)
                        Lines.RemoveAt(i);
                    break;
                }
            }
        }
    }
}

sealed class MaliciousMist(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.MaliciousMist);

sealed class Scream : Components.SimpleAOEs
{
    public Scream(ModuleBase module) : base(module, (uint)AID.Scream, new AOEShapeCone(20f, 30f.Degrees()), 4) { MaxDangerColor = 2; }
}

sealed class D081HisRoyalHeadnessLeonoggIStates : StateMachineBuilder
{
    public D081HisRoyalHeadnessLeonoggIStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChange>()
            .ActivateOnEnter<MaliciousMist>()
            .ActivateOnEnter<LoomingNightmare>()
            .ActivateOnEnter<EvilScheme>()
            .ActivateOnEnter<FallingNightmare>()
            .ActivateOnEnter<SpiritedCharge>()
            .ActivateOnEnter<Scream>();
    }
}

[ModuleInfo(CFCID = 981u, NameID = 13073u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class D081HisRoyalHeadnessLeonoggI(WorldState ws, Actor primary) : ModuleBase(ws, primary, ArenaCenter, StartingBounds)
{
    public static readonly WPos ArenaCenter = new(default, 150f);
    public static readonly ArenaBoundsCircle StartingBounds = new(19.5f);
    public static readonly ArenaBoundsCircle DefaultBounds = new(14f);
}
