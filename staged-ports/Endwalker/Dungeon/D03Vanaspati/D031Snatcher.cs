// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Dungeon.D03Vanaspati.D031Snatcher;

public enum OID : uint
{
    Boss = 0x33E8, // R=3.99
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target

    LastGasp = 25141, // Boss->player, 5.0s cast, single-target
    LostHope = 25143, // Boss->self, 4.0s cast, range 20 circle, applies temporary misdirection
    MouthOff = 25137, // Boss->self, 3.0s cast, single-target
    NoteOfDespair = 25144, // Boss->self, 5.0s cast, range 40 circle
    Vitriol = 25138, // Helper->self, 9.0s cast, range 13 circle
    Wallow = 25142, // Helper->player, 5.0s cast, range 6 circle
    WhatIsLeft = 25140, // Boss->self, 8.0s cast, range 20 180-degree cone
    WhatIsRight = 25139 // Boss->self, 8.0s cast, range 20 180-degree cone
}

class Cleave(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(40f, 90f.Degrees()));
class WhatIsLeft(ModuleBase module) : Cleave(module, (uint)AID.WhatIsLeft);
class WhatIsRight(ModuleBase module) : Cleave(module, (uint)AID.WhatIsRight);

class LostHope(ModuleBase module) : Components.TemporaryMisdirection(module, (uint)AID.LostHope);
class Vitriol(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Vitriol, 13f);
class NoteOfDespair(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.NoteOfDespair);
class Wallow(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.Wallow, 6f);
class LastGasp(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.LastGasp);

class D031SnatcherStates : StateMachineBuilder
{
    public D031SnatcherStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<WhatIsLeft>()
            .ActivateOnEnter<WhatIsRight>()
            .ActivateOnEnter<LostHope>()
            .ActivateOnEnter<Vitriol>()
            .ActivateOnEnter<NoteOfDespair>()
            .ActivateOnEnter<Wallow>()
            .ActivateOnEnter<LastGasp>();
    }
}

[ModuleInfo(CFCID = 789u, NameID = 10717u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (LTS, Malediktus) (ported from BMR)")]
public class D031Snatcher(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(-375f, 85f), 19.5f * CosPI.Pi36th, 36)],
    [new Rectangle(new(-375f, 105f), 20f, 1.2f), new Rectangle(new(-375f, 61f), 20f, 2f, -30f.Degrees())]);
}
