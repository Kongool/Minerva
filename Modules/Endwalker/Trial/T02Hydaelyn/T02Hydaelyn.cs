// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Trial.T02Hydaelyn;

class MousasScorn(ModuleBase module) : Components.CastSharedTankbuster(module, (uint)AID.MousasScorn, 4f);

class HerossSundering(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.HerossSundering, new AOEShapeCone(40f, 45f.Degrees()), tankbuster: true);

class HerossRadiance(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.HerossRadiance);
class MagossRadiance(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.MagossRadiance);
class RadiantHalo(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.RadiantHalo);
class CrystallineStoneIII(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.CrystallineStoneIII2, 6f, 8, 8);
class CrystallineBlizzardIII(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.CrystallineBlizzardIII2, 5f);
class Beacon1(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.Beacon1, 3f);
class Beacon2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Beacon2, new AOEShapeRect(45f, 3f), 10);
class HydaelynsRay(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HydaelynsRay, new AOEShapeRect(45f, 15f));

class T02HydaelynStates : StateMachineBuilder
{
    public T02HydaelynStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ParhelicCircle>()
            .ActivateOnEnter<MousasScorn>()
            .ActivateOnEnter<Echoes>()
            .ActivateOnEnter<Beacon1>()
            .ActivateOnEnter<Beacon2>()
            .ActivateOnEnter<CrystallineStoneIII>()
            .ActivateOnEnter<CrystallineBlizzardIII>()
            .ActivateOnEnter<HerossSundering>()
            .ActivateOnEnter<HerossRadiance>()
            .ActivateOnEnter<MagossRadiance>()
            .ActivateOnEnter<HydaelynsRay>()
            .ActivateOnEnter<RadiantHalo>()
            .ActivateOnEnter<Lightwave>()
            .ActivateOnEnter<WeaponTracker>()
            .ActivateOnEnter<Exodus>();
    }
}

[ModuleInfo(CFCID = 790u, NameID = 10453u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class T02Hydaelyn(WorldState ws, Actor primary) : ModuleBase(ws, primary, ArenaCenter, ArenaBounds)
{
    public static readonly WPos ArenaCenter = new(100, 100);
    public static readonly ArenaBoundsCustom ArenaBounds = new([new Polygon(ArenaCenter, 20, 48)]);

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.CrystalOfLight), Colors.Object);
    }
}
