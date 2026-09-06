// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.MaskedCarnivale.Stage20.Act2;

public enum OID : uint
{
    Boss = 0x272B, //R=5.1
    Helper = 0x233C
}

public enum AID : uint
{
    AquaBreath = 14713, // Boss->self, 2.5s cast, range 8+R 90-degree cone
    Megavolt = 14714, // Boss->self, 3.0s cast, range 6+R circle
    ImpSong = 14712, // Boss->self, 6.0s cast, range 50+R circle
    Waterspout = 14718, // Helper->location, 2.5s cast, range 4 circle
    LightningBolt = 14717 // Helper->location, 3.0s cast, range 3 circle
}

sealed class AquaBreath(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AquaBreath, new AOEShapeCone(13.1f, 45f.Degrees()));
sealed class Megavolt(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Megavolt, 11.1f);
sealed class Waterspout(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Waterspout, 4f);
sealed class LightningBolt(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LightningBolt, 3f);
sealed class ImpSong(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.ImpSong);

sealed class Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add($"{Module.PrimaryActor.Name} is weak to fire. Interrupt Imp Song.");
    }
}

sealed class Stage20Act2States : StateMachineBuilder
{
    public Stage20Act2States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .DeactivateOnEnter<Hints>()
            .ActivateOnEnter<LightningBolt>()
            .ActivateOnEnter<Waterspout>()
            .ActivateOnEnter<Megavolt>()
            .ActivateOnEnter<AquaBreath>()
            .ActivateOnEnter<LightningBolt>()
            .ActivateOnEnter<ImpSong>();
    }
}

[ModuleInfo(Group = ModuleGroup.MaskedCarnivale, GroupID = 630u, CFCID = 630u, NameID = 7111u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Stage20Act2 : ModuleBase
{
    public Stage20Act2(WorldState ws, Actor primary) : base(ws, primary, Layouts.ArenaCenter, Layouts.CircleSmall)
    {
        ActivateComponent<Hints>();
    }
}
