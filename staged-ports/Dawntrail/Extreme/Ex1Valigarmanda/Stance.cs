// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex1Valigarmanda;

sealed class Stance(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];

    private readonly AOEShapeCone _shapeCone = new(50f, 40f.Degrees());
    private readonly AOEShapeCone _shapeOut = new(24f, 90f.Degrees());
    private readonly AOEShapeDonut _shapeIn = new(8f, 30f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        AOEShape? shape = spell.Action.ID switch
        {
            (uint)AID.SusurrantBreathAOE => _shapeCone,
            (uint)AID.SlitheringStrikeAOE => _shapeOut,
            (uint)AID.StranglingCoilAOE => _shapeIn,
            _ => null
        };
        if (shape != null)
        {
            var rot = spell.Rotation;
            var pos = spell.LocXZ;
            _aoe = [new(shape, pos, rot, Module.CastFinishAt(spell), shapeDistance: shape.Distance(pos, rot))];
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.SusurrantBreathAOE or (uint)AID.SlitheringStrikeAOE or (uint)AID.StranglingCoilAOE)
        {
            ++NumCasts;
        }
    }
}

sealed class CharringCataclysm(ModuleBase module) : Components.UniformStackSpread(module, 4f, default, 2, 2)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.SusurrantBreathAOE or (uint)AID.SlitheringStrikeAOE or (uint)AID.StranglingCoilAOE)
        {
            // note: dd vs supports is random, select supports arbitrarily
            AddStacks(Module.Raid.WithoutSlot(true, true, true).Where(p => p.Class.IsSupport()), Module.CastFinishAt(spell, 0.7f));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.CharringCataclysm)
            Stacks.Clear();
    }
}

sealed class ChillingCataclysm(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.ChillingCataclysmAOE)
{
    public readonly List<AOEInstance> AOEs = [];

    private readonly AOEShapeCross rect = new(40f, 2.5f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(AOEs);

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.ChillingCataclysmArcaneSphere)
        {
            var act = World.FutureTime(5.6d);
            var pos = actor.Position.Quantized();
            var rot1 = Angle.AnglesCardinals[1];
            var rot2 = Angle.AnglesIntercardinals[1];
            AOEs.Add(new(rect, pos, rot1, act, shapeDistance: rect.Distance(pos, rot1)));
            AOEs.Add(new(rect, pos, rot2, act, shapeDistance: rect.Distance(pos, rot2)));
        }
    }
}

sealed class CracklingCataclysm(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CracklingCataclysm, 6f);
