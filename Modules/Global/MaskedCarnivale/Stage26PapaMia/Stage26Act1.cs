// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.MaskedCarnivale.Stage26.Act1;

public enum OID : uint
{
    Boss = 0x2C84, //R=2.55
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 6498, // Boss->player, no cast, single-target

    AlternatePlumage = 18686, // Boss->self, 3.0s cast, single-target, armor up, needs dispel
    RuffledFeathers = 18685, // Boss->player, no cast, single-target
    Gust = 18687, // Boss->location, 2.5s cast, range 3 circle
    CaberToss = 18688 // Boss->player, 5.0s cast, single-target, interrupt or wipe
}

public enum SID : uint
{
    VulnerabilityDown = 63, // Boss->Boss, extra=0x0
    Windburn = 269 // Boss->player, extra=0x0
}

sealed class Gust(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Gust, 3f);
sealed class AlternatePlumage(ModuleBase module) : Components.CastHint(module, (uint)AID.AlternatePlumage, "Prepare to dispel buff");
sealed class CaberToss(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.CaberToss);

sealed class Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add($"{Module.PrimaryActor.Name} will cast Alternate Plumage, which makes him almost\nimmune to damage. Use Eerie Soundwave to dispel it. Caber Toss must be\ninterrupted or you will wipe.\nAdditionally Exuviation and earth spells are recommended for act 2.");
    }
}

sealed class Hints2(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        if (Module.PrimaryActor.FindStatus((uint)SID.VulnerabilityDown) != null)
        {
            hints.Add($"Dispel {Module.PrimaryActor.Name} with Eerie Soundwave!");
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (actor.FindStatus((uint)SID.Windburn) != null)
        {
            hints.Add("Windburn on you! Cleanse it with Exuviation.");
        }
    }
}

sealed class Stage26Act1States : StateMachineBuilder
{
    public Stage26Act1States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<CaberToss>()
            .ActivateOnEnter<Gust>()
            .ActivateOnEnter<AlternatePlumage>()
            .ActivateOnEnter<Hints2>()
            .DeactivateOnEnter<Hints>();
    }
}

[ModuleInfo(Group = ModuleGroup.MaskedCarnivale, GroupID = 695u, CFCID = 695u, NameID = 9230u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Stage26Act1 : ModuleBase
{
    public Stage26Act1(WorldState ws, Actor primary) : base(ws, primary, Layouts.ArenaCenter, Layouts.CircleBig)
    {
        ActivateComponent<Hints>();
    }
}
