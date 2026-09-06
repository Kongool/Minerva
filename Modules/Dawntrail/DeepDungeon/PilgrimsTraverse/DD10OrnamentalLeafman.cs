// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.DeepDungeon.PilgrimsTraverse.DD10OrnamentalLeafman;

public enum OID : uint
{
    OrnamentalLeafman = 0x460D, // R4.8
    StrikingShrublet = 0x460E, // R1.8
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 45130, // OrnamentalLeafman/StrikingShrublet->player, no cast, single-target
    Teleport = 44052, // OrnamentalLeafman->location, no cast, single-target

    BranchOut = 44051, // OrnamentalLeafman->self, 3.0s cast, single-target
    HedgeMazingVisual1 = 44054, // StrikingShrublet->self, 12.7+0,8s cast, single-target
    HedgeMazingVisual2 = 44053, // OrnamentalLeafman->self, 12.7+0,8s cast, single-target
    HedgeMazing1 = 44855, // Helper->self, 13.5s cast, range 14 circle
    HedgeMazing2 = 44854, // Helper->self, 13.5s cast, range 14 circle

    LittleLeafmash = 44059, // StrikingShrublet->player, no cast, range 5 circle, seems to target random players

    LeafmashTelegraph = 44058, // Helper->location, no cast, single-target
    LeafmashVisual1 = 44056, // OrnamentalLeafman->location, no cast, single-target
    LeafmashVisual2 = 44055, // OrnamentalLeafman->location, 10.0+0,9s cast, single-target
    Leafmash = 44057 // Helper->self, 1.9s cast, range 15 circle
}

[SkipLocalsInit]
sealed class HedgeMazing(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.HedgeMazing1, (uint)AID.HedgeMazing2], 14f, riskyWithSecondsLeft: 5d);

[SkipLocalsInit]
sealed class Leafmash(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private readonly AOEShapeCircle circle = new(14f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
        {
            return [];
        }
        var max = count > 3 ? 3 : count;
        return CollectionsMarshal.AsSpan(_aoes)[..max];
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.LeafmashTelegraph)
        {
            var pos = spell.TargetXZ;
            var count = _aoes.Count;
            _aoes.Add(new(circle, pos, default, World.FutureTime(8.9d - 0.3d * count), shapeDistance: circle.Distance(pos, default)));
            if (count == 1)
            {
                _aoes.Ref(0).Color = Colors.Danger;
            }
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        var count = _aoes.Count;
        if (count != 0 && spell.Action.ID == (uint)AID.Leafmash)
        {
            _aoes.RemoveAt(0);
            if (count > 2)
            {
                _aoes.Ref(0).Color = Colors.Danger;
            }
        }
    }
}

[SkipLocalsInit]
sealed class LittleLeafmash(ModuleBase module) : Components.GenericStackSpread(module)
{
    private bool active;
    private readonly DD10OrnamentalLeafman bossmod = (DD10OrnamentalLeafman)module;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.LeafmashVisual2)
        {
            active = true;
        }
    }

    public override void Update()
    {
        if (active)
        {
            var adds = bossmod.Shrublets;
            var count = adds.Count;
            if (Spreads.Count == 0)
            {
                for (var i = 0; i < count; ++i)
                {
                    if (adds[i].IsTargetable)
                    {
                        var party = Raid.WithoutSlot(true, true, true);
                        var len = party.Length;
                        for (var j = 0; j < len; ++j)
                        {
                            Spreads.Add(new(party[j], 5f));
                        }
                        return;
                    }
                }
            }

            for (var i = 0; i < count; ++i)
            {
                var a = adds[i];
                if (a.IsTargetable && !a.IsDead)
                {
                    return;
                }
            }
            Spreads.Clear();
            active = false;
        }
    }
}

[SkipLocalsInit]
sealed class DD10OrnamentalLeafmanStates : StateMachineBuilder
{
    public DD10OrnamentalLeafmanStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<LittleLeafmash>()
            .ActivateOnEnter<Leafmash>()
            .ActivateOnEnter<HedgeMazing>();
    }
}

[ModuleInfo(CFCID = 1032u, NameID = 13979u, PrimaryActorOID = (uint)OID.OrnamentalLeafman, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
[SkipLocalsInit]
public sealed class DD10OrnamentalLeafman : ModuleBase
{
    private static readonly WPos arenaCenter = new(-300f, -300f);

    public DD10OrnamentalLeafman(WorldState ws, Actor primary) : base(ws, primary, arenaCenter, new ArenaBoundsCustom([new Polygon(arenaCenter, 19.5f, 36)]))
    {
        Shrublets = Enemies((uint)OID.StrikingShrublet);
    }
    public readonly List<Actor> Shrublets;

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Shrublets);
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.StrikingShrublet => 1,
                _ => 0
            };
            if (e.Priority == 1)
            {
                e.ForbidDOTs = true;
            }
        }
    }
}
