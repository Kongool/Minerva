// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Quest.MSQ.AFeastOfLies;

public enum OID : uint
{
    Boss = 0x295A,
    Helper = 0x233C
}

public enum AID : uint
{
    UnceremoniousBeheading = 16274, // Boss->self, 4.0s cast, range 10 circle
    KatunCycle = 16275, // Boss->self, 4.0s cast, range 5-40 donut

    Evisceration = 16277, // Boss->self, 4.5s cast, range 40 120-degree cone
    HotPursuit = 16291, // Boss->self, 2.5s cast, single-target
    HotPursuit1 = 16285, // 29E6->location, 3.0s cast, range 5 circle

    NexusOfThunder = 16280, // Boss->self, 2.5s cast, single-target
    NexusOfThunder1 = 16276, // 29E6->self, 4.3s cast, range 45 width 5 rect
    NexusOfThunder2 = 16296, // 29E6->self, 6.3s cast, range 45 width 5 rect

    LivingFlame = 16294, // Boss->self, 3.0s cast, single-target
    Spiritcall = 16292, // Boss->self, 3.0s cast, range 40 circle
    Burn = 16290, // 29C2->self, 4.5s cast, range 8 circle
    RisingThunder = 16293, // Boss->self, 3.0s cast, single-target
    Electrocution = 16286, // 295B->self, 10.0s cast, range 6 circle
    ShatteredSkyVisual = 17191, // Boss->self, 4.0s cast, single-target
    ShatteredSky = 16282, // 29E6->self, 0.5s cast, range 40 circle
    MercilessLeftVisual = 16279, // Boss->self, 4.0s cast, single-target
    MercilessLeft1 = 16298, // 29FC->self, 3.8s cast, range 40 120-degree cone
    MercilessLeft2 = 16297, // 29FD->self, 4.2s cast, range 40 120-degree cone
    MercilessRightVisual = 16278, // Boss->self, 4.0s cast, single-target
    MercilessRight1 = 16283, // 29FB->self, 3.8s cast, range 40 120-degree cone
    MercilessRight2 = 16284, // 29FE->self, 4.2s cast, range 40 120-degree cone
}

class UnceremoniousBeheading(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.UnceremoniousBeheading, 10);
class KatunCycle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.KatunCycle, new AOEShapeDonut(5, 40));

abstract class Cleaves(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(40, 60.Degrees()));
class MercilessRight(ModuleBase module) : Cleaves(module, (uint)AID.MercilessRight1);
class MercilessRight1(ModuleBase module) : Cleaves(module, (uint)AID.MercilessRight2);
class MercilessLeft(ModuleBase module) : Cleaves(module, (uint)AID.MercilessLeft1);
class MercilessLeft1(ModuleBase module) : Cleaves(module, (uint)AID.MercilessLeft2);
class Evisceration(ModuleBase module) : Cleaves(module, (uint)AID.Evisceration);

class HotPursuit(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HotPursuit1, 5);

abstract class NoT(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeRect(45, 2.5f));
class NexusOfThunder1(ModuleBase module) : NoT(module, (uint)AID.NexusOfThunder1);
class NexusOfThunder2(ModuleBase module) : NoT(module, (uint)AID.NexusOfThunder2);

class Burn(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Burn, 8, 5);
class Spiritcall(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.Spiritcall, 20, stopAtWall: true);

class Electrocution(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Electrocution, 6)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count == 12)
        {
            var enemy = hints.PotentialTargets.Where(x => x.Actor.OID == 0x295B).MinBy(e => actor.DistanceToHitbox(e.Actor));
            for (var i = 0; i < hints.PotentialTargets.Count; ++i)
            {
                var e = hints.PotentialTargets[i];
                e.Priority = e == enemy ? 1 : 0;
            }
        }
        else
        {
            base.AddAIHints(slot, actor, assignment, hints);
        }
    }
}

class SerpentHead(ModuleBase module) : Components.Adds(module, 0x29E8, 1);

class RanjitStates : StateMachineBuilder
{
    public RanjitStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<UnceremoniousBeheading>()
            .ActivateOnEnter<KatunCycle>()
            .ActivateOnEnter<MercilessRight>()
            .ActivateOnEnter<MercilessRight1>()
            .ActivateOnEnter<MercilessLeft>()
            .ActivateOnEnter<MercilessLeft1>()
            .ActivateOnEnter<Evisceration>()
            .ActivateOnEnter<HotPursuit>()
            .ActivateOnEnter<NexusOfThunder1>()
            .ActivateOnEnter<NexusOfThunder2>()
            .ActivateOnEnter<Burn>()
            .ActivateOnEnter<Electrocution>()
            .ActivateOnEnter<Spiritcall>()
            .ActivateOnEnter<SerpentHead>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 69167u, NameID = 8374u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Ranjit(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(0, 18), new ArenaBoundsCircle(15));
