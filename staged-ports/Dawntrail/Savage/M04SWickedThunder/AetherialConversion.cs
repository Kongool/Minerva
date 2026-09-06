// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M04SWickedThunder;

sealed class AetherialConversion(ModuleBase module) : Components.CastCounter(module, default)
{
    public enum Mechanic { None, AOE, Knockback }

    public Mechanic CurMechanic;
    public float FirstOffsetX;

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (CurMechanic != default)
            hints.Add($"{CurMechanic} {(FirstOffsetX < 0 ? "L->R" : "R->L")}");
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        var (mechanic, firstOffset) = spell.Action.ID switch
        {
            (uint)AID.AetherialConversionHitLR => (Mechanic.AOE, -10f),
            (uint)AID.AetherialConversionKnockbackLR => (Mechanic.Knockback, -10f),
            (uint)AID.AetherialConversionHitRL => (Mechanic.AOE, +10f),
            (uint)AID.AetherialConversionKnockbackRL => (Mechanic.Knockback, +10f),
            _ => default
        };
        if (mechanic != default)
        {
            CurMechanic = mechanic;
            FirstOffsetX = firstOffset;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.TailThrust or (uint)AID.SwitchOfTides)
            ++NumCasts;
    }
}

sealed class AetherialConversionTailThrust(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.TailThrust)
{
    private readonly AetherialConversion? _comp = module.FindComponent<AetherialConversion>();

    private static readonly AOEShapeCircle _shape = new(18f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (_comp?.CurMechanic == AetherialConversion.Mechanic.AOE && _comp.NumCasts < 2)
            return new AOEInstance[1] { new(_shape, (Center + new WDir(_comp.NumCasts == 0 ? _comp.FirstOffsetX : -_comp.FirstOffsetX, default)).Quantized(), default) };
        return [];
    }
}

sealed class AetherialConversionSwitchOfTides(ModuleBase module) : Components.GenericKnockback(module, (uint)AID.SwitchOfTides)
{
    private readonly AetherialConversion? _comp = module.FindComponent<AetherialConversion>();

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor)
    {
        if (_comp?.CurMechanic == AetherialConversion.Mechanic.Knockback && _comp.NumCasts < 2)
            return new Knockback[1] { new(Center + new WDir(_comp.NumCasts == 0 ? _comp.FirstOffsetX : -_comp.FirstOffsetX, 0), 25f, ignoreImmunes: true) };
        return [];
    }
}
