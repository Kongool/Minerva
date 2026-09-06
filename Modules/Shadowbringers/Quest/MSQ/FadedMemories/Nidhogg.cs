// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Quest.MSQ.FadedMemories.Nidhogg;

public enum OID : uint
{
    Boss = 0x2F21, // R2.7
    Clone = 0x2F22 // R2.7
}

public enum AID : uint
{
    AutoAttack = 6498, // Boss->player, no cast, single-target

    HighJumpVisual = 21099, // Clone/Boss->self, 4.0s cast, single-target
    HighJump = 21299, // player->self, 4.0s cast, range 8 circle
    Geirskogul = 21098 // Clone/Boss->self, 4.0s cast, range 62 width 8 rect
}

class HighJump(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HighJump, 8f);
class Geirskogul(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Geirskogul, new AOEShapeRect(62f, 4f));

class NidhoggStates : StateMachineBuilder
{
    public NidhoggStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<HighJump>()
            .ActivateOnEnter<Geirskogul>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 69311u, NameID = 3458u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Nidhogg(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-242, 436.5f), new ArenaBoundsCircle(20));
