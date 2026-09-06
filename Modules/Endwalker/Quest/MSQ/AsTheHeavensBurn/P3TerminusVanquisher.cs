// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

using Minerva.Endwalker.Quest.MSQ.AsTheHeavensBurn.P2TerminusLacerator;

namespace Minerva.Endwalker.Quest.MSQ.AsTheHeavensBurn.P3TerminusVanquisher;

public enum OID : uint
{
    Boss = 0x35EE, // R6.0
    TerminusVanquisher = 0x35EF, // R4.2
    SparkSphere = 0x35F0, // R1.3
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack1 = 872, // Boss->tank, no cast, single-target
    AutoAttack2 = 27028, // TerminusVanquisher->tank, no cast, single-target
    TheBlackDeath = 27010, // Boss->self, no cast, range 25 120-degree cone
    Teleport = 27033, // TerminusVanquisher->location, no cast, single-target

    BlackStarVisual = 27011, // Boss->self, 5.0s cast, single-target
    BlackStar = 27012, // Helper->location, 6.0s cast, range 40 circle

    DeadlyImpactVisual = 27013, // Boss->self, 4.0s cast, single-target
    DeadlyImpact = 27014, // Helper->location, 7.0s cast, range 10 circle
    ForcefulImpactAOE = 26239, // TerminusVanquisher->location, 5.0s cast, range 7 circle
    ForcefulImpactKB = 27030, // Helper->self, 5.6s cast, range 20 circle

    MutableLawsVisual = 27039, // TerminusVanquisher->self, 4.0s cast, single-target
    MutableLawsBig = 27041, // Helper->location, 10.0s cast, range 6 circle
    MutableLawsSmall = 27040, // Helper->location, 10.0s cast, range 6 circle
    AccursedTongueVisual = 27037, // TerminusVanquisher->self, 4.0s cast, single-target
    AccursedTongue = 27038, // Helper->all, 5.0s cast, range 6 circle, spread

    Thundercall = 27034, // TerminusVanquisher->self, 4.0s cast, single-target
    Shock = 27035, // SparkSphere->self, 5.0s cast, range 10 circle
    Depress = 27036, // TerminusVanquisher->tank, 5.0s cast, range 7 circle
    ForcefulImpact = 27029, // TerminusVanquisher->location, 5.0s cast, range 7 circle

    WaveOfLoathing = 27032, // TerminusVanquisher->self, 5.0s cast, range 40 circle
    ForceOfLoathing = 27031 // TerminusVanquisher->self, no cast, range 10 120-degree cone
}

class TheBlackDeath(ModuleBase module) : Components.Cleave(module, (uint)AID.TheBlackDeath, new AOEShapeCone(25f, 60f.Degrees()), [(uint)OID.Boss], activeWhileCasting: false);
class ForceOfLoathing(ModuleBase module) : Components.Cleave(module, (uint)AID.ForceOfLoathing, new AOEShapeCone(10f, 60f.Degrees()), [(uint)OID.TerminusVanquisher], activeWhileCasting: false);
class DeadlyImpact(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DeadlyImpact, 10f, 6);
class BlackStar(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.BlackStar);

class ForcefulImpact(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ForcefulImpactAOE, 7f);
class ForcefulImpactKB(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.ForcefulImpactKB, 10f, stopAtWall: true)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
            hints.AddPredictedDamage(World.Party.WithSlot(false, true).Mask(), Casters.Ref(0).Activation);
    }
}
class MutableLaws1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MutableLawsBig, 15f);
class MutableLaws2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MutableLawsSmall, 6f);
class AccursedTongue(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.AccursedTongue, 6f);
class ForcefulImpact2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ForcefulImpact, 7f);
class Shock(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Shock, 10f, 6);
class Depress(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.Depress, 7f);

class TerminusVanquisherStates : StateMachineBuilder
{
    private readonly TerminusVanquisher _module;

    public TerminusVanquisherStates(TerminusVanquisher module) : base(module)
    {
        _module = module;

        TrivialPhase()
            .ActivateOnEnter<TheBlackDeath>()
            .ActivateOnEnter<DeadlyImpact>()
            .ActivateOnEnter<BlackStar>();
        TrivialPhase(1)
            .ActivateOnEnter<ForceOfLoathing>()
            .ActivateOnEnter<ForcefulImpact>()
            .ActivateOnEnter<ForcefulImpactKB>()
            .ActivateOnEnter<MutableLaws1>()
            .ActivateOnEnter<MutableLaws2>()
            .ActivateOnEnter<AccursedTongue>()
            .ActivateOnEnter<ForcefulImpact2>()
            .ActivateOnEnter<Shock>()
            .ActivateOnEnter<Depress>()
            .Raw.Update = () => _module.BossP2?.IsDeadOrDestroyed ?? false;
    }
}

[ModuleInfo(CFCID = 804u, NameID = 10935u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class TerminusVanquisher(WorldState ws, Actor primary) : ModuleBase(ws, primary, TerminusLacerator.ArenaBounds.Center, TerminusLacerator.ArenaBounds)
{
    public Actor? BossP2 => Enemies((uint)OID.TerminusVanquisher)[0];

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actor(BossP2);
    }
}

