// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Dungeon.D03Copperbell.D032IchorousIre;

public enum OID : uint
{
    Boss = 0x3870,
    IchorousDrip = 0x3871 // x6
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast
    Syrup = 28462, // Boss->location, 4.0s cast, range 4 aoe
    FluidSpread = 28461, // Boss->player, 5.0s cast, tankbuster
    Divide = 28463, // Boss->self, 3.0s cast, visual
    DivideAppear = 28464, // IchorousDrip->location, no cast, teleport/appear
    Burst = 28465 // IchorousDrip->self, 6.0s cast, range 8 aoe
}

class Syrup(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Syrup, 4);
class FluidSpread(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.FluidSpread);
class Divide(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Burst, 8);

class D032IchorousIreStates : StateMachineBuilder
{
    public D032IchorousIreStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Syrup>()
            .ActivateOnEnter<FluidSpread>()
            .ActivateOnEnter<Divide>();
    }
}

[ModuleInfo(CFCID = 3u, NameID = 554u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class D032IchorousIre(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    public static readonly ArenaBoundsCustom arena = new([new Circle(new(27, 114), 19.5f)], [new Rectangle(new(37.5f, 95), 20, 2.4f, -25.Degrees()), new Rectangle(new(6, 112), 20, 1.75f, -270.Degrees())]);
}
