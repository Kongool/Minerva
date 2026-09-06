// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.MaskedCarnivale.Stage26.Act2;

public enum OID : uint
{
    Boss = 0x2C58, //R=3.6
    Thunderhead = 0x2C59, //R=1.0
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 6499, // Boss->player, no cast, single-target

    RawInstinct = 18604, // Boss->self, 3.0s cast, single-target
    BodyBlow = 18601, // Boss->player, 4.0s cast, single-target
    VoidThunderII = 18602, // Boss->location, 3.0s cast, range 4 circle
    LightningBolt = 18606, // Thunderhead->self, no cast, range 8 circle
    DadJoke = 18605, // Boss->self, no cast, range 25+R 120-degree cone, knockback 15, dir forward
    VoidThunderIII = 18603 // Boss->player, 4.0s cast, range 20 circle
}

public enum SID : uint
{
    CriticalStrikes = 1797, // Boss->Boss, extra=0x0
    Electrocution = 271 // Boss/2C59->player, extra=0x0
}

public enum IconID : uint
{
    BaitKnockback = 23 // player
}

sealed class Thunderhead(ModuleBase module) : Components.Voidzone(module, 8f, GetVoidzones)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.Thunderhead);
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

sealed class DadJoke(ModuleBase module) : Components.GenericKnockback(module)
{
    private DateTime _activation;
    private readonly Thunderhead _aoe = module.FindComponent<Thunderhead>()!;

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor)
    {
        if (_activation != default)
            return new Knockback[1] { new(Module.PrimaryActor.Position, 15f, _activation, direction: Angle.FromDirection(actor.Position - Module.PrimaryActor.Position), kind: Kind.DirForward) };
        return [];
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.BaitKnockback)
        {
            _activation = World.FutureTime(5d);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.DadJoke)
        {
            _activation = default;
        }
    }

    public override bool DestinationUnsafe(int slot, Actor actor, WPos pos)
    {
        var aoes = _aoe.ActiveAOEs(slot, actor);
        var len = aoes.Length;
        for (var i = 0; i < len; ++i)
        {
            if (aoes[i].Check(pos))
            {
                return true;
            }
        }
        return !InBounds(pos);
    }
}

sealed class VoidThunderII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.VoidThunderII, 4);
sealed class RawInstinct(ModuleBase module) : Components.CastHint(module, (uint)AID.RawInstinct, "Prepare to dispel buff");
sealed class VoidThunderIII(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.VoidThunderIII, "Raidwide + Electrocution");
sealed class BodyBlow(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.BodyBlow, "Soft Tankbuster");

sealed class Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add($"{Module.PrimaryActor.Name} will cast Raw Instinct, which causes all his hits to crit.\nUse Eerie Soundwave to dispel it.\n{Module.PrimaryActor.Name} is weak against earth and strong against lightning attacks.");
    }
}

sealed class Hints2(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        if (Module.PrimaryActor.FindStatus((uint)SID.CriticalStrikes) != null)
        {
            hints.Add($"Dispel {Module.PrimaryActor.Name} with Eerie Soundwave!");
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (actor.FindStatus((uint)SID.Electrocution) != null)
        {
            hints.Add("Electrocution on you! Cleanse it with Exuviation.");
        }
    }
}

sealed class Stage26Act2States : StateMachineBuilder
{
    public Stage26Act2States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<RawInstinct>()
            .ActivateOnEnter<VoidThunderII>()
            .ActivateOnEnter<VoidThunderIII>()
            .ActivateOnEnter<BodyBlow>()
            .ActivateOnEnter<Thunderhead>()
            .ActivateOnEnter<DadJoke>()
            .ActivateOnEnter<Hints2>()
            .DeactivateOnEnter<Hints>();
    }
}

[ModuleInfo(Group = ModuleGroup.MaskedCarnivale, GroupID = 695u, CFCID = 695u, NameID = 9231u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Stage26Act2 : ModuleBase
{
    public Stage26Act2(WorldState ws, Actor primary) : base(ws, primary, Layouts.ArenaCenter, Layouts.CircleSmall)
    {
        ActivateComponent<Hints>();
    }
}
