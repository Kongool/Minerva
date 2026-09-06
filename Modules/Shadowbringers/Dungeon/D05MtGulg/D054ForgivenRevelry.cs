// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Dungeon.D05MtGulg.D054ForgivenRevelry;

public enum OID : uint
{
    Boss = 0x28F3, //R=7.5
    Brightsphere = 0x2947, //R=1.0
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 16246, // Boss->player, no cast, single-target

    LeftPalmVisual = 16249, // Boss->self, no cast, single-target
    LeftPalm = 16250, // Helper->self, 4.5s cast, range 30 width 15 rect
    RightPalmVisual = 16247, // Boss->self, no cast, single-target
    RightPalm = 16248, // Helper->self, 4.5s cast, range 30 width 15 rect

    LightShot = 16251 // Brightsphere->self, 4.0s cast, range 40 width 4 rect
}

abstract class Palm(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeRect(30f, 7.5f));
class LeftPalm(ModuleBase module) : Palm(module, (uint)AID.LeftPalm);
class RightPalm(ModuleBase module) : Palm(module, (uint)AID.RightPalm);

class LightShot(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LightShot, new AOEShapeRect(40f, 2f));

class D054ForgivenRevelryStates : StateMachineBuilder
{
    public D054ForgivenRevelryStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<LeftPalm>()
            .ActivateOnEnter<RightPalm>()
            .ActivateOnEnter<LightShot>();
    }
}

[ModuleInfo(CFCID = 659u, NameID = 8270u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class D054ForgivenRevelry(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-240f, 176.3f), new ArenaBoundsRect(14.65f, 14.4f));
