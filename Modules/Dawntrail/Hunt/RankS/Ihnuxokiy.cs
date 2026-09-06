// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Hunt.RankS.Ihnuxokiy;

public enum OID : uint
{
    Boss = 0x4582 // R7.0
}

public enum AID : uint
{
    AutoAttack = 870, // Boss->player, no cast, single-target

    Gapcloser = 39772, // Boss->location, no cast, single-target
    AbyssalSmog1 = 39773, // Boss->self, 8.0s cast, range 40 180-degree cone
    AbyssalSmog2 = 39828, // Boss->self, 10.0s cast, range 40 180-degree cone
    Aetherstock1 = 39778, // Boss->self, 4.0s cast, single-target, thunderspark
    Aetherstock2 = 39779, // Boss->self, 4.0s cast, single-target, cyclonic ring
    CyclonicRing = 39781, // Boss->self, no cast, range 8-40 donut
    ChaoticStorm = 39777, // Boss->self, 5.0s cast, range 40 circle, raidwide, forced march debuffs
    Thunderspark = 39780, // Boss->self, no cast, range 12 circle
    RazorZephyr = 39774, // Boss->self, 3.0s cast, range 50 width 12 rect
    Blade = 39776 // Boss->player, 5.0s cast, single-target, tankbuster
}

public enum SID : uint
{
    AboutFace = 2162, // Boss->player, extra=0x0
    LeftFace = 2163, // Boss->player, extra=0x0
    RightFace = 2164, // Boss->player, extra=0x0
    ForwardMarch = 2161 // Boss->player, extra=0x0
}

sealed class AetherstockAbyssalSmog(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEShape? shape;
    private static readonly AOEShapeCone cone = new(40f, 90f.Degrees());
    private static readonly AOEShapeDonut donut = new(8f, 40f);
    private static readonly AOEShapeCircle circle = new(12f);
    private readonly List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
            return [];

        var aoes = CollectionsMarshal.AsSpan(_aoes);
        for (var i = 0; i < count; ++i)
        {
            ref var aoe = ref aoes[i];
            if (i == 0)
            {
                if (count > 1)
                    aoe.Color = Colors.Danger;
                aoe.Risky = true;
            }
            else
                aoe.Risky = false;
        }
        return aoes;
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.Aetherstock1:
                shape = circle;
                break;
            case (uint)AID.Aetherstock2:
                shape = donut;
                break;
            case (uint)AID.AbyssalSmog1:
            case (uint)AID.AbyssalSmog2:
                var position = spell.LocXZ;
                _aoes.Add(new(cone, position, spell.Rotation, Module.CastFinishAt(spell)));
                if (shape != null)
                {
                    _aoes.Add(new(shape, position, default, Module.CastFinishAt(spell, 2.2f)));
                    shape = null;
                }
                break;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (_aoes.Count == 0)
            return;
        switch (spell.Action.ID)
        {
            case (uint)AID.AbyssalSmog1:
            case (uint)AID.AbyssalSmog2:
            case (uint)AID.CyclonicRing:
            case (uint)AID.Thunderspark:
                _aoes.RemoveAt(0);
                break;
        }
    }
}

sealed class ChaoticStormForcedMarch(ModuleBase module) : Components.StatusDrivenForcedMarch(module, 3f, (uint)SID.ForwardMarch, (uint)SID.AboutFace, (uint)SID.LeftFace, (uint)SID.RightFace);
sealed class ChaoticStorm(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.ChaoticStorm, "Raidwide + forced march debuffs");
sealed class RazorZephyr(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RazorZephyr, new AOEShapeRect(50f, 6f));
sealed class Blade(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Blade);

sealed class IhnuxokiyStates : StateMachineBuilder
{
    public IhnuxokiyStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ChaoticStorm>()
            .ActivateOnEnter<ChaoticStormForcedMarch>()
            .ActivateOnEnter<RazorZephyr>()
            .ActivateOnEnter<Blade>()
            .ActivateOnEnter<AetherstockAbyssalSmog>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 13444u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class Ihnuxokiy(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
