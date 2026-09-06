// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex7Doomtrain;

[SkipLocalsInit]
sealed class PlasmaBeam(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private readonly AOEShapeRect rectLong = new(30f, 2.5f), rectMedium = new(20f, 2.5f), rectShort = new(10f, 2.5f);
    private readonly ArenaChanges arena = module.FindComponent<ArenaChanges>()!;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.LevinSignal && actor.PosRot.Y == default)
        {
            var pos = actor.Position;
            var origin = pos.Quantized();
            var rect = rectLong;
            if (arena.Car == 2u)
            {
                var posX = pos.X;
                if (posX == 92.5f)
                {
                    rect = rectMedium;
                }
                else if (posX == 107.5f)
                {
                    rect = rectShort;
                }
            }
            var rot = actor.Rotation;
            _aoes.Add(new(rect, origin, rot, World.FutureTime(6.9d), shapeDistance: rect.Distance(origin, rot)));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.PlasmaBeam1)
        {
            ++NumCasts;
            _aoes.Clear();
        }
    }
}
