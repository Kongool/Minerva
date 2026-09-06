// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.DeepDungeon.PalaceOfTheDead.D40Ixtab;

public enum OID : uint
{
    Boss = 0x16B9, // R3.800, x1
    AccursedPoxVoidZone = 0x1E8EA9, // R0.500, x0 (spawn during fight), EventObj type
    NightmareBhoot = 0x1764 // R1.800, x0 (spawn during fight)
}

public enum AID : uint
{
    AutoAttack = 6498, // Boss->player, no cast, single-target

    AccursedPox = 6434, // Boss->location, 3.0s cast, range 8 circle
    AncientEruption = 6430, // Boss->location, 2.5s cast, range 4 circle
    Blizzard = 967, // NightmareBhoot->player, 1.0s cast, single-target
    EntropicFlame = 6431, // Boss->self, 3.0s cast, range 50+R width 8 rect
    Scream = 6433, // Boss->self, 3.0s cast, range 25 circle
    ShadowFlare = 6432 // Boss->self, 3.0s cast, range 25+R circle
}

class Adds(ModuleBase module) : Components.Adds(module, (uint)OID.NightmareBhoot, 1);

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
            if (z.ModelState.AnimState1 == 1)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }
}
class EntropicFlame(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.EntropicFlame, new AOEShapeRect(53.8f, 4f));
class Scream(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Scream, "Raidwide + Fear, Adds need to be dead by now");
class ShadowFlare(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ShadowFlare);

class DD40IxtabStates : StateMachineBuilder
{
    public DD40IxtabStates(ModuleBase module) : base(module)
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

[ModuleInfo(CFCID = 177u, NameID = 5025u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "LegendofIceman (ported from BMR)")]
public class DD40Ixtab(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(-300f, -226f), 24.18f, 32)], [new Rectangle(new(-300f, -250.954f), 20f, 1.25f)]);
}
