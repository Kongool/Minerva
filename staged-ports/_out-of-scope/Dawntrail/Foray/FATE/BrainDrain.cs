// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.FATE.BrainDrain;

public enum OID : uint
{
    Boss = 0x4737,
    Helper = 0x4738
}

public enum AID : uint
{
    AutoAttack = 42005, // Boss->player, no cast, single-target
    Teleport = 41995, // Boss->location, no cast, single-target

    ZombieScalesVisual1 = 41998, // Boss->self, 8.0s cast, single-target
    ZombieScalesVisual2 = 41997, // Boss->self, 8.0s cast, single-target
    ZombieScalesVisual3 = 41999, // Boss->self, 8.0s cast, single-target
    ZombieScalesVisual4 = 41996, // Boss->self, 8.0s cast, single-target
    ZombieScales1 = 42000, // Helper->location, 8.0s cast, range 40 45-degree cone
    ZombieScales2 = 42001, // Helper->location, 8.0s cast, range 40 45-degree cone

    AeroIIVisual = 42017, // Boss->self, 3.0s cast, single-target
    AeroII = 42018, // Helper->location, 3.0s cast, range 4 circle

    ZombieBreath = 42004, // Boss->self, 5.0s cast, range 40 180-degree cone
    TripleFlight = 42012, // Boss->self, 4.0s cast, range 10-20 donut
    CyclonicRing = 42013, // Boss->self, no cast, range 10-20 donut
    CycloneVisual = 42010, // Boss->self, no cast, single-target
    Cyclone1 = 42011, // Helper->location, no cast, range 10 circle
    Cyclone2 = 42009, // Helper->location, 4.0s cast, range 10 circle
    FlashFoehn = 42014, // Boss->self, no cast, range 80 width 10 rect

    QuarryLakeVisual = 42002, // Boss->self, 5.0s cast, single-target, gaze
    QuarryLake = 42003, // Helper->location, 5.0s cast, range 40 circle

    BreathWingVisual1 = 42006, // Boss->self, 5.0s cast, single-target
    BreathWingVisual2 = 42008, // Boss->self, 4.0s cast, single-target
    BreathWing = 42007 // Helper->location, 5.0s cast, range 30 circle, raidwide
}

sealed class ZombieScales : Components.SimpleAOEGroups
{
    public ZombieScales(ModuleBase module) : base(module, [(uint)AID.ZombieScales1, (uint)AID.ZombieScales2], new AOEShapeCone(40f, 22.5f.Degrees()), 6)
    {
        MaxDangerColor = 2;
    }
}
sealed class AeroII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AeroII, 4f);
sealed class ZombieBreath(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ZombieBreath, new AOEShapeCone(40f, 90f.Degrees()));
sealed class QuarryLake(ModuleBase module) : Components.CastGaze(module, (uint)AID.QuarryLake, range: 40f);
sealed class BreathWing(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.BreathWing);

sealed class TripleFlightCyclone(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeDonut donut = new(10f, 20f);
    private static readonly AOEShapeCircle circle = new(10f);
    private static readonly AOEShapeRect rect = new(40f, 5f, 40f);
    private readonly List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
            return [];
        var max = count > 1 ? 2 : 1;
        var aoes = CollectionsMarshal.AsSpan(_aoes)[..max];
        aoes[0].Risky = true;
        if (count > 1)
        {
            aoes[0].Color = Colors.Danger;
        }
        return aoes;
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        var id = spell.Action.ID;
        if (id == (uint)AID.TripleFlight)
        {
            AddAOE(donut);
            AddAOE(circle, 2.1f);
            AddAOE(rect, 2.1f);
        }
        else if (id == (uint)AID.Cyclone2)
        {
            AddAOE(circle);
            AddAOE(donut, 2.1f);
            AddAOE(rect, 2.1f);
        }
        void AddAOE(AOEShape shape, float delay = default) => _aoes.Add(new(shape, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell, delay), risky: false));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (_aoes.Count != 0 && spell.Action.ID is (uint)AID.TripleFlight or (uint)AID.Cyclone1 or (uint)AID.Cyclone2 or (uint)AID.CyclonicRing or (uint)AID.FlashFoehn)
            _aoes.RemoveAt(0);
    }
}

sealed class BrainDrainStates : StateMachineBuilder
{
    public BrainDrainStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ZombieScales>()
            .ActivateOnEnter<AeroII>()
            .ActivateOnEnter<ZombieBreath>()
            .ActivateOnEnter<QuarryLake>()
            .ActivateOnEnter<BreathWing>()
            .ActivateOnEnter<TripleFlightCyclone>();
    }
}

[ModuleInfo(Group = ModuleGroup.ForayFATE, CFCID = 1018u, NameID = 1967u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class BrainDrain(WorldState ws, Actor primary) : OpenWorldFate(ws, primary);
