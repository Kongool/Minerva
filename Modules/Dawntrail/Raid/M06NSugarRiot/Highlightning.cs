// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M06NSugarRiot;

sealed class Highlightning(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCircle circle = new(21f);
    private AOEInstance[] _aoe = [];
    private WPos lastPosition;
    private bool active;
    private DateTime nextActivation;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.TempestPiece)
        {
            active = true;
            _aoe = [new(circle, actor.Position.Quantized(), default, World.FutureTime(6.5d))];
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Highlightning)
        {
            _aoe = [];
            if (++NumCasts == 3)
            {
                active = false;
                lastPosition = default;
                NumCasts = 0;
                return;
            }
            nextActivation = World.FutureTime(10d);
            lastPosition = caster.Position;
        }
    }

    public override void Update()
    {
        if (!active || _aoe.Length != 0)
        {
            return;
        }
        var tempest = Module.Enemies((uint)OID.TempestPiece)[0];
        var angle = (int)Angle.FromDirection(tempest.Position - lastPosition).Deg;
        if (angle == 0)
        {
            return; // cloud didn't start moving yet
        }

        WPos next = angle switch
        {
            -149 or -150 or -90 => new(87f, 92f),
            90 or 146 or 147 => new(115f, 92f),
            >= -35 and <= -32 or 28 or 29 => new(100f, 115f),
            _ => default
        };
        if (next != default)
        {
            _aoe = [new(circle, next.Quantized(), default, nextActivation)];
        }
    }
}
