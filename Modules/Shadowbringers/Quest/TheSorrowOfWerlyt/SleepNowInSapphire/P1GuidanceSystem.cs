// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Quest.SorrowOfWerlyt.SleepNowInSapphire.P1GuidanceSystem;

public enum OID : uint
{
    Boss = 0x2DFF, // R4.025
    Helper = 0x233C
}

public enum AID : uint
{
    AerialBombardmentVisual = 21491, // Boss->self, 3.0s cast, single-target
    AerialBombardment = 21492, // Helper->location, 2.5s cast, range 12 circle
}

class AerialBombardment(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AerialBombardment, 12);

class GuidanceSystemStates : StateMachineBuilder
{
    public GuidanceSystemStates(ModuleBase module) : base(module)
    {
        TrivialPhase()

            .ActivateOnEnter<AerialBombardment>();
    }
}

[ModuleInfo(CFCID = 69431u, NameID = 9461u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class GuidanceSystem(WorldState ws, Actor primary) : SleepNowInSapphireSharedBounds(ws, primary)
{
    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (actor.FindStatus((uint)Roleplay.SID.PyreticBooster) == null)
            hints.ActionsToExecute.Push(ActionID.MakeSpell(Roleplay.AID.PyreticBooster), actor, ActionQueue.Priority.Medium);
    }
}

public abstract class SleepNowInSapphireSharedBounds(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-15, 610), arena)
{
    private static readonly ArenaBoundsSquare arena = new(59.5f);
}
