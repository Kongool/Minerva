// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Dungeon.D04Vanguard.D041CommanderR8;

public enum OID : uint
{
    Boss = 0x411D, // R3.24
    VanguardSentryR8 = 0x41BC, // R3.24
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 36403, // Boss->player, no cast, single-target

    Electrowave = 36571, // Boss->self, 5.0s cast, range 60 circle

    EnhancedMobility1 = 39140, // Boss->location, 10.0s cast, range 14 width 6 rect, in, start -60°
    EnhancedMobility2 = 39141, // Boss->location, 10.0s cast, range 14 width 6 rect, out, start 60°
    EnhancedMobility3 = 36559, // Boss->location, 10.0s cast, range 14 width 6 rect, out, start -60°
    EnhancedMobility4 = 36560, // Boss->location, 10.0s cast, range 14 width 6 rect, in, start 60°

    EnhancedMobility5 = 36563, // Helper->self, 10.5s cast, range 10 width 14 rect, sword right
    EnhancedMobility6 = 36564, // Helper->self, 10.5s cast, range 10 width 14 rect, sword left
    EnhancedMobility7 = 37184, // Helper->self, 10.5s cast, range 20 width 14 rect, sword right
    EnhancedMobility8 = 37191, // Helper->self, 10.5s cast, range 20 width 14 rect, sword left

    RapidRotaryVisual1 = 36561, // Boss->self, no cast, single-target
    RapidRotaryVisual2 = 36562, // Boss->self, no cast, single-target
    RapidRotaryVisual3 = 39142, // Boss->self, no cast, single-target
    RapidRotaryVisual4 = 39143, // Boss->self, no cast, single-target

    RapidRotaryCone = 36566, // Helper->self, no cast, range 14 120-degree cone
    RapidRotaryDonutSegmentBig = 36567, // Helper->self, no cast, range 17-28 donut, 120° donut segment
    RapidRotaryDonutSegmentSmall = 36565, // Helper->self, no cast, range 11-17 donut, 120° donut segment

    Dispatch = 36568, // Boss->self, 4.0s cast, single-target
    Rush = 36569, // VanguardSentryR8->location, 6.0s cast, width 5 rect charge
    AerialOffensive = 36570, // VanguardSentryR8->location, 9.0s cast, range 4 circle, visual starts at radius 4, final size 14

    ElectrosurgeVisual = 36572, // Boss->self, 4.0+1.0s cast, single-target
    Electrosurge = 36573 // Helper->player, 5.0s cast, range 5 circle, spread
}

sealed class ElectrowaveArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Electrowave && Module.Bounds.Radius > 19f)
        {
            _aoe = [new(new AOEShapeCustom([new Square(D041CommanderR8.ArenaCenter, 20f)], [new Square(D041CommanderR8.ArenaCenter, 17f)]), Module.Center, default, Module.CastFinishAt(spell, 0.4d))];
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x0A && state == 0x00020001u)
        {
            Module.Bounds = D041CommanderR8.DefaultBounds;
            _aoe = [];
        }
    }
}

sealed class EnhancedMobility(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private readonly AOEShapeRect[] rects = [new(14f, 3f), new(10f, 7f), new(20f, 7f)];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        var shape = spell.Action.ID switch
        {
            (uint)AID.EnhancedMobility1 or (uint)AID.EnhancedMobility2 or (uint)AID.EnhancedMobility3 or (uint)AID.EnhancedMobility4 => rects[0],
            (uint)AID.EnhancedMobility5 or (uint)AID.EnhancedMobility6 => rects[1],
            (uint)AID.EnhancedMobility7 or (uint)AID.EnhancedMobility8 => rects[2],
            _ => null
        };
        if (shape != null)
        {
            _aoes.Add(new(shape, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell)));
            if (_aoes.Count == 2)
            {
                var aoes = CollectionsMarshal.AsSpan(_aoes);
                ref var aoe1 = ref aoes[0];
                ref var aoe2 = ref aoes[1];
                if (aoe1.Activation > aoe2.Activation)
                {
                    (aoe1, aoe2) = (aoe2, aoe1);
                }
            }
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (_aoes.Count != 0)
        {
            switch (spell.Action.ID)
            {
                case (uint)AID.EnhancedMobility1:
                case (uint)AID.EnhancedMobility2:
                case (uint)AID.EnhancedMobility3:
                case (uint)AID.EnhancedMobility4:
                case (uint)AID.EnhancedMobility5:
                case (uint)AID.EnhancedMobility6:
                case (uint)AID.EnhancedMobility7:
                case (uint)AID.EnhancedMobility8:
                    _aoes.RemoveAt(0);
                    break;
            }
        }
    }
}

sealed class RapidRotary(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly Angle a60 = 60f.Degrees();
    private static readonly Angle a120 = 120f.Degrees();
    private readonly List<AOEInstance> _aoes = [];
    private readonly AOEShapeDonutSector donutSectorSmall = new(11f, 17f, a60);
    private readonly AOEShapeDonutSector donutSectorBig = new(17f, 28f, a60);
    private readonly AOEShapeCone cone = new(14f, a60);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
        {
            return [];
        }
        var aoes = CollectionsMarshal.AsSpan(_aoes);
        if (count > 2)
        {
            var color = Colors.Danger;
            for (var i = 0; i < 2; ++i)
            {
                aoes[i].Color = color;
            }
        }
        return aoes;
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.EnhancedMobility1:
                AddAOEs(donutSectorBig, -a60);
                break;
            case (uint)AID.EnhancedMobility2:
                AddAOEs(cone, a60);
                break;
            case (uint)AID.EnhancedMobility3:
                AddAOEs(cone, -a60);
                break;
            case (uint)AID.EnhancedMobility4:
                AddAOEs(donutSectorBig, a60);
                break;
        }
        void AddAOEs(AOEShape shape2, Angle initialAngle)
        {
            var pos = Module.Center.Quantized();
            for (var i = 0; i < 3; ++i)
            {
                var activation = Module.CastFinishAt(spell, 1.8d + i * 0.3d);
                var angle = initialAngle - i * a120;
                _aoes.Add(new(donutSectorSmall, pos, angle, activation));
                _aoes.Add(new(shape2, pos, angle, activation));
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (_aoes.Count != 0 && spell.Action.ID is (uint)AID.RapidRotaryCone or (uint)AID.RapidRotaryDonutSegmentBig or (uint)AID.RapidRotaryDonutSegmentSmall)
        {
            _aoes.RemoveAt(0);
        }
    }
}

sealed class Electrowave(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Electrowave);

sealed class Rush(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.Rush, 2.5f);
sealed class AerialOffensive(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AerialOffensive, 14f, 4);
sealed class Electrosurge(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.Electrosurge, 5f);

sealed class D041CommanderR8States : StateMachineBuilder
{
    public D041CommanderR8States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ElectrowaveArenaChange>()
            .ActivateOnEnter<Electrowave>()
            .ActivateOnEnter<RapidRotary>()
            .ActivateOnEnter<EnhancedMobility>()
            .ActivateOnEnter<Rush>()
            .ActivateOnEnter<AerialOffensive>()
            .ActivateOnEnter<Electrosurge>();
    }
}

[ModuleInfo(CFCID = 831u, NameID = 12750u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class D041CommanderR8(WorldState ws, Actor primary) : ModuleBase(ws, primary, ArenaCenter, StartingBounds)
{
    public static readonly WPos ArenaCenter = new(-100f, 207f);
    public static readonly ArenaBoundsSquare StartingBounds = new(19.5f);
    public static readonly ArenaBoundsSquare DefaultBounds = new(17f);
}
