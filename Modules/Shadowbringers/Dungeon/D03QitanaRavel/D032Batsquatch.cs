// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Dungeon.D03QitanaRavel.D032Batsquatch;

public enum OID : uint
{
    Boss = 0x27B0, //R=3.2
    StalactiteBig = 0x1EAACD, //R=0.5
    StalactiteSmall1 = 0x1EAACC, //R=0.5
    StalactiteSmall2 = 0x1EAACB, //R=0.5
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 870, // Boss->player, no cast, single-target

    RipperFang = 15505, // Boss->player, 4.0s cast, single-target
    Soundwave = 15506, // Boss->self, 3.0s cast, range 40 circle
    Subsonics = 15507, // Boss->self, 6.0s cast, single-target
    Subsonics2 = 15508, // Helper->self, no cast, range 40 circle
    FallingRock = 15510, // Helper->self, 2.0s cast, range 3 circle
    FallingRock2 = 15509, // Helper->self, 2.0s cast, range 2 circle
    FallingBoulder = 15511, // Helper->self, 2.0s cast, range 4 circle
    Towerfall = 15512 // Helper->self, 3.0s cast, range 15 30-degree cone
}

class Towerfall(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCone cone = new(15f, 15f.Degrees());
    public List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.StalactiteBig) // preliminary, caster position is somewhat off from stalactite position...
            _aoes.Add(new(cone, actor.Position, actor.Rotation, World.FutureTime(12d)));
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Towerfall)
        {
            var count = _aoes.Count; // remove preliminary AOE
            var pos = caster.Position;
            for (var i = 0; i < count; ++i)
            {
                var aoe = _aoes[i];
                if (aoe.Origin == pos)
                {
                    _aoes.RemoveAt(i);
                    break;
                }
            }
            _aoes.Add(new(cone, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell))); // add aoe with correct location and activation
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (_aoes.Count != 0 && spell.Action.ID == (uint)AID.Towerfall)
            _aoes.Clear();
    }
}

class Soundwave(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Soundwave, "Raidwide + towers fall");
class Subsonics(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Subsonics, "Raidwide x11");
class RipperFang(ModuleBase module) : Components.SingleTargetDelayableCast(module, (uint)AID.RipperFang);
class FallingBoulder(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FallingBoulder, 4f);
class FallingRock(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FallingRock, 3f);
class FallingRock2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FallingRock2, 2f);

class D032BatsquatchStates : StateMachineBuilder
{
    public D032BatsquatchStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Towerfall>()
            .ActivateOnEnter<Soundwave>()
            .ActivateOnEnter<Subsonics>()
            .ActivateOnEnter<RipperFang>()
            .ActivateOnEnter<FallingBoulder>()
            .ActivateOnEnter<FallingRock>()
            .ActivateOnEnter<FallingRock2>();
    }
}

[ModuleInfo(CFCID = 651u, NameID = 8232u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class D032Batsquatch(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(62f, -35f), 14.5f * CosPI.Pi28th, 28)], [new Rectangle(new(61.9f, -20), 20f, 2f), new Rectangle(new(61.9f, -50f), 20f, 2f)]);
}
