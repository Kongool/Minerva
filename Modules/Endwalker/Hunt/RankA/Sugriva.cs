// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;


namespace Minerva.Endwalker.Hunt.RankA.Sugriva;

public enum OID : uint
{
    Boss = 0x35FC // R6.0
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target

    Twister = 27219, // Boss->players, 5.0s cast, range 8 circle stack + knockback 20 (except target)
    BarrelingSmash = 27220, // Boss->player, no cast, single-target, knockback 10, charges to random player and starts casting Spark or Scythe Tail immediately afterwards
    Spark = 27221, // Boss->self, 5.0s cast, range 14-24+R donut
    ScytheTail = 27222, // Boss->self, 5.0s cast, range 17 circle
    Butcher = 27223, // Boss->self, 5.0s cast, range 8 120-degree cone
    Rip = 27224, // Boss->self, 2.5s cast, range 8 120-degree cone
    RockThrowFirst = 27225, // Boss->location, 4.0s cast, range 6 circle
    RockThrowRest = 27226, // Boss->location, 1.6s cast, range 6 circle
    Crosswind = 27227, // Boss->self, 5.0s cast, range 36 circle
    ApplyPrey = 27229 // Boss->player, 0.5s cast, single-target
}

public enum SID : uint
{
    Prey = 2939 // Boss->player, extra=0x0
}

sealed class TwisterStack(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.Twister, 8f, 8, 8);
sealed class TwisterKB(ModuleBase module) : Components.GenericKnockback(module)
{
    private Actor? target;
    private DateTime activation;

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor)
    {
        if (target is not Actor t || actor == t)
        {
            return [];
        }

        if (actor.Position.InCircle(t.Position, 8f))
        {
            return new Knockback[1] { new(t.Position, 20f, activation) };
        }
        return [];
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Twister && World.Actors.Find(spell.TargetID) is Actor t)
        {
            target = t;
            activation = Module.CastFinishAt(spell);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Twister)
        {
            target = null;
        }
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (target != null)
        {
            hints.Add("Stack and knockback");
        }
    }
}

sealed class Spark(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Spark, new AOEShapeDonut(14f, 30f));
sealed class ScytheTail(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ScytheTail, 17f);

sealed class Butcher(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.Butcher, new AOEShapeCone(8f, 60f.Degrees()), endsOnCastEvent: true, tankbuster: true);

sealed class Rip(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Rip, new AOEShapeCone(8f, 60f.Degrees()));

sealed class RockThrowAOE(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.RockThrowFirst, (uint)AID.RockThrowRest], 6f);
sealed class RockThrowBait(ModuleBase module) : Components.GenericBaitAway(module, centerAtTarget: true)
{
    private static readonly AOEShapeCircle circle = new(6f);

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.Prey)
        {
            CurrentBaits.Clear(); // incase player dies
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.ApplyPrey:
                NumCasts = 0;
                if (World.Actors.Find(spell.TargetID) is Actor t && t.Type != ActorType.Chocobo) // player Chocobos are immune against prey, so mechanic doesn't happen if a chocobo gets selected
                {
                    CurrentBaits.Add(new(caster, t, circle, Module.CastFinishAt(spell, 1.1d)));
                }
                break;
            case (uint)AID.RockThrowRest:
                if (++NumCasts == 2)
                {
                    CurrentBaits.Clear();
                }
                break;
        }
    }
}

sealed class Crosswind(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Crosswind);

sealed class SugrivaStates : StateMachineBuilder
{
    public SugrivaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<TwisterStack>()
            .ActivateOnEnter<TwisterKB>()
            .ActivateOnEnter<Spark>()
            .ActivateOnEnter<ScytheTail>()
            .ActivateOnEnter<Butcher>()
            .ActivateOnEnter<Rip>()
            .ActivateOnEnter<RockThrowAOE>()
            .ActivateOnEnter<RockThrowBait>()
            .ActivateOnEnter<Crosswind>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 10626u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Sugriva(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
