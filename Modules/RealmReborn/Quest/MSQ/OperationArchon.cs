// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Quest.MSQ.OperationArchon;

public enum OID : uint
{
    Boss = 0x38F5, // R1.5
    ImperialPilusPrior = 0x38F7, // R1.5
    ImperialCenturion = 0x38F6, // R1.5
    Helper = 0x233C
}

public enum AID : uint
{
    TartareanShockwaveSmall = 28871, // 38F5->self, 3.0s cast, range 7 circle
    GalesOfTartarus = 28870, // 38F5->self, 3.0s cast, range 30 width 5 rect
    MagitekMissiles = 28865, // 233C->location, 4.0s cast, range 7 circle
    TartareanTomb = 28869, // 233C->self, 8.0s cast, range 11 circle
    DrillShot = 28874, // Boss->self, 3.0s cast, range 30 width 5 rect
    TartareanShockwaveBig = 28877, // Boss->self, 6.0s cast, range 14 circle
    GalesOfTartarus1 = 28876, // Boss->self, 6.0s cast, range 30 width 30 rect
}

class Adds(ModuleBase module) : Components.AddsMulti(module, [(uint)OID.ImperialPilusPrior, (uint)OID.ImperialCenturion]);

class MagitekMissilesTartareanShockwave(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.MagitekMissiles, (uint)AID.TartareanShockwaveSmall], 7f);
class DrillShotGalesOfTartarus(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.DrillShot, (uint)AID.GalesOfTartarus], new AOEShapeRect(30f, 2.5f));
class TartareanShockwaveBig(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TartareanShockwaveBig, 14f);
class GalesOfTartarusBig(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GalesOfTartarus1, new AOEShapeRect(30f, 15f));
class DirectionalParry(ModuleBase module) : Components.DirectionalParry(module, [(uint)OID.Boss]);
class TartareanTomb(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TartareanTomb, 11f);

class RhitahtynSasArvinaStates : StateMachineBuilder
{
    public RhitahtynSasArvinaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<MagitekMissilesTartareanShockwave>()
            .ActivateOnEnter<DirectionalParry>()
            .ActivateOnEnter<TartareanTomb>()
            .ActivateOnEnter<Adds>()
            .ActivateOnEnter<DrillShotGalesOfTartarus>()
            .ActivateOnEnter<TartareanShockwaveBig>()
            .ActivateOnEnter<GalesOfTartarusBig>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 70057u, NameID = 2160u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class RhitahtynSasArvina(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-689, -815), new ArenaBoundsCircle(14.5f));
