// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Dungeon.D06Haukke.D061ManorClaviger;

public enum OID : uint
{
    Boss = 0x110 // x1
}

public enum AID : uint
{
    AutoAttack = 870, // Boss->player, no cast
    SweetSteel = 489, // Boss->self, no cast, range 7.4 ?-degree cone cleave (120-degree)
    VoidFire2 = 855, // Boss->location, 3.0s cast, range 5 aoe
    DarkMist = 705 // Boss->self, 4.0s cast, range 9.4 aoe
}

class SweetSteel(ModuleBase module) : Components.Cleave(module, (uint)AID.SweetSteel, new AOEShapeCone(7.4f, 60.Degrees()));
class VoidFire2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.VoidFire2, 5);
class DarkMist(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DarkMist, 9.4f);

class D061ManorClavigerStates : StateMachineBuilder
{
    public D061ManorClavigerStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<SweetSteel>()
            .ActivateOnEnter<VoidFire2>()
            .ActivateOnEnter<DarkMist>();
    }
}

[ModuleInfo(CFCID = 6u, NameID = 423u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class D061ManorClaviger(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly PolygonCustom[] shape = [new ([new(-21.5f, 3), new(-14, 9.2f), new(-14.1f, 14),
    new(8.2f, 14.7f), new(16.5f, 12.3f), new(16.5f, 7), new(22.1f, 7),
    new(21.5f, -9.4f), new(12.6f, -14.5f), new(-15.9f, -13.1f), new(-16.1f, -10), new(-20.3f, -10)])];
    public static readonly ArenaBoundsCustom arena = new(shape);
}
