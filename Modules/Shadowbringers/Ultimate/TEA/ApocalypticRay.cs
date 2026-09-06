// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Ultimate.TEA;

[SkipLocalsInit]
abstract class ApocalypticRay(ModuleBase module, bool faceCenter) : Components.GenericAOEs(module)
{
    public Actor? Source;
    private readonly bool _faceCenter = faceCenter;
    private Angle _rotation;
    private DateTime _activation;

    private readonly AOEShapeCone _shape = new(25.5f, 45f.Degrees());

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (Source != null)
            return new AOEInstance[1] { new(_shape, Source.Position, _rotation, _activation) };
        return [];
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.ApocalypticRay:
                Source = caster;
                if (_faceCenter)
                {
                    _rotation = Angle.FromDirection(Center - caster.Position);
                }
                else
                {
                    var target = World.Actors.Find(caster.TargetID);
                    _rotation = target != null ? Angle.FromDirection(target.Position - caster.Position) : caster.Rotation; // this seems to be how it is baited
                }
                _activation = World.FutureTime(0.6d);
                break;
            case (uint)AID.ApocalypticRayAOE:
                ++NumCasts;
                _activation = World.FutureTime(1.1d);
                _rotation = caster.Rotation; // fix possible mistake
                break;
        }
    }
}

sealed class P2ApocalypticRay(ModuleBase module) : ApocalypticRay(module, false);
sealed class P3ApocalypticRay(ModuleBase module) : ApocalypticRay(module, true);
