// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.DeepDungeon.EurekaOrthos.DD30TiamatsClone;

public enum OID : uint
{
    Boss = 0x3D9A, // R19.0
    DarkWanderer = 0x3D9B, // R2.0
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 32702, // Boss->player, no cast, single-target
    HeadAttack = 31842, // Helper->player, no cast, single-target

    CreatureOfDarkness = 31841, // Boss->self, 3.0s cast, single-target, summon Heads E<->W heading S
    DarkMegaflareVisual = 31849, // Boss->self, 3.0s cast, single-target
    DarkMegaflare = 31850, // Helper->location, 3.0s cast, range 6 circle
    DarkWyrmtailVisual = 31843, // Boss->self, 5.0s cast, single-target
    DarkWyrmtail = 31844, // Helper->self, 6.0s cast, range 40 width 16 rect, summon Heads Heading E/W from Middle Lane
    DarkWyrmwingVisual = 31845, // Boss->self, 5.0s cast, single-target
    DarkWyrmwing = 31846, // Helper->self, 6.0s cast, range 40 width 16 rect, summon Heads Heading E/W from E/W Walls
    WheiMornFirst = 31847, // Boss->location, 5.0s cast, range 6 circle
    WheiMornRest = 31848 // Boss->location, no cast, range 6 circle
}

public enum IconID : uint
{
    WheiMorn = 197 // player
}

class WheiMorn(ModuleBase module) : Components.StandardChasingAOEs(module, 6f, (uint)AID.WheiMornFirst, (uint)AID.WheiMornRest, 6f, 2f, 5, true, (uint)IconID.WheiMorn);
class DarkMegaflare(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DarkMegaflare, 6f);

class DarkWyrm(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeRect(40f, 8f));
class DarkWyrmwing(ModuleBase module) : DarkWyrm(module, (uint)AID.DarkWyrmwing);
class DarkWyrmtail(ModuleBase module) : DarkWyrm(module, (uint)AID.DarkWyrmtail);
class CreatureOfDarkness(ModuleBase module) : Components.Voidzone(module, 2f, GetVoidzones, 6f)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.DarkWanderer);
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

class DD30TiamatsCloneStates : StateMachineBuilder
{
    public DD30TiamatsCloneStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<DarkWyrmwing>()
            .ActivateOnEnter<DarkWyrmtail>()
            .ActivateOnEnter<DarkMegaflare>()
            .ActivateOnEnter<WheiMorn>()
            .ActivateOnEnter<CreatureOfDarkness>();
    }
}

[ModuleInfo(CFCID = 899u, NameID = 12242u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class DD30TiamatsClone(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-300f, -300f), new ArenaBoundsSquare(19.5f));
