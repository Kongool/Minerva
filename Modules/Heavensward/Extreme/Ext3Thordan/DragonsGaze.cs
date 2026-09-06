// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Extreme.Ex3Thordan;

sealed class DragonsGaze(ModuleBase module) : Components.GenericGaze(module)
{
    private readonly List<Eye> _eyes = [];
    private WPos _posHint;

    public override ReadOnlySpan<Eye> ActiveEyes(int slot, Actor actor) => CollectionsMarshal.AsSpan(_eyes);

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        base.DrawArenaForeground(pcSlot, pc);
        if (_posHint != default)
        {
            Arena.ZoneCircleOutline(_posHint, 1f, Colors.Safe);
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.DragonsGaze or (uint)AID.DragonsGlory)
        {
            _eyes.Add(new(spell.LocXZ, Module.CastFinishAt(spell)));
            _posHint = default;
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.DragonsGaze or (uint)AID.DragonsGlory)
        {
            _eyes.RemoveAt(0);
        }
    }

    public override void OnActorEAnim(Actor actor, uint state)
    {
        if (state == 0x00040008u && actor.OID is >= (uint)OID.DragonEyeN and <= (uint)OID.DragonEyeNW)
        {
            var index = actor.OID - (uint)OID.DragonEyeN; // 0 = N, then CW
            _posHint = Center + 19f * (180f - (int)index * 45f).Degrees().ToDirection();
        }
    }
}
