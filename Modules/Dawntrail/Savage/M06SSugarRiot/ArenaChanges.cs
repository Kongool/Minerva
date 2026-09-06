// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M06SSugarRiot;

sealed class ArenaChanges(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly M06SSugarRiot bossmod = (M06SSugarRiot)module;
    private AOEShapeCustom? shape;
    private bool _risky = true;
    private AOEInstance[] _aoe = [];
    private bool active;
    public bool DangerousRiver;
    public bool DangerousLava;
    private readonly PolygonCustom[] combinedLava = [new([new(92.707f, 99.682f), new(88.088f, 101.596f), new(90.448f, 107.294f), new(95.068f, 105.382f)]),
     new([new(103.371f, 106.475f), new(107.338f, 109.518f), new(111.093f, 104.625f), new(107.127f, 101.58f)]),
     new([new(103.923f, 93.843f), new(104.574f, 88.886f), new(98.459f, 88.081f), new(97.804f, 93.038f)])];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x04)
        {
            switch (state)
            {
                case 0x00020001u:
                    active = true;
                    break;
                case 0x00800040u:
                    AddAOE(shape ??= GetRiverAOE(Center));
                    _risky = true;
                    break;
                case 0x02000100u:
                    AddAOE(new AOEShapeCustom(Center, combinedLava));
                    break;
                case 0x00200010u:
                    SetArena(new ArenaBoundsCustom([new Square(Center, 20f)], bossmod.GetCombinedRiver()));
                    DangerousRiver = true;
                    active = false;
                    break;
            }
        }
        else if (index == 0x05)
        {
            switch (state)
            {
                case 0x00020001u:
                    SetArena(new ArenaBoundsCustom([new Square(Center, 20f)], [.. bossmod.GetCombinedRiver(), .. combinedLava]));
                    DangerousLava = true;
                    break;
                case 0x00080004u:
                    Bounds = new ArenaBoundsSquare(20f);
                    break;
            }
        }
        void AddAOE(AOEShapeCustom shape)
        => _aoe = [new(shape, Center, default, World.FutureTime(7.1d))];
        void SetArena(ArenaBoundsCustom bounds)
        {
            Bounds = bounds;
            _aoe = [];
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.DoubleStyle3:
                AddAOE(shape ??= GetRiverAOE(Center), Colors.SafeFromAOE, true);
                _risky = false;
                break;
            case (uint)AID.DoubleStyle5:
                AddAOE(shape ??= GetRiverAOE(Center));
                break;
        }

        void AddAOE(AOEShapeCustom shape, uint color = default, bool invert = false)
        {
            var center = Center;
            _aoe = [new(shape, center, default, Module.CastFinishAt(spell, 4.2d), color, shapeDistance: shape.Distance(center, default))];
            ref var aoe = ref _aoe[0];
            aoe.Shape.InvertForbiddenZone = invert;
        }
    }

    AOEShapeCustom GetRiverAOE(WPos center)
    {
        return new AOEShapeCustom(center, bossmod.GetCombinedRiver());
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.TasteOfFire or (uint)AID.TasteOfThunderSpread)
        {
            _aoe = [];
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (!active || _aoe.Length == 0)
        {
            return;
        }
        ref var aoe = ref _aoe[0];
        var isInside = aoe.Check(actor.Position);
        if (!_risky)
        {
            hints.Add("Be inside river!", !isInside);
            return;
        }
        if (isInside)
        {
            hints.Add("GTFO from river!");
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        if (!active)
        {
            return;
        }
        var pos = actor.Position;
        if (actor.PrevPosition != pos)
        {
            hints.WantJump = bossmod.IntersectJumpEdge(pos, (pos - actor.PrevPosition).Normalized(), 1f);
        }
    }
}
