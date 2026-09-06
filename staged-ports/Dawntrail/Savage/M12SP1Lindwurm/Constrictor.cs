// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M12SLindwurm;

sealed class Constrictor(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCircle _shape = new(13f);
    private readonly List<AOEInstance> _aoes = [];
    private int _rotationCount;
    private bool _act2Active;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnMapEffect(byte index, uint state)
    {
        // track rotations same as CruelCoil
        if (index is >= 0x02 and <= 0x09 && state == 0x00010001u && _act2Active)
        {
            _rotationCount++;
            if (_rotationCount >= 6 && _aoes.Count == 0)
            {
                _aoes.Add(new(_shape, Center, default, World.FutureTime(3f))); // show early
            }
        }
    }

    public override void Update()
    {
        var hasVessels = false;
        foreach (var actor in Module.Enemies((uint)OID.BloodVessel))
        {
            if (!actor.IsDestroyed)
            {
                hasVessels = true;
                break;
            }
        }

        if (hasVessels && !_act2Active)
        {
            _rotationCount = 0;
        }

        _act2Active = hasVessels;

        if (!_act2Active && _aoes.Count > 0)
            _aoes.Clear();
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Constrictor3)
        {
            _aoes.Clear();
            _aoes.Add(new(_shape, caster.Position, default, Module.CastFinishAt(spell)));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.Constrictor3)
            _aoes.Clear();
    }
}
