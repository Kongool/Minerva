// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.CriticalEngagement.CE204AppallingBehavior;

public enum OID : uint
{
    Pallmagia = 0x4D8F, // R3.504
    Pallkeeper = 0x4D90, // R2.300, x4
    PallkeeperVFX = 0x1EC02A, // R0.500, x4, EventObj type - Used to display the cast type hints vfx in-game
    RouletteRing1 = 0x1EC02B, // R0.500, x0 (spawn during fight), EventObj type
    RouletteRing2 = 0x1EC02C, // R0.500, x0 (spawn during fight), EventObj type
    Deathwall = 0x4D91, // R1.000, x1
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 50494, // Pallmagia->player, no cast, single-target
    Deathwall = 49771, // 4D91->self, no cast, range 20-25 donut

    BadBreathBoss = 50490, // Pallmagia->self, 4.3+0.7s cast, single-target
    BadBreath = 50491, // Helper->self, 5.0s cast, range 50 100-degree cone
    PlaincrackerBoss = 50492, // Pallmagia->self, 4.3+0.7s cast, single-target
    Plaincracker = 50493, // Helper->self, 5.0s cast, range 15 circle
    GreatWhirlwindCast = 49798, // Pallmagia->self, 4.3+0.7s cast, single-target
    GreatWhirlwindVisual = 49799, // Helper->self, 5.0s cast, single-target
    GreatWhirlwind = 50450, // Helper->self, 5.0s cast, ???
    OccultMissileCast = 49795, // Pallmagia->self, 3.3+0.7s cast, single-target
    OccultMissile = 49797, // Helper->location, 4.0s cast, range 6 circle
    LilliputianLyricCast = 49791, // Pallmagia->self, 4.3+0.7s cast, single-target
    LilliputianLyric = 49792, // Helper->self, 5.0s cast, range 40 180-degree cone
    MagicHammerCast = 49793, // Pallmagia->self, 3.0s cast, single-target
    MagicHammer = 49794, // Helper->location, 5.5s cast, range 8 circle

    Summon = 49772, // Pallmagia->self, 3.0s cast, single-target
    EsotericInstruction = 49773, // Pallmagia->self, 13.0s cast, single-target
    EsotericInstructionSwap = 49774, // Pallmagia->self, 13.0s cast, single-target // TODO check if this one is casted they will swap
    ReversePolarity = 49775, // Pallmagia->self, 5.0s cast, single-target
    BadBreathPallkeeperVisual = 49776, // 4D90->self, no cast, single-target
    BadBreathPallkeeper = 49777, // Helper->self, 3.0s cast, range 50 100-degree cone
    PlaincrackerPallkeeperVisual = 49778, // 4D90->self, no cast, single-target
    PlaincrackerPallkeeper = 49779, // Helper->self, 3.0s cast, range 30 circle
    PallKeeperTeleport = 49786, // 4D90->location, no cast, single-target
    PallKeeperTeleport1 = 49785, // 4D90->location, no cast, single-target
    PallKeeperTeleport2 = 49784, // 4D90->location, no cast, single-target

    RouletteVisual = 49787, // Pallmagia->self, 4.0s cast, single-target
    RouletteCircle = 49788, // Helper->self, no cast, range 5 circle
    RouletteInner = 49789, // Helper->self, no cast, range 5-12 donut
    RouletteOuter = 49790 // Helper->self, no cast, range 12-20 donut
}

public enum TetherID : uint
{
    IconTether = 14, // 4D90->Pallmagia
    SwapTether = 207, // 4D90->4D90
}

sealed class BadBreath(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BadBreath, new AOEShapeCone(50f, 50f.Degrees()));
sealed class Plaincracker(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Plaincracker, 15f);
sealed class GreatWhirlwind(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.GreatWhirlwind);
sealed class LilliputianLyric(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LilliputianLyric, new AOEShapeCone(40f, 90f.Degrees()));

sealed class OccultMissile : Components.SimpleAOEs
{
    public OccultMissile(ModuleBase module) : base(module, (uint)AID.OccultMissile, 6f, 8)
    {
        MaxDangerColor = 4;
    }
}

