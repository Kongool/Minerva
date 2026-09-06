// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Criterion.C01AMT.C013PariOfPlenty;

class Fireflight(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> aoes = [];
    public List<PathAOE> pathAOEs = [];
    public int side = 0;

    public class PathAOE()
    {
        public Actor? actor;
        public WPos startPosition;
        public WPos endPosition;
        public int aoePosition;
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is var id && id is (uint)AID.FireflightByPyrelightLeft or (uint)AID.FireflightByEmberlightLeft)
        {
            side = -1;
        }
        else if (id is (uint)AID.FireflightByPyrelightRight or (uint)AID.FireflightByEmberlightRight)
        {
            side = 1;
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.CarpetRide1 or (uint)AID.CarpetRide2)
        {
            if (pathAOEs.Count > 0)
            {
                pathAOEs.RemoveAt(0);
                NumCasts++;
            }
        }
    }

    public override void OnUntethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID == (uint)TetherID.CarpetRideTether)
        {
            Service.Log.Debug($"tether {source.Position}");
            var pathAoe = new PathAOE();
            var actor = World.Actors.Find(tether.Target);

            if (actor == null)
            {
                return;
            }

            pathAoe.actor = actor;
            pathAoe.startPosition = source.Position;
            pathAoe.endPosition = actor.Position;
            pathAoe.aoePosition = pathAOEs.Count + 1;
            pathAOEs.Add(pathAoe);
        }
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        aoes.Clear();

        var dirOffset = side == 1 ? 90.Degrees() : (-90).Degrees();

        var shown = 0;
        foreach (var pathAOE in pathAOEs)
        {
            if (shown >= 2)
            {
                break;
            }

            var nextDir = (pathAOE.endPosition - pathAOE.startPosition);
            Angle nextAng = nextDir.ToAngle();
            var baseAng = 0.Degrees();
            var delta = baseAng.DistanceToAngle(nextAng);
            var final = (baseAng + delta).Normalized();

            var halfWidth = pathAOE.aoePosition == 2 ? 27 : 60;
            var colour = (shown < 1) ? Colors.Danger : Colors.AOE;

            var shape = new AOEShapeRect(40, halfWidth, 0f, dirOffset);
            aoes.Add(new AOEInstance(shape, pathAOE.startPosition, final, default, colour, (shown < 1)));
            shown++;
        }

        return CollectionsMarshal.AsSpan(aoes);
    }
}

// TODO find out who actually gets the stack - is it random?
class FireflightStackSpread(ModuleBase module) : Components.UniformStackSpread(module, 3, 3)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is var id && id is (uint)AID.FireflightByPyrelightLeft or (uint)AID.FireflightByPyrelightRight)
        {
            var stackPlayer = Raid.WithSlot(false, true, true).FirstOrDefault().Item2;
            AddStack(stackPlayer);
        }
        else if (id is (uint)AID.FireflightByEmberlightLeft or (uint)AID.FireflightByEmberlightRight)
        {
            foreach (var player in Raid.WithoutSlot(true, true, true))
            {
                AddSpread(player);
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is var id && id == (uint)AID.FireflightByPyrelightStack)
        {
            Stacks.Clear();
        }
        else if (id == (uint)AID.FireflightByEmberlightSpread)
        {
            Spreads.Clear();
        }
    }
}

class SunCirclet(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SunCirclet, new AOEShapeDonut(8f, 60f));