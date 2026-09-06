// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Quest.MSQ.CloseEncountersOfTheVIthKind;

public enum OID : uint
{
    Boss = 0xF1C, // R0.550, x?
    Puddle = 0x1E88F5, // R0.500, x?
    TerminusEst = 0xF5D, // R1.000, x?
}

public enum AID : uint
{
    HandOfTheEmpire = 4000, // Boss->location, 2.0s cast, range 2 circle
    TerminusEstBoss = 4005, // Boss->self, 3.0s cast, range 50 circle
    TerminusEstAOE = 3825, // TerminusEst->self, no cast, range 40+R width 4 rect
}

class RegulaVanHydrusStates : StateMachineBuilder
{
    public RegulaVanHydrusStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<TerminusEst>()
            .ActivateOnEnter<Voidzone>()
            .ActivateOnEnter<HandOfTheEmpire>();
    }
}

class HandOfTheEmpire(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HandOfTheEmpire, 2f);
class Voidzone(ModuleBase module) : Components.Voidzone(module, 8f, GetPuddles)
{
    private static List<Actor> GetPuddles(ModuleBase module) => module.Enemies((uint)OID.Puddle);
}

class TerminusEst(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.TerminusEstAOE)
{
    private bool _active;
    private static readonly AOEShapeRect rect = new(40, 2);

    public static List<Actor> GetTerminusEst(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.TerminusEst);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var terminus = new List<Actor>(count);
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (!z.IsDead)
                terminus.Add(z);
        }
        return terminus;
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        Arena.Actors(GetTerminusEst(Module), Colors.Danger, true);
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var terminus = GetTerminusEst(Module);
        var count = terminus.Count;
        if (!_active || count == 0)
            return [];
        var aoes = new AOEInstance[count];
        for (var i = 0; i < count; ++i)
        {
            var t = terminus[i];
            aoes[i] = new(rect, t.Position.Quantized(), t.Rotation);
        }
        return aoes;
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.TerminusEstBoss)
            _active = true;
    }

    public override void OnActorDestroyed(Actor actor)
    {
        if (actor.OID == (uint)OID.TerminusEst)
            _active = false;
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 67203u, NameID = 3818u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class RegulaVanHydrus(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(252.75f, 553f), new ArenaBoundsCircle(19.5f));

