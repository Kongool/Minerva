// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Hunt.RankS.Burfurlur;

public enum OID : uint
{
    Boss = 0x360A // R6.0
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target

    Trolling = 27316, // Boss->self, no cast, range 10 circle
    QuintupleInhale1 = 27307, // Boss->self, 4.0s cast, range 27 120-degree cone
    QuintupleInhale24 = 27308, // Boss->self, 0.5s cast, range 27 120-degree cone
    QuintupleSneeze1 = 27309, // Boss->self, 5.0s cast, range 40 120-degree cone
    QuintupleSneeze24 = 27310, // Boss->self, 0.5s cast, range 40 120-degree cone
    QuintupleInhale35 = 27692, // Boss->self, 0.5s cast, range 27 120-degree cone
    QuintupleSneeze35 = 27693, // Boss->self, 0.5s cast, range 40 120-degree cone
    Uppercut = 27314, // Boss->self, 3.0s cast, range 15 120-degree cone
    RottenSpores = 27313 // Boss->location, 3.0s cast, range 6 circle
}

class QuintupleSneeze(ModuleBase module) : Components.GenericAOEs(module)
{
    private Angle _referenceAngle;
    private readonly List<Angle> _pendingOffsets = [];
    private DateTime _nextSneeze;

    private static readonly AOEShapeCone _shape = new(40f, 60f.Degrees());

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (_nextSneeze != default)
        {
            var count = _pendingOffsets.Count;
            var max = count > 2 ? 2 : count;
            var aoes = new AOEInstance[max];
            for (var i = 0; i < max; ++i)
            {
                aoes[i] = new(_shape, Module.PrimaryActor.Position, _referenceAngle + _pendingOffsets[i], i == 0 ? _nextSneeze : _nextSneeze.AddSeconds(2.2d), count > 1 && i == 0 ? Colors.Danger : default);
            }
            return aoes;
        }
        return [];
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.QuintupleInhale1:
                _referenceAngle = spell.Rotation;
                _pendingOffsets.Clear();
                _pendingOffsets.Add(default);
                _nextSneeze = default;
                break;
            case (uint)AID.QuintupleInhale24:
            case (uint)AID.QuintupleInhale35:
                _pendingOffsets.Add(spell.Rotation - _referenceAngle);
                break;
            case (uint)AID.QuintupleSneeze1:
                _referenceAngle = spell.Rotation;
                _nextSneeze = Module.CastFinishAt(spell);
                break;
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (_pendingOffsets.Count > 0 && spell.Action.ID is (uint)AID.QuintupleSneeze1 or (uint)AID.QuintupleSneeze24 or (uint)AID.QuintupleSneeze35)
        {
            _pendingOffsets.RemoveAt(0);
            _nextSneeze = World.FutureTime(2.2d);
        }
    }
}

class Uppercut(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Uppercut, new AOEShapeCone(15f, 60f.Degrees()));
class RottenSpores(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RottenSpores, 6f);

class BurfurlurStates : StateMachineBuilder
{
    public BurfurlurStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<QuintupleSneeze>()
            .ActivateOnEnter<Uppercut>()
            .ActivateOnEnter<RottenSpores>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 10617u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Burfurlur(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
