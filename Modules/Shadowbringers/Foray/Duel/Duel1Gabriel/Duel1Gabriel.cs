// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.Duel.Duel1Gabriel;

sealed class MagitekMissile(ModuleBase module) : Components.Voidzone(module, 1.3f, GetMissiles, 5f)
{
    private static List<Actor> GetMissiles(ModuleBase module) => module.Enemies((uint)OID.MagitekMissile);
}

sealed class MissileLauncher(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MissileLauncher, 4f);

sealed class InfraredHomingMissile(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.InfraredHomingMissile, 15f);
sealed class InfraredHomingMissileBait(ModuleBase module) : Components.GenericBaitAway(module, centerAtTarget: true)
{
    private static readonly AOEShapeCircle circle = new(15f);

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.Prey)
            CurrentBaits.Add(new(Module.PrimaryActor, actor, circle, status.ExpireAt));
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.InfraredHomingMissile)
            CurrentBaits.Clear();
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (CurrentBaits.Count != 0)
            hints.Add("Bait away!");
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (CurrentBaits.Count != 0)
            hints.AddForbiddenZone(new SDCircle(Center, 13.5f));
    }
}

sealed class DynamicSensoryJammer(ModuleBase module) : Components.StayMove(module, 3f)
{
    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.ExtremeCaution && Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
            PlayerStates[slot] = new(Requirement.Stay, status.ExpireAt);
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.ExtremeCaution && Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
            PlayerStates[slot] = default;
    }
}

[ModuleInfo(Group = ModuleGroup.BozjaDuel, GroupID = 735u, CFCID = 735u, NameID = 4u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class Gabriel(WorldState ws, Actor primary) : ModuleBase(ws, primary, new WPos(631f, 687f).Quantized(), new ArenaBoundsCircle(15f))
{
    protected override bool CheckPull() => base.CheckPull() && Raid.Player()!.Position.InCircle(Center, 15f);
}