sealed class MagicHammer : Components.SimpleAOEs
{
    public MagicHammer(ModuleBase module) : base(module, (uint)AID.MagicHammer, 8f, 8)
    {
        MaxDangerColor = 4;
    }
}

// TODO add spell timers depending on cast version
sealed class EsotericInstruction(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private readonly AOEShapeCone cone = new(50f, 50f.Degrees());
    private readonly AOEShapeCircle circle = new(30f);
    private bool swap;
    private bool reversed;
    private readonly List<Actor> _keepers = module.Enemies((uint)OID.Pallkeeper);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is var id && id == (uint)AID.EsotericInstructionSwap)
        {
            swap = true;
            reversed = false;
        }
        else if (id == (uint)AID.ReversePolarity)
        {
            reversed = true;
        }
        else if (id is (uint)AID.BadBreathPallkeeper or (uint)AID.PlaincrackerPallkeeper)
        {
            // The cast lands at the keeper that fires next: it names the zone (by position) and its exact
            // time. The list order from the markers is right only until the swap tethers shuffle it, and
            // the estimated activations drifted ten seconds from the real casts on the 2026-09-05 log.
            var at = spell.LocXZ;
            for (var i = 0; i < _aoes.Count; ++i)
            {
                if ((_aoes[i].Origin - at).LengthSq() < 1f)
                {
                    var aoe = _aoes[i];
                    aoe.Activation = Module.CastFinishAt(spell);
                    aoe.Risky = true;
                    _aoes.RemoveAt(i);
                    _aoes.Insert(0, aoe);
                    break;
                }
            }
        }
    }

    public override void OnActorEAnim(Actor actor, uint state)
    {
        if (actor.OID == (uint)OID.PallkeeperVFX && state is 0x00010002u or 0x00100020u)
        {
            // the animation comes from a different actor but on the same position
            var count = _keepers.Count;
            var pos = actor.Position;
            var isstate10002 = state == 0x00010002u;
            for (var i = 0; i < count; ++i)
            {
                var keeper = _keepers[i];
                // Within a yalm, not equal: the first set of keepers spawns on whole-number coordinates that
                // match the marker exactly, later sets stand a few centimetres off (806.973 against 807.000 on
                // the 2026-09-05 recording), so exact equality silently dropped one zone per later phase --
                // three dodged, the fourth eaten for uptime, twice in one pull. BossmodReborn has the same test.
                if ((keeper.Position - pos).LengthSq() < 1f)
                {
                    _aoes.Add(new(isstate10002 ? cone : circle, pos.Quantized(), actor.Rotation, actorID: keeper.InstanceID));
                    break;
                }
            }
        }
    }

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID == (uint)TetherID.SwapTether)
        {
            var tetherInfo = tether;
            var aoes = CollectionsMarshal.AsSpan(_aoes);
            var len = aoes.Length;
            var sID = source.InstanceID;
            var tID = tetherInfo.Target;

            var pallKeeperSource = -1;
            var pallKeeperTarget = -1;
            for (var i = 0; i < len; ++i)
            {
                var id = aoes[i].ActorID;
                if (id == sID)
                {
                    pallKeeperSource = i;
                }
                else if (id == tID)
                {
                    pallKeeperTarget = i;
                }
                if (pallKeeperSource != -1 && pallKeeperTarget != -1)
                {
                    break;
                }
            }

            if (pallKeeperSource < 0 || pallKeeperTarget < 0)
            {
                return;
            }

            ref var aoe1 = ref aoes[pallKeeperSource];
            ref var aoe2 = ref aoes[pallKeeperTarget];
            (aoe1.Origin, aoe2.Origin) = (aoe2.Origin, aoe1.Origin);
            (aoe1.Rotation, aoe2.Rotation) = (aoe2.Rotation, aoe1.Rotation);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.EsotericInstruction or (uint)AID.EsotericInstructionSwap)
        {
            var swapping = spell.Action.ID == (uint)AID.EsotericInstructionSwap;

            var count = _aoes.Count;
            var aoes = CollectionsMarshal.AsSpan(_aoes);
            for (var i = 0; i < count; i++)
            {
                aoes[i].Activation = World.FutureTime((swapping ? 6.6d : 0d) + 6d + i * 4.5d);
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.BadBreathPallkeeper or (uint)AID.PlaincrackerPallkeeper)
        {
            if (_aoes.Count > 0)
            {
                _aoes.RemoveAt(0);
            }
        }
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        // don't show until swapped
        if (!swap || swap && reversed)
        {
            var count = _aoes.Count;
            if (count == 0)
            {
                return [];
            }

            var max = count > 2 ? 2 : count;
            var aoeSpan = CollectionsMarshal.AsSpan(_aoes);
            // Only risky zones become forbidden for the dodge. The flag was set once at creation for the
            // first two entries and never moved, so after two pops the remaining zones were drawn but never
            // forbidden -- the third and fourth cast of every phase. The two shown are the two that matter.
            for (var i = 0; i < max; ++i)
                aoeSpan[i].Risky = true;
            if (count > 1)
            {
                ref var aoe0 = ref aoeSpan[0];
                aoe0.Color = Colors.Danger;
            }
            return aoeSpan[..max];
        }

        return [];
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        // move if no swap or already swapped, stay in center while waiting for swap
        if (_aoes.Count != 0)
        {
            if (!swap || swap && reversed)
            {
                base.AddAIHints(slot, actor, assignment, hints);
            }
            else
            {
                hints.GoalZones.Add(AIHints.GoalSingleTarget(Module.Center, 5f, 5f));
            }
        }
    }
}

