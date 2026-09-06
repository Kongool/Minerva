// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Dungeon.D09SaintMociannesArboretum.D093Belladonna;

public enum OID : uint
{
    Boss = 0x1434, // R5.0
    BloatedBulb = 0x1435, // R0.75
    LilyOfTheSaint = 0x1436, // R1.0)
    Helper = 0x1B2
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target

    Deracinator = 5219, // Boss->player, no cast, single-target
    AtropineSpore = 5215, // Boss->self, 4.0s cast, range 9-40 donut
    SoulVacuum = 5221, // Boss->self, 4.0s cast, range 40 circle
    MildewSpawn = 5379, // Boss->self, no cast, single-target
    Mildew = 5222, // BloatedBulb->self, no cast, range 6 circle
    FrondFatale = 5216, // Boss->self, 4.0s cast, range 40 circle, gaze
    FrondFataleFail = 5220, // Helper->player, no cast, single-target, gaze fail, no idea why it is an extra AID
    PetalShower = 5217, // Boss->self, no cast, single-target
    Petal = 5218, // Helper->player, no cast, range 8 circle
    Decay = 5223 // LilyOfTheSaint->self, 12.0s cast, range 40 circle
}

public enum IconID : uint
{
    Spreadmarker = 43 // player->self
}

sealed class AtropineSpore(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AtropineSpore, new AOEShapeDonut(9f, 40f));
sealed class SoulVacuum(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.SoulVacuum);
sealed class FrondFatale(ModuleBase module) : Components.CastGaze(module, (uint)AID.FrondFatale);
sealed class Petal(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.Spreadmarker, (uint)AID.Petal, 8f, 3.1f);

sealed class Deracinator(ModuleBase module) : Components.SingleTargetInstant(module, (uint)AID.Deracinator)
{
    private bool start = true;

    public override void Update()
    {
        if (start)
        {
            AddTankbuster(3.1d); // its assumed that the tank will aggro the boss first
            start = false;
        }
    }

    public override void OnActorModelStateChange(Actor actor, byte modelState, byte animState1, byte animState2)
    {
        if (actor.OID == (uint)OID.BloatedBulb && animState1 == 1 && Targets.Count == 0 && Module.Enemies((uint)OID.BloatedBulb).Count == 5)
        {
            AddTankbuster(8.1d);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.FrondFatale)
        {
            AddTankbuster(4.1d);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.Deracinator)
        {
            Targets.Clear();
        }
    }

    private void AddTankbuster(double delay)
    {
        var id = Module.PrimaryActor.TargetID;
        if (World.Actors.Find(id) is Actor t)
        {
            Targets.Add((Raid.FindSlot(id), World.FutureTime(delay), id, Module.PrimaryActor, t));
        }
    }
}

sealed class Mildew(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly AOEShapeCircle circle = new(6f);
    private readonly List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnActorModelStateChange(Actor actor, byte modelState, byte animState1, byte animState2)
    {
        if (actor.OID == (uint)OID.BloatedBulb)
            if (animState1 == 0x01)
            {
                _aoes.Add(new(circle, actor.Position.Quantized(), default, World.FutureTime(10d))); // despite spawning at the same time, there can be multiple seconds difference between explosions, taking a low estimate here
            }
            else
            {
                _aoes.Clear();
            }
    }
}

sealed class D093BelladonnaStates : StateMachineBuilder
{
    public D093BelladonnaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Mildew>()
            .ActivateOnEnter<AtropineSpore>()
            .ActivateOnEnter<SoulVacuum>()
            .ActivateOnEnter<FrondFatale>()
            .ActivateOnEnter<Petal>()
            .ActivateOnEnter<Deracinator>();
    }
}

[ModuleInfo(CFCID = 41u, NameID = 4658u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class D093Belladonna : ModuleBase
{
    public D093Belladonna(WorldState ws, Actor primary) : this(ws, primary, BuildArena()) { }

    private D093Belladonna(WorldState ws, Actor primary, (WPos center, ArenaBoundsCustom arena) a) : base(ws, primary, a.center, a.arena) { }

    private static (WPos center, ArenaBoundsCustom arena) BuildArena()
    {
        var arena = new ArenaBoundsCustom([new Polygon(default, 19.5f * CosPI.Pi64th, 64)], [new Rectangle(new(-16.441f, -11.753f), 20f, 1.25f, 55.859f.Degrees())]);
        return (arena.Center, arena);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.LilyOfTheSaint));
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.LilyOfTheSaint => 1,
                _ => 0
            };
        }
    }
}
