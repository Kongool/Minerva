// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Hunt.RankS.CrystalIncarnation;

public enum OID : uint
{
    Boss = 0x4571 // R2.4
}

public enum AID : uint
{
    AutoAttack = 39622, // Boss->player, no cast, single-target

    FireII = 39623, // Boss->location, 5.0s cast, range 5 circle
    BlizzardII = 39624 // Boss->self, 5.0s cast, range 40 45-degree cone
}

sealed class FireII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FireII, 5f);
sealed class BlizzardII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BlizzardII, new AOEShapeCone(40f, 22.5f.Degrees()));

sealed class CrystalIncarnationStates : StateMachineBuilder
{
    public CrystalIncarnationStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<FireII>()
            .ActivateOnEnter<BlizzardII>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 13407u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class CrystalIncarnation(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
