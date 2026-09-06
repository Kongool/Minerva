// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Quest.MSQ.TheResonant;

public enum OID : uint
{
    Boss = 0x1B7D,
    MarkXLIIIArtilleryCannon = 0x1B7E, // R0.600, x0 (spawn during fight)
    Terminus = 0x1BCA, // R1.000, x0 (spawn during fight)
    Helper2 = 0x18D6,
    Helper = 0x233C
}

public enum AID : uint
{
    MagitekRay = 9104, // 1B7E->self, 2.5s cast, range 45+R width 2 rect
    ChoppingBlock1 = 9110, // Helper2->location, 3.0s cast, range 5 circle
    TheOrder = 9106, // Boss->self, 5.0s cast, single-target
    TerminusEst1 = 9108, // Terminus->self, no cast, range 40+R width 4 rect
    Skullbreaker1 = 9112, // Helper2->self, 6.0s cast, range 40 circle
}

public enum SID : uint
{
    Resonant = 780,
}

class Skullbreaker(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Skullbreaker1, 12);

class TerminusEst(ModuleBase module) : Components.GenericAOEs(module)
{
    private DateTime? Activation;
    private static readonly AOEShapeRect rect = new(41f, 2f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (Activation == null)
            return [];
        var terminii = Module.Enemies((uint)OID.Terminus);
        var count = terminii.Count;

        var aoes = new AOEInstance[count];
        var act = Activation.Value;
        for (var i = 0; i < count; ++i)
        {
            var t = terminii[i];
            if (!t.Position.AlmostEqual(Center, 0.5f))
            {
                aoes[i] = new(rect, t.Position, t.Rotation, activation: act);
            }
        }
        return aoes;
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.TheOrder)
            Activation = Module.CastFinishAt(spell).AddSeconds(0.8d);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (int)AID.TerminusEst1)
            Activation = null;
    }
}
class MagitekRay(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MagitekRay, new AOEShapeRect(45.6f, 1f));
class ChoppingBlock(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ChoppingBlock1, 5f);

class Siphon(ModuleBase module) : ModuleComponent(module)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var h = hints.PotentialTargets[i];
            if (h.Actor.FindStatus((uint)SID.Resonant) != null)
            {
                h.Priority = AIHints.Enemy.PriorityForbidden;
                hints.ActionsToExecute.Push(World.Client.DutyActions[0].Action, h.Actor, ActionQueue.Priority.ManualEmergency); // use emergency mode to bypass forbidden state - duty action is the only thing we can use on fordola without being stunned
            }
        }
    }
}

public class FordolaRemLupisStates : StateMachineBuilder
{
    public FordolaRemLupisStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Siphon>()
            .ActivateOnEnter<MagitekRay>()
            .ActivateOnEnter<ChoppingBlock>()
            .ActivateOnEnter<TerminusEst>()
            .ActivateOnEnter<Skullbreaker>();
    }
}

[ModuleInfo(CFCID = 68086u, NameID = 6104u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class FordolaRemLupis(WorldState ws, Actor primary) : ModuleBase(ws, primary, default, new ArenaBoundsSquare(19.5f));
