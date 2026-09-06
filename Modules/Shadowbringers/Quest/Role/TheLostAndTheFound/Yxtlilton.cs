// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Quest.Role.TheLostAndTheFound.Yxtlilton;

public enum OID : uint
{
    Boss = 0x29B0,
    Helper = 0x233C
}

public enum AID : uint
{
    TheCodexOfDarknessII = 17010, // Boss->self, 3.0s cast, range 100 circle
    TheCodexOfGravity = 17014, // Boss->player, 4.5s cast, range 6 circle
}

class CodexOfDarknessII(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.TheCodexOfDarknessII);
class CodexOfGravity(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.TheCodexOfGravity, 6f)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        if (Stacks.Count != 0)
            hints.AddForbiddenZone(new SDInvertedCircle(Center, 1.5f), Stacks[0].Activation);
    }
}

class YxtliltonStates : StateMachineBuilder
{
    public YxtliltonStates(ModuleBase module) : base(module)
    {
        TrivialPhase()

            .ActivateOnEnter<CodexOfDarknessII>()
            .ActivateOnEnter<CodexOfGravity>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 68806u, NameID = 8393u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Yxtlilton(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-120f, -770f), new ArenaBoundsCircle(20f))
{
    protected override void DrawEnemies(int pcSlot, Actor pc) => Arena.Actors(World.Actors.Where(x => !x.IsAlly));
}
