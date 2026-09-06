// Ported from Veyn's bossmod (awgil/ffxiv_bossmod, BSD-3; see THIRD-PARTY-NOTICES.txt). BossmodReborn has no
// module for this Bozjan Southern Front skirmish.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.Skirmish.OfSteelAndFlame;

public enum OID : uint
{
    Boss = 0x2FAE, // Cacus
    Helper = 0x233C,
    SartauvoirTheInferno = 0x2FAD,
    SartHelper = 0x2EA1
}

public enum AID : uint
{
    Exhaust = 21556, // Boss->self, 4.0s cast, range 40 width 10 rect
    MagitekRay = 21558, // Boss->location, 3.5s cast, range 6 circle
    ThermalShock = 21022, // SartHelper->self, 7.0s cast, range 30 circle
    GrandSword = 21553, // Boss->self, 9.0s cast, range 27 120-degree cone
    GrandSwordInstant = 21555, // Boss->self, 0.5s cast, range 27 ?-degree cone
    TridirectionalFlame = 20927, // SartHelper->self, 3.0s cast, range 60 width 8 rect
    PyreticEruption = 20929, // SartHelper->location, 3.0s cast, range 8 circle
    Pyroscatter = 20930, // SartHelper->location, 3.0s cast, range 8 circle
    Pyroburst = 20931, // SartHelper->self, 4.0s cast, range 10 circle
    GrandCrossflame1 = 20928, // SartHelper->self, 4.5s cast, range 40 width 18 cross
    AtomicRay = 21559, // Boss->self, 8.0s cast, range 40 circle
}

class TridirectionalFlame(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TridirectionalFlame, new AOEShapeRect(60f, 4f));
class Exhaust(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Exhaust, new AOEShapeRect(40f, 5f));
class MagitekRay(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MagitekRay, new AOEShapeCircle(6f));
// the drawn circle is the lethal core; the whole 30y radius still takes damage, so that part is recorded as predicted damage
class ThermalShock(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThermalShock, new AOEShapeCircle(10f))
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        if (this.Casters.Count > 0)
            hints.AddPredictedDamage(this.Raid.WithSlot().InRadius(this.Casters[0].Origin, 30f).Mask(), this.Casters[0].Activation);
    }
}
class PyreticEruption(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PyreticEruption, new AOEShapeCircle(8f));
class Pyroscatter(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Pyroscatter, new AOEShapeCircle(8f));
class Pyroburst(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Pyroburst, new AOEShapeCircle(10f));
class GrandCrossflame(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GrandCrossflame1, new AOEShapeCross(40f, 9f));
class AtomicRay(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AtomicRay);

// a 120-degree cone that steps 120 degrees clockwise every 2.8s: three swings the first time, six after
class GrandSword(ModuleBase module) : Components.GenericRotatingAOE(module)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID == AID.GrandSword)
            this.Sequences.Add(new(new AOEShapeCone(27f, 60.Degrees()), caster.Position, caster.Rotation, -120.Degrees(), this.Module.CastFinishAt(spell), 2.8d, this.NumCasts > 0 ? 6 : 3));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if ((AID)spell.Action.ID is AID.GrandSword or AID.GrandSwordInstant)
        {
            this.NumCasts++;
            this.AdvanceSequence(caster.Position, spell.Rotation, this.World.CurrentTime);
        }
    }
}

class OfSteelAndFlameStates : StateMachineBuilder
{
    public OfSteelAndFlameStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<Exhaust>()
            .ActivateOnEnter<MagitekRay>()
            .ActivateOnEnter<ThermalShock>()
            .ActivateOnEnter<GrandSword>()
            .ActivateOnEnter<TridirectionalFlame>()
            .ActivateOnEnter<PyreticEruption>()
            .ActivateOnEnter<Pyroscatter>()
            .ActivateOnEnter<Pyroburst>()
            .ActivateOnEnter<GrandCrossflame>()
            .ActivateOnEnter<AtomicRay>()
            .Raw.Update = () => module.PrimaryActor.IsDeadOrDestroyed && module.Enemies((uint)OID.SartauvoirTheInferno).All(e => e.HPRatio < 1f && !e.IsTargetable);
    }
}

[ModuleInfo(Group = ModuleGroup.ForayFATE, GroupID = 1626u, CFCID = 735u, NameID = 9634u, PrimaryActorOID = (uint)OID.Boss, Maturity = ModuleMaturity.WIP, Contributors = "ported from Veyn's bossmod (awgil)")]
public class OfSteelAndFlame(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-234.1f, 260.1f), new ArenaBoundsCircle(50f))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        this.Arena.Actor(this.PrimaryActor, Colors.Enemy);
        this.Arena.Actors(this.Enemies((uint)OID.SartauvoirTheInferno), Colors.Enemy);
    }
}
