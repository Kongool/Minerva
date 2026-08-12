using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.CE107Unbridled;

// Occult Crescent Critical Engagement "Unbridled" — boss Crescent Berserker. IDs/mechanics referenced
// from BossmodReborn's CE107Unbridled. Occult Crescent CEs all report CFC 1018, so this coexists with
// CE103 (Vassal Vessel) under the same CFC — the registry activates whichever boss OID is present.
// Most damage comes from Helper casts (the boss self-casts are just the visual), and HoppingMad is an
// in/out circle+donut sequence, so a plain generated SimpleAOEs draft mislabels a lot of it.

public enum OID : uint
{
    Boss = 0x4703,          // 'Crescent Berserker' R4.5
    Deathwall = 0x1EBD5C,   // arena-edge marker
    DeathwallHelper = 0x4871,
    Helper = 0x233C,
}

public enum AID : uint
{
    // raidwides — the boss self-cast is the visual, the Helper cast is the actual damage; watch both
    BoilOverVisual = 30788, BoilOver = 30789,
    ChanneledRageVisual = 30790, ChanneledRage = 30791,
    HeightenedRageVisual = 37809, HeightenedRage = 37815,

    WhiteHotRage = 37800,   // Boss->location, 4s, range 6 circle
    ScathingSweep = 42691,  // Boss->self, 6s, range 60 width 60 rect
    HeatedOutburst = 37804, // Helper->self, 6s, range 13 circle

    // HoppingMad: paired circle (get out) + donut (get in), growing across the sequence
    HoppingMad1 = 37323, BedrockUplift1 = 37807, // circle 8  / donut 8-60
    HoppingMad2 = 30872, BedrockUplift2 = 37805, // circle 24 / donut 24-60
    HoppingMad3 = 30873, BedrockUplift3 = 37806, // circle 16 / donut 16-60
    HoppingMad4 = 37041,                         // circle 8
}

sealed class Rages(ModuleBase module) : Components.RaidwideCasts(module,
    [(uint)AID.BoilOverVisual, (uint)AID.BoilOver, (uint)AID.ChanneledRageVisual, (uint)AID.ChanneledRage, (uint)AID.HeightenedRageVisual, (uint)AID.HeightenedRage],
    "Raidwide (mitigate)");

// the arena shrinks from its full extent to a 25y circle when the Deathwall spawns
sealed class Shrink(ModuleBase module) : Components.ArenaChange(module, new ArenaBoundsCircle(25f), triggerOID: (uint)OID.Deathwall);

/// <summary>Boil Over also erupts the arena rim: a 25-30y donut at centre, telegraphed by its cast.</summary>
sealed class BoilOverRim(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeDonut donut = new(25f, 30f);
    private readonly System.Collections.Generic.List<AOEInstance> aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(this.aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        // only meaningful while the arena is still big; once shrunk to 25y the ring is off-field anyway
        if (cast.Action.ID == (uint)AID.BoilOverVisual && this.Module.Bounds.Radius > 25f)
            this.aoes.Add(new AOEInstance(donut, this.Module.Center, default, this.Module.CastFinishAt(cast, 1f), actorID: caster.InstanceID));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == (uint)AID.BoilOverVisual)
            this.aoes.RemoveAll(a => a.ActorID == caster.InstanceID);
    }
}

sealed class WhiteHotRage(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WhiteHotRage, new AOEShapeCircle(6f));
sealed class ScathingSweep(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ScathingSweep, new AOEShapeRect(60f, 30f));
sealed class HeatedOutburst(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HeatedOutburst, new AOEShapeCircle(13f));

/// <summary>
/// HoppingMad: a sequence of expanding circles and matching donuts (stand out of the circle / in the
/// donut hole). Each Helper cast telegraphs one shape at its location; cleared when that cast resolves.
/// </summary>
sealed class HoppingMad(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(this.aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        AOEShape? shape = cast.Action.ID switch
        {
            (uint)AID.HoppingMad1 or (uint)AID.HoppingMad4 => new AOEShapeCircle(8f),
            (uint)AID.HoppingMad2 => new AOEShapeCircle(24f),
            (uint)AID.HoppingMad3 => new AOEShapeCircle(16f),
            (uint)AID.BedrockUplift1 => new AOEShapeDonut(8f, 60f),
            (uint)AID.BedrockUplift2 => new AOEShapeDonut(24f, 60f),
            (uint)AID.BedrockUplift3 => new AOEShapeDonut(16f, 60f),
            _ => null,
        };
        if (shape != null)
        {
            var origin = cast.LocXZ != default ? cast.LocXZ : caster.Position;
            this.aoes.Add(new AOEInstance(shape, origin, default, this.Module.CastFinishAt(cast), actorID: caster.InstanceID));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (this.aoes.Count > 0)
            this.aoes.RemoveAll(a => a.ActorID == caster.InstanceID);
    }
}

sealed class CE107UnbridledStates : StateMachineBuilder
{
    public CE107UnbridledStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<Rages>()
            .ActivateOnEnter<Shrink>()
            .ActivateOnEnter<BoilOverRim>()
            .ActivateOnEnter<WhiteHotRage>()
            .ActivateOnEnter<ScathingSweep>()
            .ActivateOnEnter<HeatedOutburst>()
            .ActivateOnEnter<HoppingMad>();
    }
}

[ModuleInfo(CFCID = 1018u, NameID = 35u, Maturity = ModuleMaturity.WIP, Contributors = "Minerva (ids from BMR CE107)")]
public sealed class CE107Unbridled(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(620f, 800f), new ArenaBoundsCircle(29.5f)); // shrinks to 25y via Shrink
