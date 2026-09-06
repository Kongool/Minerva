// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A22UltimaOmega;

sealed class GuidedMissile(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GuidedMissile, 6f);

sealed class TrajectoryProjection(ModuleBase module) : Components.GenericBaitAway(module, centerAtTarget: true)
{
    private DateTime activation;
    private readonly AOEShapeCircle circle = new(6);

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        switch (iconID)
        {
            case (uint)IconID.GuidedMissileEast:
                AddBait(new(5f, default));
                break;
            case (uint)IconID.GuidedMissileWest:
                AddBait(new(-5f, default));
                break;
            case (uint)IconID.GuidedMissileSouth:
                AddBait(new(default, 5f));
                break;
            case (uint)IconID.GuidedMissileNorth:
                AddBait(new(default, -5f));
                break;
        }
        void AddBait(WDir offset) => CurrentBaits.Add(new(Module.PrimaryActor, actor, circle, activation, offset: offset));
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.TrajectoryProjection:
                activation = Module.CastFinishAt(spell, 9.8d);
                break;
            case (uint)AID.GuidedMissile:
                CurrentBaits.Clear();
                break;
        }
    }
}
