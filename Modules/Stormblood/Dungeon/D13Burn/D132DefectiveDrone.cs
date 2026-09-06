// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Dungeon.D13TheBurn.D132DefectiveDrone;

public enum OID : uint
{
    Boss = 0x23AA, // R3.2
    MiningDrone = 0x23AB, // R1.92
    RockBiter = 0x23AC, // R3.0
    RepurposedDreadnaught = 0x23AD, // R2.4
    SludgeVoidzone = 0x1EA9EF
}

public enum AID : uint
{
    AutoAttack = 872, // Boss/RepurposedDreadnaught->player, no cast, single-target

    AetherochemicalFlame = 11635, // Boss->self, 3.0s cast, range 40 circle
    AetherochemicalResidue = 11636, // Boss->location, no cast, range 5 circle

    ThrottleVisual1 = 13526, // Boss->self, no cast, single-target
    ThrottleVisual2 = 13529, // MiningDrone->self, no cast, single-target
    ThrottleVisual3 = 13525, // Boss->self, no cast, single-target
    Throttle = 11638, // MiningDrone->location, 3.0s cast, width 3 rect charge
    FullThrottle = 11637, // Boss->location, 3.0s cast, width 5 rect charge

    SelfDetonate = 11639, // MiningDrone->self, 3.0s cast, range 5 circle, outside arena bounds
    AetherochemicalCoil = 11634, // Boss->player, 3.0s cast, single-target
    AditDriver = 11640 // RockBiter->self, 6.0s cast, range 30+R width 6 rect
}

public enum IconID : uint
{
    Baitaway = 2 // player
}

class Throttle(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShapeRect rectSmall = new(50f, 1.5f);
    private static readonly AOEShapeRect rectBig = new(50f, 2.5f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.ThrottleVisual1)
        {
            var activation = World.FutureTime(8.2d);
            var enemies = Module.Enemies((uint)OID.MiningDrone);
            var count = enemies.Count;

            for (var i = 0; i < count; ++i)
            {
                var e = enemies[i];
                if (e.ModelState.AnimState1 != 1)
                {
                    _aoes.Add(new(rectSmall, e.Position.Quantized(), e.Rotation, activation));
                }
            }
            var offset = _aoes.Count != 0 && _aoes.Ref(0).Origin.X < 0f ? -1f : 1f;
            _aoes.Add(new(rectBig, new WPos(offset * 18f, -71.5f).Quantized(), -90f.Degrees() * offset, activation));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.FullThrottle)
        {
            _aoes.Clear();
        }
    }
}

class AetherochemicalFlame(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AetherochemicalFlame);
class AetherochemicalResidue(ModuleBase module) : Components.BaitAwayIcon(module, 5f, (uint)IconID.Baitaway, (uint)AID.AetherochemicalResidue, 4.1d);
class AditDriver(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AditDriver, new AOEShapeRect(33f, 3f));
class AetherochemicalCoil(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.AetherochemicalCoil);
class SludgeVoidzone(ModuleBase module) : Components.Voidzone(module, 2.5f, GetVoidzones)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.SludgeVoidzone);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (z.EventState != 7)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }
}

class D132DefectiveDroneStates : StateMachineBuilder
{
    public D132DefectiveDroneStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Throttle>()
            .ActivateOnEnter<AetherochemicalFlame>()
            .ActivateOnEnter<AetherochemicalResidue>()
            .ActivateOnEnter<AetherochemicalCoil>()
            .ActivateOnEnter<AditDriver>()
            .ActivateOnEnter<SludgeVoidzone>();
    }
}

[ModuleInfo(CFCID = 585u, NameID = 7669u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public class D132DefectiveDrone(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(default, -70f), new ArenaBoundsRect(10f, 9.5f))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.RepurposedDreadnaught));
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.RepurposedDreadnaught => 1,
                _ => 0
            };
        }
    }
}
