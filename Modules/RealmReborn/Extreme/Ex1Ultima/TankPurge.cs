// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Extreme.Ex1Ultima;

class TankPurge(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.TankPurge)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);

        // execute raid saves; it happens every ~45s, so 2 people in reprisal/feint/addle rotation
        if (Active)
        {
            switch (assignment)
            {
                case PartyRolesConfig.Assignment.MT:
                    if ((NumCasts & 1) == 0)
                        hints.ActionsToExecute.Push(ActionID.MakeSpell(ClassShared.AID.Reprisal), actor, ActionQueue.Priority.High);
                    break;
                case PartyRolesConfig.Assignment.OT:
                    if ((NumCasts & 1) == 1)
                        hints.ActionsToExecute.Push(ActionID.MakeSpell(ClassShared.AID.Reprisal), actor, ActionQueue.Priority.High);
                    break;
                case PartyRolesConfig.Assignment.M1:
                    if ((NumCasts & 1) == 0)
                        hints.ActionsToExecute.Push(ActionID.MakeSpell(ClassShared.AID.Feint), Module.PrimaryActor, ActionQueue.Priority.High);
                    break;
                case PartyRolesConfig.Assignment.M2:
                    if ((NumCasts & 1) == 1)
                        hints.ActionsToExecute.Push(ActionID.MakeSpell(ClassShared.AID.Feint), Module.PrimaryActor, ActionQueue.Priority.High);
                    break;
                case PartyRolesConfig.Assignment.R1:
                    if ((NumCasts & 1) == 0)
                        hints.ActionsToExecute.Push(ActionID.MakeSpell(ClassShared.AID.Addle), Module.PrimaryActor, ActionQueue.Priority.High);
                    break;
                case PartyRolesConfig.Assignment.R2:
                    if ((NumCasts & 1) == 1)
                        hints.ActionsToExecute.Push(ActionID.MakeSpell(ClassShared.AID.Addle), Module.PrimaryActor, ActionQueue.Priority.High);
                    break;
            }
        }
    }
}
