// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C03AAI.C030Trash1;

class LeadHook(ModuleBase module) : Components.CastCounterMulti(module, [(uint)AID.NLeadHook, (uint)AID.NLeadHookAOE1,
(uint)AID.NLeadHookAOE2, (uint)AID.SLeadHook, (uint)AID.SLeadHookAOE1, (uint)AID.SLeadHookAOE2]);

abstract class TailScrew(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, 4f);
class NTailScrew(ModuleBase module) : TailScrew(module, (uint)AID.NTailScrew);
class STailScrew(ModuleBase module) : TailScrew(module, (uint)AID.STailScrew);

class C030KiwakinStates : StateMachineBuilder
{
    private readonly bool _savage;

    public C030KiwakinStates(ModuleBase module, bool savage) : base(module)
    {
        _savage = savage;
        DeathPhase(0, SinglePhase)
            .ActivateOnEnter<NTailScrew>(!_savage)
            .ActivateOnEnter<STailScrew>(_savage)
            .ActivateOnEnter<NWater>(!_savage) // note: second pack is often pulled together with first one
            .ActivateOnEnter<SWater>(_savage)
            .ActivateOnEnter<BubbleShowerCrabDribble>()
            .ActivateOnEnter<Twister>();
    }

    private void SinglePhase(uint id)
    {
        LeadHook(id, 8.1f);
        SharpStrike(id + 0x10000, 3.4f);
        TailScrew(id + 0x20000, 4.2f);
        LeadHook(id + 0x30000, 15.1f);
        SimpleState(id + 0xFF0000, 10, "???");
    }

    private void LeadHook(uint id, float delay)
    {
        Cast(id, _savage ? (uint)AID.SLeadHook : (uint)AID.NLeadHook, delay, 4)
            .ActivateOnEnter<LeadHook>();
        ComponentCondition<LeadHook>(id + 2, 0.1f, comp => comp.NumCasts > 0, "Mini tankbuster hit 1")
            .SetHint(StateMachine.StateHint.Tankbuster);
        ComponentCondition<LeadHook>(id + 3, 1.1f, comp => comp.NumCasts > 1)
            .SetHint(StateMachine.StateHint.Tankbuster);
        ComponentCondition<LeadHook>(id + 4, 1.1f, comp => comp.NumCasts > 2, "Mini tankbuster hit 3")
            .DeactivateOnExit<LeadHook>()
            .SetHint(StateMachine.StateHint.Tankbuster);
    }

    private void SharpStrike(uint id, float delay)
    {
        Cast(id, _savage ? (uint)AID.SSharpStrike : (uint)AID.NSharpStrike, delay, 5f, "Tankbuster")
            .SetHint(StateMachine.StateHint.Tankbuster);
    }

    private void TailScrew(uint id, float delay)
    {
        Cast(id, _savage ? (uint)AID.STailScrew : (uint)AID.NTailScrew, delay, 5f, "AOE");
    }
}
class C030NKiwakinStates(ModuleBase module) : C030KiwakinStates(module, false);
class C030SKiwakinStates(ModuleBase module) : C030KiwakinStates(module, true);

[ModuleInfo(CFCID = 979u, NameID = 12632u, PrimaryActorOID = (uint)OID.NKiwakin, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class C030NKiwakin(WorldState ws, Actor primary) : C030Trash1(ws, primary);

[ModuleInfo(CFCID = 980u, NameID = 12632u, PrimaryActorOID = (uint)OID.SKiwakin, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class C030SKiwakin(WorldState ws, Actor primary) : C030Trash1(ws, primary);
