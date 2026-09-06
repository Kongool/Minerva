// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.DeepDungeon.PalaceOfTheDead.DD140AhPuch;

public enum OID : uint
{
    Boss = 0x181B, // R3.800, x1
    DeepPalaceFollower = 0x1906, // R1.800, x0 (spawn during fight)
    AccursedPoxVoidZone = 0x1E8EA9, // R0.500, x0 (spawn during fight), EventObj type
}

public enum AID : uint
{
    AutoAttack = 6498, // Boss->player, no cast, single-target
    AccursedPox = 7146, // Boss->location, 3.0s cast, range 8 circle
    AncientEruption = 7142, // Boss->location, 2.5s cast, range 4 circle
    Blizzard = 967, // DeepPalaceFollower->player, 1.0s cast, single-target
    EntropicFlame = 7143, // Boss->self, 3.0s cast, range 50+R width 8 rect
    Scream = 7145, // Boss->self, 3.0s cast, range 30 circle
    ShadowFlare = 7144, // Boss->self, 3.0s cast, range 25+R circle
}

class Adds(ModuleBase module) : Components.Adds(module, (uint)OID.DeepPalaceFollower, 1);

class AccursedPox(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AccursedPox, 8f);
class AncientEruption(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AncientEruption, 4f);
class AncientEruptionZone(ModuleBase module) : Components.PersistentInvertibleVoidzone(module, 4f, GetVoidzones)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.AccursedPoxVoidZone);
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
class EntropicFlame(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.EntropicFlame, new AOEShapeRect(53.8f, 4f));
class Scream(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Scream, "Raidwide + Fear, Adds need to be dead by now");
class ShadowFlare(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ShadowFlare);

class DD140AhPuchStates : StateMachineBuilder
{
    public DD140AhPuchStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Adds>()
            .ActivateOnEnter<AccursedPox>()
            .ActivateOnEnter<AncientEruption>()
            .ActivateOnEnter<AncientEruptionZone>()
            .ActivateOnEnter<EntropicFlame>()
            .ActivateOnEnter<Scream>()
            .ActivateOnEnter<ShadowFlare>();
    }
}

[ModuleInfo(CFCID = 212u, NameID = 5410u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "LegendofIceman (ported from BMR)")]
public class DD140AhPuch(WorldState ws, Actor primary) : ModuleBase(ws, primary, SharedBounds.ArenaBounds140150.Center, SharedBounds.ArenaBounds140150);
