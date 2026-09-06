// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRN3QueensGuard;

sealed class TurretsTour(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShapeRect rect = new(50f, 3f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.TurretsTourFirst)
        {
            var toTarget = spell.LocXZ - caster.Position;
            var act = Module.CastFinishAt(spell);
            _aoes.Add(new(new AOEShapeRect(toTarget.Length(), 3f), caster.Position.Quantized(), Angle.FromDirection(toTarget), act, actorID: caster.InstanceID));

            var turrets = Module.Enemies((uint)OID.AutomaticTurret);
            var count = turrets.Count;
            if (_aoes.Count != count / 2)
                return;
            for (var i = 0; i < count; ++i)
            {
                var t = turrets[i];
                var minDistance = float.MaxValue;
                Actor? closestTarget = null;

                for (var j = 0; j < count; ++j)
                {
                    if (i == j)
                        continue; // Exclude the current turret itself
                    var potentialTarget = turrets[j];

                    if (potentialTarget.Position.InRect(t.Position, t.Rotation, 50f, default, 0.1f)) // full half width of 3 gives false positives
                    {
                        var distance = (potentialTarget.Position - t.Position).LengthSq();
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestTarget = potentialTarget;
                        }
                    }
                }

                var shape = closestTarget != null ? new AOEShapeRect(MathF.Sqrt(minDistance), 3f) : rect;
                _aoes.Add(new(shape, t.Position, t.Rotation, act, actorID: t.InstanceID));
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.TurretsTourFirst or (uint)AID.TurretsTourRest1 or (uint)AID.TurretsTourRest2)
        {
            var count = _aoes.Count;
            for (var i = 0; i < count; ++i)
            {
                if (_aoes[i].ActorID == caster.InstanceID)
                {
                    _aoes.RemoveAt(i);
                    return;
                }
            }
        }
    }

    public override void OnActorUntargetable(Actor actor)
    {
        if (actor.OID == (uint)OID.QueensGunner)
        {
            _aoes.Clear();
        }
    }
}
