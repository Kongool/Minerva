// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C03AAI.C030Trash2;

abstract class Tornado(ModuleBase module, uint aid) : Components.SpreadFromCastTargets(module, aid, 4f);
class NTornado(ModuleBase module) : Tornado(module, (uint)AID.NTornado);
class STornado(ModuleBase module) : Tornado(module, (uint)AID.STornado);

abstract class Ovation(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeRect(12f, 2f));
class NOvation(ModuleBase module) : Ovation(module, (uint)AID.NOvation);
class SOvation(ModuleBase module) : Ovation(module, (uint)AID.SOvation);

class C030WoodGolemStates : StateMachineBuilder
{
    private readonly bool _savage;

    public C030WoodGolemStates(ModuleBase module, bool savage) : base(module)
    {
        _savage = savage;
        DeathPhase(0, SinglePhase);
    }

    private void SinglePhase(uint id)
    {
        AncientAero(id, 11.5f);
        Tornado(id + 0x10000, 2);
        Ovation(id + 0x20000, 2.7f);
        SimpleState(id + 0xFF0000, 10, "???");
    }

    private void AncientAero(uint id, float delay)
    {
        CastStart(id, _savage ? (uint)AID.SAncientAero : (uint)AID.NAncientAero, delay);

        var castEnd = SimpleState(id + 1, 5, "Interruptible raidwide"); // note: we use custom state instead of cast-end, since cast-end happens whenever anyone presses interrupt - and if not interrupted, spell finish can be slightly delayed
        castEnd.Raw.Comment = "Interruptible cast end";
        castEnd.Raw.Update = timeSinceTransition => Module.PrimaryActor.CastInfo == null && timeSinceTransition >= castEnd.Raw.Duration ? 0 : -1;
    }

    private void Tornado(uint id, float delay)
    {
        Cast(id, _savage ? (uint)AID.STornado : (uint)AID.NTornado, delay, 5, "Spread")
            .ActivateOnEnter<NTornado>(!_savage)
            .ActivateOnEnter<STornado>(_savage)
            .DeactivateOnExit<Tornado>();
    }

    private void Ovation(uint id, float delay)
    {
        Cast(id, _savage ? (uint)AID.SOvation : (uint)AID.NOvation, delay, 4, "Line")
            .ActivateOnEnter<NOvation>(!_savage)
            .ActivateOnEnter<SOvation>(_savage)
            .DeactivateOnExit<Ovation>();
    }
}
class C030NWoodGolemStates(ModuleBase module) : C030WoodGolemStates(module, false);
class C030SWoodGolemStates(ModuleBase module) : C030WoodGolemStates(module, true);

[ModuleInfo(CFCID = 979u, NameID = 12560u, PrimaryActorOID = (uint)OID.NWoodGolem, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class C030NWoodGolem(WorldState ws, Actor primary) : C030Trash2(ws, primary);

[ModuleInfo(CFCID = 980u, NameID = 12560u, PrimaryActorOID = (uint)OID.SWoodGolem, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class C030SWoodGolem(WorldState ws, Actor primary) : C030Trash2(ws, primary);
