// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.MaskedCarnivale.Stage21.Act2;

public enum OID : uint
{
    Boss = 0x2730, //R=3.7
    ArenaImp = 0x2731, //R=0.45
    Voidzone = 0x1E972A
}

public enum AID : uint
{
    AutoAttack = 6497, // Boss->player, no cast, single-target

    Blizzard = 14267, // Imp->player, 1.0s cast, single-target
    VoidBlizzard = 15063, // Imp->player, 6.0s cast, single-target
    Icefall = 15064, // Imp->location, 2.5s cast, range 5 circle
    TheRamsVoice = 15079, // Boss->self, 4.0s cast, range 9 circle
    TheDragonsVoice = 15080, // Boss->self, 4.0s cast, range 8-30 donut
    TheRamsKeeper = 15081 // Boss->location, 6.0s cast, range 9 circle
}

sealed class TheRamsKeeper(ModuleBase module) : Components.VoidzoneAtCastTarget(module, 9f, (uint)AID.TheRamsKeeper, GetVoidzones, 0.9d)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.Voidzone);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (z.EventState != 7)
            {
                voidzones[index++] = z;
            }
        }
        return voidzones[..index];
    }
}
sealed class TheRamsKeeperHint(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.TheRamsKeeper);
sealed class TheRamsVoice(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TheRamsVoice, 9f);
sealed class TheDragonsVoice(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TheDragonsVoice, new AOEShapeDonut(8f, 30f));
sealed class Icefall(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Icefall, 5f);
sealed class VoidBlizzard(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.VoidBlizzard);

sealed class Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add("Interrupt The Rams Keeper with Flying Sardine. You can start the\nFinal Sting combination at about 50% health left.\n(Off-guard->Bristle->Moonflute->Final Sting).\nThe boss will sometimes spawn an Arena Imp during the fight.");
    }
}

sealed class Hints2(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        var imps = Module.Enemies((uint)OID.ArenaImp);
        var count = imps.Count;
        if (count != 0)
        {
            for (var i = 0; i < count; ++i)
            {
                if (!imps[i].IsDead)
                {
                    hints.Add("The imps are weak to fire spells and strong against ice.\nInterrupt Void Blizzard with Flying Sardine.");
                    return;
                }
            }
        }
    }
}

sealed class Stage21Act2States : StateMachineBuilder
{
    public Stage21Act2States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<VoidBlizzard>()
            .ActivateOnEnter<Icefall>()
            .ActivateOnEnter<TheDragonsVoice>()
            .ActivateOnEnter<TheRamsKeeper>()
            .ActivateOnEnter<TheRamsKeeperHint>()
            .ActivateOnEnter<TheRamsVoice>()
            .ActivateOnEnter<Hints2>()
            .DeactivateOnEnter<Hints>();
    }
}

[ModuleInfo(Group = ModuleGroup.MaskedCarnivale, GroupID = 631u, CFCID = 631u, NameID = 8121u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Stage21Act2 : ModuleBase
{
    public Stage21Act2(WorldState ws, Actor primary) : base(ws, primary, Layouts.ArenaCenter, Layouts.CircleSmall)
    {
        ActivateComponent<Hints>();
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.ArenaImp), Colors.Object);
    }
}
