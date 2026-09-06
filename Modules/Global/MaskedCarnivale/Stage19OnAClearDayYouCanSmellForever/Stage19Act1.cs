// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.MaskedCarnivale.Stage19.Act1;

public enum OID : uint
{
    Boss = 0x2727, //R=5.775
    Voidzone = 0x1EA9F9 //R=0.5
}

public enum AID : uint
{
    AutoAttack = 6499, // Boss->player, no cast, single-target

    Reflect = 15073, // Boss->self, 3.0s cast, single-target, boss starts reflecting all melee attacks
    BadBreath = 15074, // Boss->self, 3.5s cast, range 12+R 120-degree cone
    VineProbe = 15075, // Boss->self, 2.5s cast, range 6+R width 8 rect
    OffalBreath = 15076 // Boss->location, 3.5s cast, range 6 circle, interruptible, voidzone
}

sealed class BadBreath(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BadBreath, new AOEShapeCone(17.775f, 60f.Degrees()));
sealed class VineProbe(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.VineProbe, new AOEShapeRect(11.775f, 4f));
sealed class OffalBreath(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.OffalBreath);
sealed class OffalBreathVoidzone(ModuleBase module) : Components.VoidzoneAtCastTarget(module, 6f, (uint)AID.OffalBreath, GetVoidzones, 1.6f)
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

sealed class Reflect(ModuleBase module) : ModuleComponent(module)
{
    private bool reflect;
    private bool casting;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Reflect)
        {
            casting = true;
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Reflect)
        {
            reflect = true;
            casting = false;
        }
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (casting)
        {
            hints.Add("Boss will reflect all magic damage!");
        }
        else if (reflect)
        {
            hints.Add("Boss reflects all magic damage!"); // TODO: could use an AI hint to never use magic abilities after this is casted
        }
    }
}

sealed class Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add("At the start of the fight Rebekkah will cast Reflect. This will reflect all\nmagic damage back to you. Useful skills: Sharpened Knife,\nFlying Sardine, Ink Jet (Act 2), Exuviation (Act 2), potentially a Final Sting\ncombo. (Off-guard->Bristle->Moonflute->Final Sting)");
    }
}

sealed class Stage19Act1States : StateMachineBuilder
{
    public Stage19Act1States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .DeactivateOnEnter<Hints>()
            .ActivateOnEnter<Reflect>()
            .ActivateOnEnter<BadBreath>()
            .ActivateOnEnter<VineProbe>()
            .ActivateOnEnter<OffalBreath>()
            .ActivateOnEnter<OffalBreathVoidzone>();
    }
}

[ModuleInfo(Group = ModuleGroup.MaskedCarnivale, GroupID = 629u, CFCID = 629u, NameID = 8117u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Stage19Act1 : ModuleBase
{
    public Stage19Act1(WorldState ws, Actor primary) : base(ws, primary, Layouts.ArenaCenter, Layouts.CircleSmall)
    {
        ActivateComponent<Hints>();
    }
}
