// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Trial.T02ZoraalJa;

sealed class SoulOverflowCalamitysEdge(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.SoulOverflow1, (uint)AID.SoulOverflow1, (uint)AID.CalamitysEdge]);
sealed class PatricidalPique(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.PatricidalPique);
sealed class Burst(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Burst, 8f);

sealed class VorpalTrail(ModuleBase module) : Components.SimpleChargeAOEGroups(module, [(uint)AID.VorpalTrail1, (uint)AID.VorpalTrail2], 2f);

sealed class T02ZoraalJaStates : StateMachineBuilder
{
    public T02ZoraalJaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<SoulOverflowCalamitysEdge>()
            .ActivateOnEnter<DoubleEdgedSwords>()
            .ActivateOnEnter<PatricidalPique>()
            .ActivateOnEnter<Burst>()
            .ActivateOnEnter<VorpalTrail>()
            .Raw.Update = () => module.PrimaryActor.IsDeadOrDestroyed || !module.PrimaryActor.IsTargetable;
    }
}

public abstract class ZoraalJa(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), GetDefaultBounds())
{
    public static ArenaBoundsSquare GetDefaultBounds() => new(20f, 45f.Degrees());
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 995u, CFCID = 995u, NameID = 12881u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class T02ZoraalJa(WorldState ws, Actor primary) : ZoraalJa(ws, primary);
