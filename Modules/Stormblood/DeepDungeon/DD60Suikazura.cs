// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.DeepDungeon.HeavenOnHigh.DD60Suikazura;

public enum OID : uint
{
    Boss = 0x23E4, // R2.5
    AccursedCane = 0x23E5 // R1.0
}

public enum AID : uint
{
    AutoAttack = 6499, // Boss->player, no cast, single-target

    Firewalker = 11925, // Boss->self, 3.0s cast, range 10 90-degree cone
    InfiniteAnguish = 11930, // AccursedCane->self, 3.0s cast, range 6-12 donut
    FireII = 11927, // Boss->location, 3.5s cast, range 5 circle
    Topple = 11926, // Boss->self, 3.0s cast, range 3+R circle
    SearingChain = 11929, // AccursedCane->self, 3.0s cast, range 60+R width 4 rect
    AncientFlare = 11928 // Boss->self, 5.0s cast, range 50 circle
}

class Firewalker(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Firewalker, new AOEShapeCone(10f, 45f.Degrees()));

class InfiniteAnguish(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeDonut donut = new(6f, 12f);
    private readonly List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.AccursedCane)
            _aoes.Add(new(donut, actor.Position.Quantized(), default, World.FutureTime(7.9d), actorID: actor.InstanceID));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.InfiniteAnguish)
        {
            var count = _aoes.Count;
            var id = caster.InstanceID;
            for (var i = 0; i < count; ++i)
            {
                if (_aoes[i].ActorID == id)
                {
                    _aoes.RemoveAt(i);
                    return;
                }
            }
        }
    }
}

class FireII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FireII, 5f);
class Topple(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Topple, 5.5f);
class SearingChain(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SearingChain, new AOEShapeRect(61f, 2f));
class AncientFlare(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AncientFlare);

class DD60SuikazuraStates : StateMachineBuilder
{
    public DD60SuikazuraStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Firewalker>()
            .ActivateOnEnter<InfiniteAnguish>()
            .ActivateOnEnter<FireII>()
            .ActivateOnEnter<Topple>()
            .ActivateOnEnter<SearingChain>()
            .ActivateOnEnter<AncientFlare>();
    }
}

[ModuleInfo(CFCID = 545u, NameID = 7487u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "LegendofIceman, Malediktus (ported from BMR)")]
public class DD60Suikazura(WorldState ws, Actor primary) : HoHArena2(ws, primary);

