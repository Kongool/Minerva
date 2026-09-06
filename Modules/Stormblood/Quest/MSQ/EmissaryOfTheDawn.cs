// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Quest.MSQ.EmissaryOfTheDawn;

public enum OID : uint
{
    Boss = 0x234B,
    Helper = 0x233C
}

class LB(ModuleBase module) : ModuleComponent(module)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (World.Actors.Any(x => x.OID == 0x2340 && x.FindStatus(1497) != null))
            hints.ActionsToExecute.Push(ActionID.MakeSpell(Roleplay.AID.Starstorm), null, ActionQueue.Priority.VeryHigh, targetPos: new Vector3(Center.X, 0, Center.Z));
    }
}

class HostileSkyArmorStates : StateMachineBuilder
{
    public HostileSkyArmorStates(ModuleBase module) : base(module)
    {
        TrivialPhase()

            .ActivateOnEnter<LB>()
            .Raw.Update = () => module.World.CurrentCFCID != 582;
    }
}

[ModuleInfo(CFCID = 68612u, NameID = 7257u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class HostileSkyArmor(WorldState ws, Actor primary) : ModuleBase(ws, primary, default, new ArenaBoundsCircle(20f))
{
    protected override void DrawEnemies(int pcSlot, Actor pc) => Arena.Actors(World.Actors.Where(x => !x.IsAlly));
}