sealed class Roulette(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> aoes = [];
    private readonly AOEShapeDonutSector outer = new(12f, 20f, 67.5f.Degrees(), 22.5f.Degrees());
    private readonly AOEShapeDonutSector inner = new(5f, 12f, 60f.Degrees(), -60f.Degrees());
    private readonly AOEShapeCircle circle = new(5f);
    private readonly Angle outerDiff = 67.5f.Degrees();
    private readonly Angle innerDiff = 120f.Degrees();

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        return CollectionsMarshal.AsSpan(aoes);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.RouletteVisual)
        {
            var pos = Module.Center;
            aoes.Add(new(circle, pos, activation: World.FutureTime(18.3d), shapeDistance: circle.Distance(pos, default)));
        }
    }

    public override void OnActorEAnim(Actor actor, uint state)
    {
        if (actor.OID is var id && id is (uint)OID.RouletteRing1 or (uint)OID.RouletteRing2)
        {
            if (state is 0x00040010u or 0x00040020u)
            {
                var act = World.FutureTime(10d);
                var isCW = state == 0x00040020u;
                var isring2 = id == (uint)OID.RouletteRing2;
                var shape = isring2 ? inner : outer;
                var diff = isring2 ? innerDiff : outerDiff;
                var center = Module.Center;
                var rot = actor.Rotation + diff * (isCW ? -1f : 1f);

                aoes.Add(new(shape, center, rot, act, shapeDistance: shape.Distance(center, rot)));
                var a180 = rot + 180f.Degrees();
                aoes.Add(new(shape, center, a180, act, shapeDistance: shape.Distance(center, a180)));
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.RouletteCircle)
        {
            aoes.Clear();
        }
    }
}

[SkipLocalsInit]
sealed class CE204AppallingBehaviorStates : StateMachineBuilder
{
    public CE204AppallingBehaviorStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<BadBreath>()
            .ActivateOnEnter<Plaincracker>()
            .ActivateOnEnter<EsotericInstruction>()
            .ActivateOnEnter<GreatWhirlwind>()
            .ActivateOnEnter<OccultMissile>()
            .ActivateOnEnter<LilliputianLyric>()
            .ActivateOnEnter<MagicHammer>()
            .ActivateOnEnter<Roulette>();
    }
}

[ModuleInfo(Group = ModuleGroup.CriticalEngagement, CFCID = 1093u, NameID = 59u, PrimaryActorOID = (uint)OID.Pallmagia, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Gynorhino (ported from BMR)")]
[SkipLocalsInit]
public sealed class CE204AppallingBehavior(WorldState ws, Actor primary) : ModuleBase(ws, primary, new WPos(807f, -562f).Quantized(), new ArenaBoundsCircle(20f))
{
    protected override bool CheckPull() => base.CheckPull() && Raid.Player()!.Position.InCircle(Module.Center, 20f);
}
