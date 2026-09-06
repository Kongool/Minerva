// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C03AAI.C030Trash2;

abstract class GravityForce(ModuleBase module, uint aid) : Components.StackWithCastTargets(module, aid, 6, 4, 4);
class NGravityForce(ModuleBase module) : GravityForce(module, (uint)AID.NGravityForce);
class SGravityForce(ModuleBase module) : GravityForce(module, (uint)AID.SGravityForce);

abstract class IsleDrop(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, 6);
class NIsleDrop(ModuleBase module) : IsleDrop(module, (uint)AID.NIsleDrop);
class SIsleDrop(ModuleBase module) : IsleDrop(module, (uint)AID.SIsleDrop);

class C030IslekeeperStates : StateMachineBuilder
{
    private readonly bool _savage;

    public C030IslekeeperStates(ModuleBase module, bool savage) : base(module)
    {
        _savage = savage;
        DeathPhase(0, SinglePhase);
    }

    private void SinglePhase(uint id)
    {
        AncientQuaga(id, 11.9f);
        GravityForce(id + 0x10000, 6.3f);
        IsleDrop(id + 0x20000, 2.1f);
        AncientQuaga(id + 0x30000, 8.5f);
        Cast(id + 0x40000, _savage ? (uint)AID.SAncientQuagaEnrage : (uint)AID.NAncientQuagaEnrage, 4.1f, 10, "Enrage");
    }

    private void AncientQuaga(uint id, float delay)
    {
        Cast(id, _savage ? (uint)AID.SAncientQuaga : (uint)AID.NAncientQuaga, delay, 5, "Raidwide")
            .SetHint(StateMachine.StateHint.Raidwide);
    }

    private void GravityForce(uint id, float delay)
    {
        Cast(id, _savage ? (uint)AID.SGravityForce : (uint)AID.NGravityForce, delay, 5, "Stack")
            .ActivateOnEnter<NGravityForce>(!_savage)
            .ActivateOnEnter<SGravityForce>(_savage)
            .DeactivateOnExit<GravityForce>();
    }

    private void IsleDrop(uint id, float delay)
    {
        Cast(id, _savage ? (uint)AID.SIsleDrop : (uint)AID.NIsleDrop, delay, 5, "Puddle")
            .ActivateOnEnter<NIsleDrop>(!_savage)
            .ActivateOnEnter<SIsleDrop>(_savage)
            .DeactivateOnExit<IsleDrop>();
    }
}
class C030NIslekeeperStates(ModuleBase module) : C030IslekeeperStates(module, false);
class C030SIslekeeperStates(ModuleBase module) : C030IslekeeperStates(module, true);

[ModuleInfo(CFCID = 979u, NameID = 12561u, PrimaryActorOID = (uint)OID.NIslekeeper, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class C030NIslekeeper(WorldState ws, Actor primary) : C030Trash2(ws, primary);

[ModuleInfo(CFCID = 980u, NameID = 12561u, PrimaryActorOID = (uint)OID.SIslekeeper, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class C030SIslekeeper(WorldState ws, Actor primary) : C030Trash2(ws, primary);
