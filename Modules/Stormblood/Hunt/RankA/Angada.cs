// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Hunt.RankA.Angada;

public enum OID : uint
{
    Boss = 0x1AC0, // R=5.4
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target

    ScytheTail = 8190, // Boss->self, 3.0s cast, range 4+R circle, knockback 10, away from source + stun
    RockThrow = 8193, // Boss->location, 3.0s cast, range 6 circle
    Butcher = 8191, // Boss->self, 3.0s cast, range 6+R 120-degree cone
    Rip = 8192 // Boss->self, no cast, range 6+R 120-degree cone, always happens directly after Butcher
}

class ScytheTail(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ScytheTail, 9.4f);
class Butcher(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Butcher, new AOEShapeCone(11.4f, 60.Degrees()));

class Rip(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];
    private static readonly AOEShapeCone cone = new(11.4f, 60f.Degrees());

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Butcher)
        {
            _aoe = [new(cone, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell))];
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.Rip)
        {
            _aoe = [];
        }
    }
}

class RockThrow(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RockThrow, 6f);

class AngadaStates : StateMachineBuilder
{
    public AngadaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ScytheTail>()
            .ActivateOnEnter<Butcher>()
            .ActivateOnEnter<Rip>()
            .ActivateOnEnter<RockThrow>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 5999u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class Angada(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
