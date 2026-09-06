// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.DeepDungeon.HeavenOnHigh.DD40Bhima;

public enum OID : uint
{
    Boss = 0x23E2, // R2.4
    Whirlwind = 0x23E3, // R1.0
}

public enum AID : uint
{
    AutoAttack = 6499, // Boss->player, no cast, single-target

    AncientAero = 11905, // Boss->self, 3.0s cast, range 50+R width 8 rect
    AncientAeroII = 11903, // Boss->location, 3.0s cast, range 6 circle 
    AncientAeroIII = 11904, // Boss->self, 5.0s cast, range 50+R circle, KB, not immunable (though shield on hp does block the kb)
    Tornado = 11902, // Boss->player, 3.0s cast, range 6 circle
    Windage = 11906, // Whirlwind->self, 1.0s cast, range 6 circle
}

class AncientAero(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AncientAero, new AOEShapeRect(52.4f, 4f));
class AncientAeroII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AncientAeroII, 6f);
class AncientAeroIII(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.AncientAeroIII, 30f, true, stopAtWall: true);
class Tornado(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.Tornado, 6f);
class Windage(ModuleBase module) : Components.Voidzone(module, 6f, GetVoidzones)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.Whirlwind);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (z.EventState != 7)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }
}

class DD40BhimaStates : StateMachineBuilder
{
    public DD40BhimaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<AncientAero>()
            .ActivateOnEnter<AncientAeroII>()
            .ActivateOnEnter<AncientAeroIII>()
            .ActivateOnEnter<Tornado>()
            .ActivateOnEnter<Windage>();
    }
}

[ModuleInfo(CFCID = 543u, NameID = 7483u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "LegendofIceman (ported from BMR)")]
public class DD40Bhima(WorldState ws, Actor primary) : HoHArena1(ws, primary);
