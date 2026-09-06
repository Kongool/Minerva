// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.HallOfTheNovice.NoviceBasicFinal;

public enum OID : uint
{
    Boss = 0x1591,
    Helper = 0x233C,
}

class N12AttackAsTargeted(ModuleBase module) : ModuleComponent(module)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        foreach (var stateActor in World.Actors)
        {
            if (stateActor.OID == (uint)OID.Boss)
                hints.SetPriority(stateActor, 1);
        }
    }
}

class NoviceBasicFinalStates : StateMachineBuilder
{
    public NoviceBasicFinalStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
           .ActivateOnEnter<N12AttackAsTargeted>();
    }
}

[ModuleInfo(CFCID = 166u, NameID = 4787u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "erdelf (ported from BMR)")]
public class NoviceBasicFinal(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(457.383f, 271.721f), new ArenaBoundsCircle(20));
