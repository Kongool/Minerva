// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Quest.Role.TheLostAndTheFound.Sophrosyne;

public enum OID : uint
{
    Boss = 0x29AA,
    Helper = 0x233C,
}

public enum AID : uint
{
    Charge = 16999, // 29AB->29A9, 3.0s cast, width 4 rect charge
}

class Charge(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.Charge, 2);

class SophrosyneStates : StateMachineBuilder
{
    public SophrosyneStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Charge>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 68806u, NameID = 8395u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Sophrosyne(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(632, 64.15f), new ArenaBoundsCircle(20))
{
    protected override void DrawEnemies(int pcSlot, Actor pc) => Arena.Actors(World.Actors.Where(x => !x.IsAlly));
}
