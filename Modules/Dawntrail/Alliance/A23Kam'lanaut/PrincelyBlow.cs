// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A23Kamlanaut;

sealed class PrincelyBlow(ModuleBase module) : Components.BaitAwayIcon(module, new AOEShapeRect(60f, 5f), (uint)IconID.PrincelyBlow, (uint)AID.PrincelyBlow, 8.3d, tankbuster: true);

sealed class PrincelyBlowKB(ModuleBase module) : Components.GenericKnockback(module, stopAtWall: true)
{
    private readonly ShieldBash shieldBash = module.FindComponent<ShieldBash>()!;
    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor) => targets[slot] ? _kb : [];
    private Knockback[] _kb = [];
    private BitMask targets;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.PrincelyBlow)
        {
            if (targets == default)
            {
                _kb = [new(new WPos(-200f, 150f).Quantized(), 30f, World.FutureTime(8.3d))];
            }
            targets.Set(Raid.FindSlot(targetID));

            if (Bounds.Radius != 29.5f)
            {
                StopAtWall = false;
                StopAfterWall = true;
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.PrincelyBlow)
        {
            targets = default;
            _kb = [];
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (targets[slot] && Bounds.Radius > 30f)
        {
            ref readonly var kb = ref _kb[0];
            var act = kb.Activation;
            if (!IsImmune(slot, act))
            {
                // if possible press Arms Length so there are more places to go with the tankbuster
                hints.ActionsToExecute.Push(ActionDefinitions.Armslength, actor, ActionQueue.Priority.High);
                if (!shieldBash.PolygonInit)
                {
                    shieldBash.Polygon = Bounds.Shape.Offset(-1f); // pretend polygon is 1y smaller than real for less suspect knockbacks
                    shieldBash.PolygonInit = true;
                }

                hints.AddForbiddenZone(new SDKnockbackInComplexPolygonAwayFromOrigin(Center, kb.Origin, 30f, shieldBash.Polygon), act);
            }
        }
    }
}
