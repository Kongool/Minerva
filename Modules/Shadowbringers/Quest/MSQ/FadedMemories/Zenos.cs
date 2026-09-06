// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Quest.MSQ.FadedMemories.Zenos;

public enum OID : uint
{
    Boss = 0x2F28, // R0.92
    SpecterOfZenos = 0x2F29, // R0.92
    TheStorm = 0x2F2B, // R3.0
    TheSwell = 0x2F2A, // R3.0
    AmeNoHabakiri = 0x2F2C, // R3.0
    DimensionalTear = 0x2F2D // R1.5
}

public enum AID : uint
{
    EntropicFlameVisual = 21116, // SpecterOfZenos->self, 5.0s cast, single-target
    EntropicFlame = 21117, // Helper->self, 5.0s cast, range 50 width 8 rect
    VeinSplitter = 21118, // SpecterOfZenos->self, 5.0s cast, range 10 circle

    TheFinalArtVisual = 21120, // Boss->self, no cast, single-target
    TheFinalArt = 21121, // player->self, 7.0s cast, range 100 circle
    SwordDespawn = 21333, // TheSwell/TheStorm/AmeNoHabakiri->self, no cast, single-target
    Darkblight = 21122 // DimensionalTear->self, no cast, range 100 circle
}

class EntropicFlame(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.EntropicFlame, new AOEShapeRect(50f, 4f));
class VeinSplitter(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.VeinSplitter, 10f);
class TheFinalArt(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.TheFinalArt);

class ZenosYaeGalvusStates : StateMachineBuilder
{
    public ZenosYaeGalvusStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<EntropicFlame>()
            .ActivateOnEnter<TheFinalArt>()
            .ActivateOnEnter<VeinSplitter>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 69311u, NameID = 6039u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class ZenosYaeGalvus(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-321.03f, 617.73f), new ArenaBoundsCircle(20f))
{
    public static readonly uint[] swords = [(uint)OID.TheStorm, (uint)OID.TheSwell, (uint)OID.AmeNoHabakiri];

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(this, swords);
    }

    protected override bool CheckPull() => IsAnyActorInCombat(swords);
}
