// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Dungeon.D03SkydeepCenote.D032Firearms;

public enum OID : uint
{
    Boss = 0x4184, // R4.62
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target
    Teleport = 36451, // Boss->location, no cast, single-target

    DynamicDominance = 36448, // Boss->self, 5.0s cast, range 70 circle
    MirrorManeuver = 39139, // Boss->self, 3.0s cast, single-target

    ThunderlightBurstVisual = 36443, // Boss->self, 8.0s cast, single-target
    ThunderlightBurstAOE = 36445, // Helper->self, 10.9s cast, range 35 circle

    ThunderlightBurst1 = 38581, // Helper->self, 8.2s cast, range 42 width 8 rect
    ThunderlightBurst2 = 38582, // Helper->self, 8.2s cast, range 49 width 8 rect
    ThunderlightBurst3 = 38583, // Helper->self, 8.2s cast, range 35 width 8 rect
    ThunderlightBurst4 = 38584, // Helper->self, 8.2s cast, range 36 width 8 rect

    AncientArtillery = 36442, // Boss->self, 3.0s cast, single-target
    EmergentArtillery = 39000, // Boss->self, 3.0s cast, single-target

    Artillery1 = 38660, // Helper->self, 8.5s cast, range 10 width 10 rect
    Artillery2 = 38661, // Helper->self, 8.5s cast, range 10 width 10 rect
    Artillery3 = 38662, // Helper->self, 8.5s cast, range 10 width 10 rect
    Artillery4 = 38663, // Helper->self, 8.5s cast, range 10 width 10 rect

    Pummel = 36447, // Boss->player, 5.0s cast, single-target

    ThunderlightFlurry = 36450 // Helper->player, 5.0s cast, range 6 circle
}

sealed class DynamicDominanceArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCustom square = new([new Square(D032Firearms.ArenaCenter, 25f)], [new Square(D032Firearms.ArenaCenter, 20f)]);
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.DynamicDominance && Module.Bounds.Radius > 20f)
        {
            _aoe = [new(square, Module.Center, default, Module.CastFinishAt(spell, 0.6d))];
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (state == 0x00020001 && index == 0x14)
        {
            Module.Bounds = D032Firearms.DefaultBounds;
            _aoe = [];
        }
    }
}

sealed class DynamicDominance(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.DynamicDominance);

sealed class ThunderlightBurstAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThunderlightBurstAOE, 35f);

abstract class ThunderlightBurst(ModuleBase module, uint aid, float length) : Components.SimpleAOEs(module, aid, new AOEShapeRect(length, 4f));
sealed class ThunderlightBurst1(ModuleBase module) : ThunderlightBurst(module, (uint)AID.ThunderlightBurst1, 42f);
sealed class ThunderlightBurst2(ModuleBase module) : ThunderlightBurst(module, (uint)AID.ThunderlightBurst2, 49f);
sealed class ThunderlightBurst3(ModuleBase module) : ThunderlightBurst(module, (uint)AID.ThunderlightBurst3, 35f);
sealed class ThunderlightBurst4(ModuleBase module) : ThunderlightBurst(module, (uint)AID.ThunderlightBurst4, 36f);

sealed class Artillery(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.Artillery1, (uint)AID.Artillery2, (uint)AID.Artillery3, (uint)AID.Artillery4], new AOEShapeRect(10f, 5f));

sealed class Pummel(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Pummel);
sealed class ThunderlightFlurry(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.ThunderlightFlurry, 6f);

sealed class D032FirearmsStates : StateMachineBuilder
{
    public D032FirearmsStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<DynamicDominanceArenaChange>()
            .ActivateOnEnter<DynamicDominance>()
            .ActivateOnEnter<ThunderlightBurstAOE>()
            .ActivateOnEnter<ThunderlightBurst1>()
            .ActivateOnEnter<ThunderlightBurst2>()
            .ActivateOnEnter<ThunderlightBurst3>()
            .ActivateOnEnter<ThunderlightBurst4>()
            .ActivateOnEnter<Artillery>()
            .ActivateOnEnter<Pummel>()
            .ActivateOnEnter<ThunderlightFlurry>();
    }
}

[ModuleInfo(CFCID = 829u, NameID = 12888u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class D032Firearms(WorldState ws, Actor primary) : ModuleBase(ws, primary, ArenaCenter, StartingBounds)
{
    public static readonly WPos ArenaCenter = new(-85f, -155f);
    public static readonly ArenaBoundsSquare StartingBounds = new(24.5f);
    public static readonly ArenaBoundsSquare DefaultBounds = new(20f);
}
