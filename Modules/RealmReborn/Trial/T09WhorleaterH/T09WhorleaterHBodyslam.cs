// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Trial.T09WhorleaterH;

sealed class BodySlamKB(ModuleBase module) : Components.GenericKnockback(module, stopAtWall: true)
{
    private Knockback[] _kb = [];
    private float LeviathanZ;
    private readonly Hydroshot _aoe1 = module.FindComponent<Hydroshot>()!;
    private readonly Dreadstorm _aoe2 = module.FindComponent<Dreadstorm>()!;

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor) => _kb;

    public override void Update()
    {
        var z = Module.PrimaryActor.PosRot.Z;
        if (LeviathanZ == default)
        {
            LeviathanZ = z;
        }
        if (z != LeviathanZ && z != default)
        {
            LeviathanZ = z;
            _kb = [new(Center, 25f, World.FutureTime(4.8d), direction: z <= 0f ? 180f.Degrees() : default, kind: Kind.DirForward)];
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.BodySlamNorth or (uint)AID.BodySlamSouth)
        {
            _kb = [];
        }
    }

    public override bool DestinationUnsafe(int slot, Actor actor, WPos pos)
    {
        var aoes1 = _aoe1.ActiveAOEs(slot, actor);
        var len1 = aoes1.Length;
        for (var i = 0; i < len1; ++i)
        {
            ref readonly var aoe = ref aoes1[i];
            if (aoe.Check(pos))
            {
                return true;
            }
        }
        var aoes2 = _aoe2.ActiveAOEs(slot, actor);
        var len2 = aoes2.Length;
        for (var i = 0; i < len2; ++i)
        {
            ref readonly var aoe = ref aoes2[i];
            if (aoe.Check(pos))
            {
                return true;
            }
        }
        return false;
    }
}

sealed class BodySlamAOE(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];
    private float LeviathanZ;
    private static readonly AOEShapeRect rect = new(34.5f, 5);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void Update()
    {
        var primary = Module.PrimaryActor;
        if (LeviathanZ == default)
        {
            LeviathanZ = primary.PosRot.Z;
        }
        if (primary.PosRot.Z is var Z && Z != LeviathanZ && Z != default)
        {
            LeviathanZ = Z;
            _aoe = [new(rect, primary.Position, primary.Rotation, World.FutureTime(2.6d))];
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.BodySlamRectAOE)
        {
            _aoe = [];
        }
    }
}
