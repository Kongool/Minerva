// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M04SWickedThunder;

sealed class ElectrifyingWitchHuntBurst(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ElectrifyingWitchHuntBurst, new AOEShapeRect(40f, 8f));

sealed class ElectrifyingWitchHuntSpread(ModuleBase module) : Components.UniformStackSpread(module, default, 6f)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.ElectrifyingWitchHunt)
            AddSpreads(Raid.WithoutSlot(true, true, true), Module.CastFinishAt(spell, 0.1f));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.ElectrifyingWitchHuntAOE)
            Spreads.Clear();
    }
}

sealed class ElectrifyingWitchHuntResolve(ModuleBase module) : Components.GenericStackSpread(module)
{
    public enum Mechanic { None, Near, Far }

    public Mechanic CurMechanic;
    public BitMask ForbidBait;
    private BitMask _baits;
    private DateTime _activation;

    public override void Update()
    {
        _baits = CurMechanic switch
        {
            Mechanic.Near => Raid.WithSlot(false, true, true).SortedByRange(Center).Take(4).Mask(),
            Mechanic.Far => Raid.WithSlot(false, true, true).SortedByRange(Center).TakeLast(4).Mask(),
            _ => default
        };

        Spreads.Clear();
        foreach (var (i, p) in Raid.WithSlot(true, true, true))
        {
            if (ForbidBait[i])
                Spreads.Add(new(p, 5, _activation));
            if (_baits[i])
                Spreads.Add(new(p, 6, _activation));
        }

        base.Update();
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (CurMechanic != Mechanic.None)
        {
            var wantBait = !ForbidBait[slot];
            var baitNear = CurMechanic == Mechanic.Near;
            hints.Add(wantBait == baitNear ? "Go near!" : "Go far!", _baits[slot] == ForbidBait[slot]);
        }
        base.AddHints(slot, actor, hints);
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        switch (status.ID)
        {
            case (uint)SID.ForkedLightning:
                ForbidBait.Set(Raid.FindSlot(actor.InstanceID));
                break;
            case (uint)SID.Marker:
                CurMechanic = status.Extra switch
                {
                    0x2F6 => Mechanic.Near,
                    0x2F7 => Mechanic.Far,
                    _ => Mechanic.None
                };
                _activation = status.ExpireAt;
                break;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.ElectrifyingWitchHuntBait:
                CurMechanic = Mechanic.None;
                break;
            case (uint)AID.ForkedLightning:
                ForbidBait = default;
                break;
        }
    }
}
