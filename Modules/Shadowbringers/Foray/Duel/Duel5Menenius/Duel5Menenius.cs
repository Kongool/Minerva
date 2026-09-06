// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

using Minerva.Dawntrail.Alliance.A12Fafnir;

namespace Minerva.Shadowbringers.Foray.Duel.Duel5Menenius;

sealed class SpiralScourge(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.SpiralScourge, "Use Manawall, Excellence, or Invuln.");
sealed class CallousCrossfire(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.CallousCrossfire, "Use Light Curtain / Reflect.");

sealed class ReactiveMunition(ModuleBase module) : Components.StayMove(module)
{
    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.AccelerationBomb)
        {
            if (Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
                PlayerStates[slot] = new(Requirement.Stay, status.ExpireAt);
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.AccelerationBomb)
        {
            if (Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
                PlayerStates[slot] = default;
        }
    }
}

sealed class SenseWeakness(ModuleBase module) : Components.StayMove(module)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.SenseWeakness)
        {
            if (Raid.FindSlot(caster.TargetID) is var slot && slot >= 0)
                PlayerStates[slot] = new(Requirement.Move, Module.CastFinishAt(spell));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.SenseWeakness)
        {
            if (Raid.FindSlot(caster.TargetID) is var slot && slot >= 0)
                PlayerStates[slot] = default;
        }
    }
}

sealed class MagitekImpetus(ModuleBase module) : Components.StatusDrivenForcedMarch(module, 3f, (uint)SID.ForwardMarch, (uint)SID.AboutFace, (uint)SID.LeftFace, (uint)SID.RightFace, activationLimit: 1);
sealed class ProactiveMunition(ModuleBase module) : Components.StandardChasingAOEs(module, 6f, (uint)AID.ProactiveMunitionTrackingStart, (uint)AID.ProactiveMunitionTrackingMove, 6, 1d, 5, true, (uint)IconID.ProactiveMunition);

[ModuleInfo(CFCID = 778u, NameID = 23u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "SourP (ported from BMR)")] // bnpcname=9695
public sealed class Duel5Menenius(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-810f, 520f), new ArenaBoundsSquare(20f))
{
    protected override bool CheckPull() => base.CheckPull() && Raid.Player()!.Position.InSquare(Center, 20f);
}
