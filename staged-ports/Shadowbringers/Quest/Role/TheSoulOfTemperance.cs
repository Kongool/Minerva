// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Quest.Role.TheSoulOfTemperance;

public enum OID : uint
{
    Boss = 0x29CE,
    BossP2 = 0x29D0,
    Helper = 0x233C,
}

public enum AID : uint
{
    SanctifiedAero1 = 16911, // 2A0C->self, 4.0s cast, range 40+R width 6 rect
    SanctifiedStone = 17322, // 29D0->self, 5.0s cast, single-target
    HolyBlur = 17547, // 2969/29CF/274F/296A/2996->self, 5.0s cast, range 40 circle
    Focus = 17548, // 29CF/296A/2996/2969->players, 5.0s cast, width 4 rect charge
    TemperedVirtue = 15928, // BossP2->self, 6.0s cast, range 15 circle
    WaterAndWine = 15604, // 2AF1->self, 5.0s cast, range 12 circle
    ForceOfRestraint = 15603, // 2AF1->self, 5.0s cast, range 60+R width 4 rect
    SanctifiedHoly1 = 16909, // BossP2->self, 4.0s cast, range 8 circle
    SanctifiedHoly2 = 17604, // 2A0C->location, 4.0s cast, range 6 circle
}

class SanctifiedHoly1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SanctifiedHoly1, 8f);
class SanctifiedHoly2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SanctifiedHoly2, 6f);
class ForceOfRestraint(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ForceOfRestraint, new AOEShapeRect(60f, 2f));
class HolyBlur(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.HolyBlur);
class Focus(ModuleBase module) : Components.BaitAwayChargeCast(module, (uint)AID.Focus, 2f);
class TemperedVirtue(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TemperedVirtue, 15f);
class WaterAndWine(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WaterAndWine, new AOEShapeDonut(6f, 12f));
class SanctifiedStone(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.SanctifiedStone, 5f, 1);

class SanctifiedAero(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SanctifiedAero1, new AOEShapeRect(40.5f, 3f));

class Repose(ModuleBase module) : ModuleComponent(module)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        static bool SleepProof(Actor a)
        {
            if (a.Statuses.Any(x => x.ID is 1967u or 1968u))
                return true;

            return a.PendingStatuses.Any(s => s.StatusId == 3);
        }

        if (World.Actors.FirstOrDefault(x => x.IsTargetable && !x.IsAlly && x.OID != (uint)OID.Boss && !SleepProof(x)) is Actor e)
            hints.ActionsToExecute.Push(ActionID.MakeSpell(WHM.AID.Repose), e, ActionQueue.Priority.VeryHigh, castTime: 2.5f);
    }
}

class SophrosyneStates : StateMachineBuilder
{
    public SophrosyneStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<HolyBlur>()
            .ActivateOnEnter<Focus>()
            .ActivateOnEnter<Repose>()
            .Raw.Update = () => module.Enemies((uint)OID.BossP2).Any(x => x.IsTargetable) || module.World.CurrentCFCID != 673;
        TrivialPhase(1)
            .ActivateOnEnter<SanctifiedAero>()
            .ActivateOnEnter<SanctifiedStone>()
            .ActivateOnEnter<TemperedVirtue>()
            .ActivateOnEnter<WaterAndWine>()
            .ActivateOnEnter<ForceOfRestraint>()
            .ActivateOnEnter<SanctifiedHoly1>()
            .ActivateOnEnter<SanctifiedHoly2>()
            .Raw.Update = () => module.Enemies((uint)OID.BossP2).All(x => x.IsDeadOrDestroyed) || module.World.CurrentCFCID != 673;
    }
}

[ModuleInfo(CFCID = 68808u, NameID = 8777u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Sophrosyne(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-651.8f, -127.25f), new ArenaBoundsCircle(20))
{
    protected override void DrawEnemies(int pcSlot, Actor pc) => Arena.Actors(World.Actors.Where(x => !x.IsAlly));
}
