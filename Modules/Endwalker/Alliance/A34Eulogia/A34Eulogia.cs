// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A34Eulogia;

sealed class ArenaChanges(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly AOEShapeDonut transitionSmallerBounds = new(30f, 35f);
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x1B)
        {
            if (state == 0x00080004u)
            {
                Bounds = new ArenaBoundsCircle(35f);
            }
            else if (state == 0x00100001u)
            {
                Bounds = new ArenaBoundsCircle(30f);
            }
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        var id = spell.Action.ID;
        if (id == (uint)AID.Hieroglyphika)
        {
            var center = Center;
            AddAOE(new AOEShapeCustom(center, [new Square(center, 30f)], [new Square(center, 24f)]), center);
        }
        else if (id == (uint)AID.Whorl)
        {
            AddAOE(transitionSmallerBounds, Center);
        }
        void AddAOE(AOEShape shape, WPos center)
        {
            _aoe = [new(shape, center, default, Module.CastFinishAt(spell), shapeDistance: transitionSmallerBounds.Distance(center, default))];
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        var id = spell.Action.ID;
        if (id == (uint)AID.Hieroglyphika)
        {
            Bounds = new ArenaBoundsSquare(24f);
            _aoe = [];
        }
        else if (id == (uint)AID.Whorl)
        {
            Bounds = new ArenaBoundsCircle(30f);
            _aoe = [];
        }
    }
}

sealed class Sunbeam(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.SunbeamAOE, 6f);
sealed class DestructiveBolt(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.DestructiveBoltAOE, 6f, 8);

sealed class HandOfTheDestroyer(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.HandOfTheDestroyerWrathAOE, (uint)AID.HandOfTheDestroyerJudgmentAOE], new AOEShapeRect(90f, 20f));

sealed class SoaringMinuet(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SoaringMinuet, new AOEShapeCone(40f, 135f.Degrees()));
sealed class EudaimonEorzea(ModuleBase module) : Components.CastCounter(module, (uint)AID.EudaimonEorzeaAOE);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 962u, CFCID = 962u, NameID = 11301u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus, LTS (ported from BMR)")]
public sealed class A34Eulogia(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(945f, -945f), new ArenaBoundsCircle(35f));
