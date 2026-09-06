// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Quest.FourLords.TortoiseInTime;

public enum OID : uint
{
    Boss = 0x2339,
    Soroban = 0x2351, // R0.5
    MonkeyMagick = 0x23C2, // R1.0
    Font = 0x233B, // R4.0
    Helper = 0x233C
}

public enum AID : uint
{
    Eddy1 = 11511, // 2351->location, 3.0s cast, range 6 circle
    GreatFlood1 = 11513, // 2351->self, no cast, range 60 circle
    SpiritBurst = 11706, // 23C2->self, 1.0s cast, range 6 circle
    WaterDrop = 11301, // 2351->234F, 8.0s cast, range 6 circle
    Whitewater1 = 11521, // 2351->self, 3.0s cast, range 40+R width 7 rect
    Upwell = 11515, // 233B->self, 3.0s cast, range 37+R ?-degree cone
}

class Whitewater(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Whitewater1, new AOEShapeRect(40.5f, 3.5f));
class Upwell(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Upwell, new AOEShapeCone(41, 15.Degrees()));
class SpiritBurst(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SpiritBurst, 6);
class WaterDrop(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.WaterDrop, 6);

class ExplosiveTataru(ModuleBase module) : ModuleComponent(module)
{
    private readonly List<Actor> Balls = [];
    private Actor? Tataru;

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID == 3)
        {
            Balls.Add(source);
            Tataru ??= World.Actors.Find(tether.Target);
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.SpiritBurst)
        {
            Balls.Remove(caster);
            if (Balls.Count == 0)
                Tataru = null;
        }
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        if (Tataru != null)
            Arena.ZoneCircleOutline(Tataru.Position, 6, Colors.Danger);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Tataru != null)
            hints.AddForbiddenZone(new SDCircle(Tataru.Position, 6));
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (Tataru != null && actor.Position.InCircle(Tataru.Position, 6))
            hints.Add("GTFO from Tataru!");
    }
}

class Eddy(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Eddy1, 6);

class ShieldHint(ModuleBase module) : ModuleComponent(module)
{
    private const float Radius = 7;
    private Actor? Shield;

    public override void OnActorEState(Actor actor, ushort state)
    {
        if (actor.OID == 0x1EA9C7 && state == 2)
            Shield = actor;
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        if (Shield is Actor s)
            Arena.ZoneCircle(s.Position, Radius, Colors.SafeFromAOE);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.GreatFlood1)
            Shield = null;
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Shield is Actor s)
            hints.AddForbiddenZone(new SDInvertedCircle(s.Position, Radius), Module.CastFinishAt(Module.PrimaryActor.CastInfo));
    }
}

class SorobanStates : StateMachineBuilder
{
    public SorobanStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Eddy>()
            .ActivateOnEnter<ShieldHint>()
            .ActivateOnEnter<WaterDrop>()
            .ActivateOnEnter<ExplosiveTataru>()
            .ActivateOnEnter<SpiritBurst>()
            .ActivateOnEnter<Whitewater>()
            .ActivateOnEnter<Upwell>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 68552u, NameID = 7240u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Soroban(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(62, -372), new ArenaBoundsSquare(19));

