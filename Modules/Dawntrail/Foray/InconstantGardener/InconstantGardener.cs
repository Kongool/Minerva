// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.FATE.InconstantGardener;

public enum OID : uint
{
    Iambe = 0x4C41,
    Helper = 0x233C,
    Iambe1 = 0x4C42, // R1.000, x0 (spawn during fight)
    WinsomeSeed = 0x4C43, // R0.240-0.528, x0 (spawn during fight)
}

public enum AID : uint
{
    AutoAttack = 50855, // Iambe->player, no cast, single-target
    DirectSeeding = 48029, // Iambe->self, 3.0s cast, single-target
    GardenersHymnCast = 48031, // Iambe->self, 2.5s cast, single-target
    GardenersHymn = 48032, // 4C42->location, 6.0s cast, range 5 circle
    Burst = 48033, // 4C43->self, 2.0s cast, range 15 circle
    OdeOfTheUnderfoot = 48037, // Iambe->self, 5.0s cast, range 10 circle
    IambicMarch = 48035, // Iambe->self, 3.0s cast, range 40 circle
}

public enum SID : uint
{
    ForwardMarch = 5142, // Iambe->player, extra=0x0
    AboutFace = 5143, // Iambe->player, extra=0x0
    ForcedMarch = 1257, // Iambe->player, extra=0x1/0x2
    Gen = 5106, // 4C42->4C43, extra=0x1
    Gen1 = 5107, // 4C42->4C43, extra=0x1
}

sealed class GardenersHymn(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GardenersHymn, 5f);
sealed class OdeOfTheUnderfoot(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.OdeOfTheUnderfoot, 10f);

sealed class IambicMarch(ModuleBase module) : Components.StatusDrivenForcedMarch(module, 2.0f, (uint)SID.ForwardMarch, (uint)SID.AboutFace, default, default)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        var state = State.GetValueOrDefault(actor.InstanceID);
        if (state == null || state.PendingMoves.Count == 0)
        {
            return;
        }

        var move0 = state.PendingMoves[0];
        var requiredFacing = Angle.FromDirection((actor.Position - Module.PrimaryActor.Position).Normalized()) - move0.dir;
        hints.ForbiddenDirections.Add((requiredFacing + 180.0f.Degrees(), 170.0f.Degrees(), move0.activation));
    }
}

sealed class Burst(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Burst, 15.0f, riskyWithSecondsLeft: 6.0f)
{
    private readonly List<Actor> seeds = [];

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.WinsomeSeed)
        {
            seeds.Add(actor);
        }
    }

    public override void OnActorDestroyed(Actor actor)
    {
        if (actor.OID == (uint)OID.WinsomeSeed)
        {
            seeds.Remove(actor);
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.GardenersHymn)
        {
            foreach (var seed in seeds)
            {
                if (seed.Position.InCircle(spell.LocXZ, 5.0f))
                {
                    Casters.Add(new(Shape, seed.Position, default, Module.CastFinishAt(spell, 3.5f), actorID: seed.InstanceID,
                        shapeDistance: Shape.Distance(seed.Position, default)));
                }
            }
        }
    }
}

[SkipLocalsInit]
sealed class InconstantGardenerStates : StateMachineBuilder
{
    public InconstantGardenerStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<GardenersHymn>()
            .ActivateOnEnter<OdeOfTheUnderfoot>()
            .ActivateOnEnter<IambicMarch>()
            .ActivateOnEnter<Burst>();
    }
}

[ModuleInfo(CFCID = 1093u, NameID = 2079u, PrimaryActorOID = (uint)OID.Iambe, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Equilius (ported from BMR)")]
[SkipLocalsInit]
// BMR derives this from OpenWorldFate, which follows the boss and gates activation on the player being
// within 30y. Minerva takes a fixed centre, so this is the centre of the cast locations across a real pull
// (Iambe, recording 2026-08-24: 81 ground casts spanning 39x39y) with BMR's own 30y FATE radius.
public sealed class InconstantGardener(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(-168.8f, -503.7f), new ArenaBoundsCircle(30f));
