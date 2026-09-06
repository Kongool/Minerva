// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.HallOfTheNovice.NoviceTank04;

public enum OID : uint
{
    Boss = 0x1564,
    Helper = 0x233C,
}

class Tank04Dummies(ModuleBase module) : ModuleComponent(module)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);

        var count = 0;
        float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;

        foreach (var stateActor in World.Actors)
        {
            if (stateActor.OID == (uint)OID.Boss)
            {
                count++;
                minX = Math.Min(minX, stateActor.Position.X);
                minZ = Math.Min(minZ, stateActor.Position.Z);
                maxX = Math.Max(maxX, stateActor.Position.X);
                maxZ = Math.Max(maxZ, stateActor.Position.Z);
                hints.SetPriority(stateActor, AIHints.Enemy.PriorityForbidden);
            }
        }

        if (count == 3)
        {
            var target = new WPos((maxX - minX) / 2f + minX, (maxZ - minZ) / 2f + minZ);
            hints.GoalZones.Add(AIHints.GoalSingleTarget(target, 1f));

            if (actor.DistanceToPoint(target) < 1f)
            {
                hints.ActionsToExecute.Clear();
                hints.ActionsToExecute.Push(new ActionID(ActionType.Spell, actor.Class switch
                {
                    Class.MRD or Class.WAR => 41u,
                    Class.GLA or Class.PLD => 7381u,
                    Class.DRK => 3621u,
                    _ => 11u
                }), null, 1f);
            }
        }
    }
}

class NoviceTank04States : StateMachineBuilder
{
    public NoviceTank04States(ModuleBase module) : base(module)
    {
        TrivialPhase()
           .ActivateOnEnter<Tank04Dummies>();
    }
}

[ModuleInfo(CFCID = 154u, NameID = 541u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "erdelf (ported from BMR)")]
public class NoviceTank04(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(0, 0), new ArenaBoundsCircle(20))
{
    protected override bool CheckPull() => PrimaryActor.IsTargetable;
}
