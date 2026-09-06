// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M03NBruteBomber;

sealed class BarbarousBarrageTower(ModuleBase module) : Components.GenericTowers(module)
{
    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x01 && state == 0x00020004u)
        {
            WPos[] positions = [new(106f, 94f), new(94f, 106f), new(106f, 106f), new(94f, 94f)];
            for (var i = 0; i < 4; ++i)
            {
                Towers.Add(new(positions[i], 4f, 1, 1, default, World.FutureTime(10d)));
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.Explosion)
        {
            Towers.Clear();
        }
    }
}

sealed class BarbarousBarrageKnockback(ModuleBase module) : Components.GenericKnockback(module)
{
    private static readonly AOEShapeCircle circle = new(4f);
    private readonly BarbarousBarrageTower _tower = module.FindComponent<BarbarousBarrageTower>()!;
    private Knockback[] _kbs = [];

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor) => _kbs;

    public override void Update()
    {
        var towers = CollectionsMarshal.AsSpan(_tower.Towers);
        if (towers.Length == 0)
        {
            _kbs = [];
            return;
        }
        _kbs = new Knockback[4];
        for (var i = 0; i < 4; ++i)
        {
            ref var t = ref towers[i];
            _kbs[i] = new(t.Position, 22f, t.Activation, circle);
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var towers = CollectionsMarshal.AsSpan(_tower.Towers);
        var len = towers.Length;
        if (len == 0)
        {
            return;
        }
        ref var t0 = ref towers[0];
        if ((t0.Activation - World.CurrentTime).TotalSeconds < 5d)
        {
            for (var i = 0; i < len; ++i)
            {
                ref var t = ref towers[i];
                if (t.IsInside(actor))
                {
                    hints.ActionsToExecute.Push(ActionDefinitions.Armslength, actor, ActionQueue.Priority.High);
                    hints.ActionsToExecute.Push(ActionDefinitions.Surecast, actor, ActionQueue.Priority.High);
                    return;
                }
            }
        }
    }
}
