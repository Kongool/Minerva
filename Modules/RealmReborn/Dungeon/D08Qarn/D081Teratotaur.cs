// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Dungeon.D08Qarn.D081Teratotaur;

public enum OID : uint
{
    Teratotaur = 0x477A, // R2.24
    DungWespe = 0x477B, // R0.4
    Platform1 = 0x1E87E2, // R2.0
    Platform2 = 0x1E87E3, // R2.0
    Platform3 = 0x1E87E4 // R2.0
}

public enum AID : uint
{
    AutoAttackBoss = 870, // Boss->player, no cast, single-target
    AutoAttackWespe = 871, // DungWespe->player, no cast, single-target

    Triclip = 42231, // Boss->player, 5.0s cast, single-target
    Mow = 42232, // Boss->self, 2.5s cast, range 6+R 120-degree cone
    MortalRay = 42229, // Boss->self, 3.0s cast, range 50 circle
    FrightfulRoar = 42233, // Boss->self, 3.0s cast, range 6 circle
    FinalSting = 42230 // DungWespe->player, 3.0s cast, single-target
}

public enum SID : uint
{
    Doom = 5187 // Boss->player, extra=0x0
}

sealed class Triclip(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Triclip);
sealed class Mow(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Mow, new AOEShapeCone(8.25f, 60f.Degrees()));
sealed class FrightfulRoar(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FrightfulRoar, 6f);

sealed class MortalRay(ModuleBase module) : Components.GenericAOEs(module)
{
    private BitMask doomed;
    private AOEInstance[] _platform = [];
    private readonly AOEShapeRect square = new(1.75f, 1.75f, 1.75f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => doomed[slot] ? _platform : [];

    public override void OnActorRenderflagsChange(Actor actor, int renderflags)
    {
        if (renderflags == 0 && actor.OID is (uint)OID.Platform1 or (uint)OID.Platform2 or (uint)OID.Platform3)
        {
            var pos = actor.Position;
            _platform = [new(square, actor.Position, default, World.FutureTime(10d), Colors.SafeFromAOE, shapeDistance: square.InvertedDistance(pos, default))];
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.MortalRay)
        {
            var targets = CollectionsMarshal.AsSpan(spell.Targets);
            var len = targets.Length;

            for (var i = 0; i < len; ++i)
            {
                ref var t = ref targets[i];
                doomed.Set(Raid.FindSlot(t.ID));
            }
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (doomed[slot])
        {
            hints.Add("Go to glowing platform!");
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.Doom)
        {
            doomed.Clear(Raid.FindSlot(actor.InstanceID));
        }
    }
}

sealed class MortalRayStun(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.MortalRay, false, true, showNameInHint: true);

sealed class D081TeratotaurStates : StateMachineBuilder
{
    public D081TeratotaurStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Triclip>()
            .ActivateOnEnter<Mow>()
            .ActivateOnEnter<FrightfulRoar>()
            .ActivateOnEnter<MortalRayStun>()
            .ActivateOnEnter<MortalRay>();
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 9u, CFCID = 9u, NameID = 1567u, PrimaryActorOID = (uint)OID.Teratotaur, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus, Chuggalo (ported from BMR)")]
public sealed class D081Teratotaur(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new PolygonCustom([new(-94.9f, -59f), new(-70.2f, -46.1f), new(-55.3f, -46.6f),
    new(-55.7f, -55.6f), new(-51.1f, -60.9f), new(-51.2f, -65), new(-58.1f, -67.7f),
    new(-64.7f, -70.6f), new(-88.4f, -72.2f), new(-89, -66.2f), new(-94.9f, -65.5f)])]);

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.DungWespe => 1,
                _ => 0
            };
        }
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.DungWespe));
    }
}
