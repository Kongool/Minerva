// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Dungeon.D05MtGulg.D050ForgivenPrejudice;

public enum OID : uint
{
    Boss = 0x28F2, //R=3.6-5.112
    ForgivenConformity = 0x28EE, //R=1.65
    ForgivenExtortion = 0x28EF, //R=2.7-4.509
    ForgivenVenery = 0x28F1, //R=1.6-2.0
    ForgivenApathy = 0x28F0 //R=8.4
}

public enum AID : uint
{
    AutoAttack = 870, // ForgivenExtortion/ForgivenVenery->player, no cast, single-target
    AutoAttack2 = 872, // Boss->player, no cast, single-target
    RavenousBite = 16812, // ForgivenExtortion->player, no cast, single-target
    SanctifiedAero = 16813, // ForgivenVenery->self, 5.0s cast, range 40 width 8 rect
    PunitiveLight = 16815, // Boss->self, 5.0s cast, range 20 circle
    Sanctification = 16814 // Boss->self, 5.0s cast, range 12 90-degree cone
}

class SanctifiedAero(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SanctifiedAero, new AOEShapeRect(40, 4));
class PunitiveLight(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.PunitiveLight, true, true, "Raidwide", true);
class Sanctification(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Sanctification, new AOEShapeCone(12, 45.Degrees()));

class D050ForgivenPrejudiceStates : StateMachineBuilder
{
    public D050ForgivenPrejudiceStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Sanctification>()
            .ActivateOnEnter<PunitiveLight>()
            .ActivateOnEnter<SanctifiedAero>()
            .Raw.Update = () => module.Enemies((uint)OID.Boss).All(e => e.IsDead) && module.Enemies((uint)OID.ForgivenVenery).All(e => e.IsDead) && module.Enemies((uint)OID.ForgivenExtortion).All(e => e.IsDead) && module.Enemies((uint)OID.ForgivenConformity).All(e => e.IsDead) || module.Enemies((uint)OID.ForgivenApathy).Any(e => e.InCombat) || module.Enemies((uint)OID.ForgivenApathy).Any(e => e.IsTargetable);
    }
}

[ModuleInfo(CFCID = 659u, NameID = 8269u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]

public class D050ForgivenPrejudice(WorldState ws, Actor primary) : SimpleBossModule(ws, primary)
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(Enemies((uint)OID.Boss));
        Arena.Actors(Enemies((uint)OID.ForgivenExtortion));
        Arena.Actors(Enemies((uint)OID.ForgivenConformity));
        Arena.Actors(Enemies((uint)OID.ForgivenVenery));
        Arena.Actors(Enemies((uint)OID.ForgivenApathy));
    }

    public override bool CheckReset() => false;
    protected override bool CheckPull() => (!Enemies((uint)OID.ForgivenApathy).Any(e => e.InCombat) || !Enemies((uint)OID.ForgivenApathy).Any(e => e.IsTargetable)) && PrimaryActor.IsTargetable && PrimaryActor.InCombat || Enemies((uint)OID.ForgivenExtortion).Any(e => e.InCombat) || Enemies((uint)OID.ForgivenConformity).Any(e => e.InCombat) || Enemies((uint)OID.ForgivenVenery).Any(e => e.InCombat);
}
