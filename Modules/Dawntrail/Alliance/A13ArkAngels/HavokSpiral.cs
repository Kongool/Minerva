// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A13ArkAngels;

sealed class HavokSpiral(ModuleBase module) : Components.GenericRotatingAOE(module)
{
    private Angle _increment;
    private DateTime _activation;
    private readonly List<Angle> _rotation = [];

    private static readonly AOEShapeCone _shape = new(30f, 15f.Degrees());

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        _increment = iconID switch
        {
            (uint)IconID.RotateCW => -30f.Degrees(),
            (uint)IconID.RotateCCW => 30f.Degrees(),
            _ => default
        };
        _activation = World.FutureTime(5.5d);
        InitIfReady();
    }

    private void InitIfReady()
    {
        if (_rotation.Count == 3 && _increment != default)
        {
            for (var i = 0; i < 3; ++i)
                Sequences.Add(new(_shape, Center.Quantized(), _rotation[i], _increment, _activation, 1.2d, 8));
            _rotation.Clear();
            _increment = default;
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.HavocSpiralFirst)
        {
            _rotation.Add(spell.Rotation);
            InitIfReady();
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.HavocSpiralFirst or (uint)AID.HavocSpiralRest)
            AdvanceSequence(caster.Position, spell.Rotation, World.CurrentTime);
    }
}

sealed class SpiralFinish(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.SpiralFinishAOE, 16f)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
        {
            ref readonly var c = ref Casters.Ref(0);
            hints.AddForbiddenZone(new SDInvertedCircle(c.Origin, 9f), c.Activation);
        }
    }
}
