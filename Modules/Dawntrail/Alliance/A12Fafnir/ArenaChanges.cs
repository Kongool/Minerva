// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A12Fafnir;

sealed class ArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeDonut donut = new(30f, 35f);
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.DarkMatterBlast && Bounds != A12Fafnir.DefaultBounds)
        {
            _aoe = [new(donut, Center, default, Module.CastFinishAt(spell, 1.1d))];
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x22 && state == 0x00020001u)
        {
            Bounds = A12Fafnir.DefaultBounds;
            _aoe = [];
        }
    }
}

sealed class DragonBreathArenaChange(ModuleBase module) : ModuleComponent(module)
{
    public override bool KeepOnPhaseChange => true;

    private Angle initialRot;
    private DateTime started;
    private static readonly Circle circle = new(A12Fafnir.ArenaCenter, 16f);

    public override void OnActorEAnim(Actor actor, uint state)
    {
        if (actor.OID == (uint)OID.FireVoidzone)
        {
            if (state == 0x00010002u) // outer arena starts to turn unsafe
            {
                Bounds = A12Fafnir.FireArena;
            }
            else if (state == 0x00040008u) // outer arena starts to turn safe again
            {
                initialRot = actor.Rotation;
                started = World.CurrentTime;
            }
        }
    }

    public override void Update()
    {
        if (started != default)
        {
            var time = (World.CurrentTime - started).TotalSeconds;
            var angle = initialRot - ((float)time * 30f).Degrees(); // 30° of the outer arena turn safe again per second
            if (time >= 12d)
            {
                started = default;
                Bounds = A12Fafnir.DefaultBounds;
                Center = A12Fafnir.ArenaCenter;
                return;
            }
            ArenaBoundsCustom refresh = new([circle, new Cone(A12Fafnir.ArenaCenter, 30f, angle, initialRot)]);
            Bounds = refresh;
            Center = refresh.Center;
        }
    }
}
