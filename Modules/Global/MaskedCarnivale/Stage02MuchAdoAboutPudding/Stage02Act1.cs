// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.MaskedCarnivale.Stage02.Act1;

public enum OID : uint
{
    Boss = 0x25C0, //R=1.8
    Marshmallow = 0x25C2, //R1.8
    Bavarois = 0x25C4 //R1.8
}

public enum AID : uint
{
    Fire = 14266, // Boss->player, 1.0s cast, single-target
    Aero = 14269, // Marshmallow->player, 1.0s cast, single-target
    Thunder = 14268, // Bavarois->player, 1.0s cast, single-target
    GoldenTongue = 14265 // Boss/Marshmallow/Bavarois->self, 5.0s cast, single-target
}

sealed class GoldenTongue(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.GoldenTongue);

sealed class Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add("To beat this stage in a timely manner,\nyou should have at least one spell of each element.\n(Water, Fire, Ice, Lightning, Earth and Wind)");
    }
}

sealed class Hints2(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add("Pudding is weak to wind spells.\nMarshmallow is weak to ice spells.\nBavarois is weak to earth spells.");
    }
}

sealed class Stage02Act1States : StateMachineBuilder
{
    public Stage02Act1States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<GoldenTongue>()
            .ActivateOnEnter<Hints2>()
            .DeactivateOnEnter<Hints>()
            .Raw.Update = () => AllDeadOrDestroyed(Stage02Act1.Trash);
    }
}

[ModuleInfo(Group = ModuleGroup.MaskedCarnivale, GroupID = 612u, CFCID = 612u, NameID = 8078u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Stage02Act1 : ModuleBase
{
    public Stage02Act1(WorldState ws, Actor primary) : base(ws, primary, Layouts.ArenaCenter, Layouts.CircleBig)
    {
        ActivateComponent<Hints>();
    }
    public static readonly uint[] Trash = [(uint)OID.Boss, (uint)OID.Marshmallow, (uint)OID.Bavarois];

    protected override bool CheckPull() => IsAnyActorInCombat(Trash);

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.Marshmallow));
        Arena.Actors(Enemies((uint)OID.Bavarois));
    }
}
