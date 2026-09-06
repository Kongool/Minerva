// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V2MountRokkon.V25Enenra;

sealed class IntoTheFire(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeRect rect = new(50f, 25f);
    private readonly List<AOEInstance> _aoes = [];
    private const float offset = 15.556349f;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnActorEAnim(Actor actor, uint state)
    {
        if (state == 0x00010002u)
        {
            var rot = actor.Rotation;
            var (positionOffset, rotation) = actor.OID switch
            {
                (uint)OID.SmokeVisual1 => (offset * (rot - 45f.Degrees()).ToDirection(), rot + 90f.Degrees()),
                (uint)OID.SmokeVisual2 => (offset * (rot + 45f.Degrees()).ToDirection(), rot - 90f.Degrees()),
                (uint)OID.SmokeVisual3 => (22f * rot.ToDirection(), rot + 180f.Degrees()),
                _ => ((WDir?)null, (Angle?)null)
            };

            if (positionOffset is WDir o && rotation is Angle a)
            {
                var correctedPosition = RoundPosition(actor.Position + o).Quantized();
                _aoes.Add(new(rect, correctedPosition, a, World.FutureTime(16.6d)));
            }

            static WPos RoundPosition(WPos position) => new(MathF.Round(position.X), MathF.Round(position.Z));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (_aoes.Count != 0 && spell.Action.ID == (uint)AID.IntoTheFire)
        {
            _aoes.RemoveAt(0);
        }
    }
}
