// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.HallOfTheNovice.NoviceDPS03;

public enum OID : uint
{
    Boss = 0x1571,
    Helper = 0x233C,
    N12Gladiator = 0x1574
}

class AttackAsTargeted(ModuleBase module) : Components.GenericInvincible(module)
{
    protected override ReadOnlySpan<Actor> ForbiddenTargets(int slot, Actor actor)
    {
        var gladiator = World.Actors.FirstOrDefault(a => a.OID == (uint)OID.N12Gladiator);
        if (gladiator != null && gladiator.TargetID != 0)
        {
            var target = World.Actors.Find(gladiator.TargetID);
            var excludedTarget = World.Actors.Exclude(target);
            return CollectionsMarshal.AsSpan<Actor>([.. excludedTarget]);
            // TODO original statement
            //return World.Actors.Exclude(target);
        }
        return [];
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var gladiator = World.Actors.FirstOrDefault(a => a.OID == (uint)OID.N12Gladiator);
        if (gladiator != null && gladiator.TargetID != 0)
        {
            var target = World.Actors.Find(gladiator.TargetID);
            hints.SetPriority(target, 1);
        }
    }
}

class NoviceDPS03States : StateMachineBuilder
{
    public NoviceDPS03States(ModuleBase module) : base(module)
    {
        TrivialPhase()
           .ActivateOnEnter<AttackAsTargeted>();
    }
}

[ModuleInfo(CFCID = 159u, NameID = 4784u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "erdelf (ported from BMR)")]
public class NoviceDPS03(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(0, 0), new ArenaBoundsCircle(20));
