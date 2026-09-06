// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Extreme.Ex4Ifrit;

class Incinerate(ModuleBase module) : Components.Cleave(module, (uint)AID.Incinerate, CleaveShape)
{
    public static readonly AOEShapeCone CleaveShape = new(21f, 60f.Degrees());
}

class RadiantPlume(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RadiantPlumeAOE, 8f);

// TODO: consider showing next charge before its cast starts...
class CrimsonCyclone(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CrimsonCyclone, new AOEShapeRect(49f, 9f));

[ModuleInfo(CFCID = 63u, NameID = 1185u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Ex4Ifrit : ModuleBase
{
    public readonly List<Actor> SmallNails;
    public readonly List<Actor> LargeNails;

    public Ex4Ifrit(WorldState ws, Actor primary) : base(ws, primary, default, Trial.T01IfritN.T01IfritN.IfritArena)
    {
        SmallNails = Enemies((uint)OID.InfernalNailSmall);
        LargeNails = Enemies((uint)OID.InfernalNailLarge);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(SmallNails, Colors.Object);
        Arena.Actors(LargeNails, Colors.Object);
    }
}
