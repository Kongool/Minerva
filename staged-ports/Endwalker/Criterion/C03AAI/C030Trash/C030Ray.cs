// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C03AAI.C030Trash1;

abstract class Hydrocannon(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeRect(15f, 3f));
class NHydrocannon(ModuleBase module) : Hydrocannon(module, (uint)AID.NHydrocannon);
class SHydrocannon(ModuleBase module) : Hydrocannon(module, (uint)AID.SHydrocannon);

abstract class Expulsion(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, 8f);
class NExpulsion(ModuleBase module) : Expulsion(module, (uint)AID.NExpulsion);
class SExpulsion(ModuleBase module) : Expulsion(module, (uint)AID.SExpulsion);

abstract class ElectricWhorl(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeDonut(8f, 60f));
class NElectricWhorl(ModuleBase module) : ElectricWhorl(module, (uint)AID.NElectricWhorl);
class SElectricWhorl(ModuleBase module) : ElectricWhorl(module, (uint)AID.SElectricWhorl);

class C030RayStates : StateMachineBuilder
{
    private readonly bool _savage;

    public C030RayStates(ModuleBase module, bool savage) : base(module)
    {
        _savage = savage;
        DeathPhase(0, SinglePhase)
            .ActivateOnEnter<NHydrocannon>(!_savage)
            .ActivateOnEnter<SHydrocannon>(_savage)
            .ActivateOnEnter<NExpulsion>(!_savage)
            .ActivateOnEnter<SExpulsion>(_savage)
            .ActivateOnEnter<NElectricWhorl>(!_savage)
            .ActivateOnEnter<SElectricWhorl>(_savage)
            .ActivateOnEnter<Twister>();
    }

    private void SinglePhase(uint id)
    {
        Hydrocannon(id, 8.3f);
        ExpulsionElectricWhorl(id + 0x10000, 2.1f);
        SimpleState(id + 0xFF0000, 10, "???");
    }

    private void Hydrocannon(uint id, float delay)
    {
        Cast(id, _savage ? (uint)AID.SHydrocannon : (uint)AID.NHydrocannon, delay, 5, "Line AOE");
    }

    private void ExpulsionElectricWhorl(uint id, float delay)
    {
        Cast(id, _savage ? (uint)AID.SExpulsion : (uint)AID.NExpulsion, delay, 5, "Out");
        Cast(id + 0x10, _savage ? (uint)AID.SExpulsion : (uint)AID.NExpulsion, 2.1f, 5, "In");
    }
}
class C030NRayStates(ModuleBase module) : C030RayStates(module, false);
class C030SRayStates(ModuleBase module) : C030RayStates(module, true);

[ModuleInfo(CFCID = 979u, NameID = 12541u, PrimaryActorOID = (uint)OID.NRay, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class C030NRay(WorldState ws, Actor primary) : C030Trash1(ws, primary)
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.NPaddleBiter));
    }
}

[ModuleInfo(CFCID = 980u, NameID = 12541u, PrimaryActorOID = (uint)OID.SRay, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class C030SRay(WorldState ws, Actor primary) : C030Trash1(ws, primary)
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.SPaddleBiter));
    }
}
