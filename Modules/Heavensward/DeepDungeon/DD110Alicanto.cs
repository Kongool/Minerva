// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.DeepDungeon.PalaceOfTheDead.DD110Alicanto;

public enum OID : uint
{
    Boss = 0x1818 // R6.000, x1
}

public enum AID : uint
{
    AutoAttack = 6497, // Boss->player, no cast, single-target

    Whipcrack = 7128, // Boss->player, no cast, single-target
    Stormwind = 7129, // Boss->self, 3.0s cast, range 12+R 90-degree cone
    Bombination = 7130, // Boss->self, 3.0s cast, range 6+R circle
    Lumisphere = 7131, // Boss->location, 3.0s cast, range 6 circle
    AeroBlast = 7132 // Boss->self, 3.0s cast, range 40+R circle
}

class AeroBlast(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AeroBlast);
class Bombination(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Bombination, 12f);
class Lumisphere(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Lumisphere, 6f);
class Stormwind(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Stormwind, new AOEShapeCone(18f, 45f.Degrees()));

class DD110AlicantoStates : StateMachineBuilder
{
    public DD110AlicantoStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<AeroBlast>()
            .ActivateOnEnter<Bombination>()
            .ActivateOnEnter<Lumisphere>()
            .ActivateOnEnter<Stormwind>();
    }
}

[ModuleInfo(CFCID = 209u, NameID = 5371u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "LegendofIceman (ported from BMR)")]
public class DD110Alicanto(WorldState ws, Actor primary) : ModuleBase(ws, primary, SharedBounds.ArenaBounds2090110.Center, SharedBounds.ArenaBounds2090110);
