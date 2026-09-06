// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.HallOfTheNovice.NoviceDPS05;

public enum OID : uint
{
    Boss = 0x157A,
    Helper = 0x233C,
}

class AttackReinforcements(ModuleBase module) : ModuleComponent(module)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        foreach (var stateActor in World.Actors)
        {
            if (stateActor.OID != (uint)OID.Boss)
                hints.SetPriority(stateActor, 1);
        }
    }
}

class NoviceDPS05States : StateMachineBuilder
{
    public NoviceDPS05States(ModuleBase module) : base(module)
    {
        TrivialPhase()
           .ActivateOnEnter<AttackReinforcements>();
    }
}

[ModuleInfo(CFCID = 161u, NameID = 4784u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "erdelf (ported from BMR)")]
public class NoviceDPS05(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(0, 0), new ArenaBoundsCircle(20));
