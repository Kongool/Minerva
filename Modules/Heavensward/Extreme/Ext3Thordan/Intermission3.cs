// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Extreme.Ex3Thordan;

sealed class HiemalStormSpread(ModuleBase module) : Components.UniformStackSpread(module, default, 6f)
{
    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.HiemalStorm)
        {
            AddSpread(actor, World.FutureTime(3d));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.HiemalStormAOE)
        {
            Spreads.Clear();
        }
    }
}

sealed class HiemalStormVoidzone(ModuleBase module) : Components.Voidzone(module, 6f, GetVoidzones)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.HiemalStorm);
        var count = enemies.Count;
        if (count == 0)
        {
            return [];
        }

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (z.EventState != 7)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }
}
sealed class SpiralPierce(ModuleBase module) : Components.BaitAwayTethers(module, new AOEShapeRect(50f, 6f), (uint)TetherID.SpiralPierce, (uint)AID.SpiralPierce);
sealed class DimensionalCollapse(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DimensionalCollapseAOE, 9f);

sealed class FaithUnmoving(ModuleBase module) : Components.GenericKnockback(module, (uint)AID.FaithUnmoving)
{
    private readonly DateTime activation = module.World.FutureTime(4.5d);
    private readonly Knockback[] _kb = GetKnockback(module);

    private static Knockback[] GetKnockback(ModuleBase module)
    {
        var grinnauxs = module.Enemies((uint)OID.SerGrinnaux);
        var grinnaux = grinnauxs.Count != 0 ? grinnauxs[0] : null;
        if (grinnaux != default)
        {
            return [new(grinnaux.Position, 16f, ignoreImmunes: true)];
        }
        return [];
    }

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor) => _kb;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (_kb.Length != 0)
        {
            ref var kb = ref _kb[0];
            hints.AddForbiddenZone(new SDKnockbackInCircleAwayFromOrigin(default, kb.Origin, 16f, 18f), activation);
        }
    }
}

sealed class CometCircle(ModuleBase module) : Components.Adds(module, (uint)OID.CometCircle);
sealed class MeteorCircle(ModuleBase module) : Components.Adds(module, (uint)OID.MeteorCircle);

sealed class HeavyImpact(ModuleBase module) : Components.ConcentricAOEs(module, _shapes)
{
    private static readonly Angle a135 = 135f.Degrees();
    private static readonly AOEShape[] _shapes = [new AOEShapeCone(6.5f, a135), new AOEShapeDonutSector(6.5f, 12.5f, a135), new AOEShapeDonutSector(12.5f, 18.5f, a135), new AOEShapeDonutSector(18.5f, 27.5f, a135)];

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.HeavyImpactAOE1)
        {
            AddSequence(spell.LocXZ, Module.CastFinishAt(spell), spell.Rotation);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        var order = spell.Action.ID switch
        {
            (uint)AID.HeavyImpactAOE1 => 0,
            (uint)AID.HeavyImpactAOE2 => 1,
            (uint)AID.HeavyImpactAOE3 => 2,
            (uint)AID.HeavyImpactAOE4 => 3,
            _ => -1
        };
        AdvanceSequence(order, spell.LocXZ, World.FutureTime(2d), spell.Rotation);
    }
}
