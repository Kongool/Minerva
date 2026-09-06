// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P6SHegemone;

// TODO: improve...
class AetheronecrosisPredation(ModuleBase module) : ModuleComponent(module)
{
    public int NumCastsAetheronecrosis;
    public int NumCastsDualPredation;
    private readonly int[] _orders = new int[PartyState.MaxPartySize];
    private BitMask _vulnSnake;
    private BitMask _vulnWing;

    public bool Active => (_vulnSnake | _vulnWing).Any();

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (_orders[slot] > 0)
            hints.Add($"Order: {_orders[slot]}, side: {(_vulnSnake[slot] ? "wing" : _vulnWing[slot] ? "snake" : "???")}", false);
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        switch ((SID)status.ID)
        {
            case SID.GlossalResistanceDown:
                _vulnSnake.Set(Raid.FindSlot(actor.InstanceID));
                break;
            case SID.ChelicResistanceDown:
                _vulnWing.Set(Raid.FindSlot(actor.InstanceID));
                break;
            case SID.Aetheronecrosis:
                var slot = Raid.FindSlot(actor.InstanceID);
                if (slot >= 0)
                {
                    _orders[slot] = (status.ExpireAt - World.CurrentTime).TotalSeconds switch
                    {
                        < 10 => 2,
                        < 14 => 3,
                        < 18 => 4,
                        _ => 1,
                    };
                }
                break;
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        switch ((SID)status.ID)
        {
            case SID.GlossalResistanceDown:
                _vulnSnake.Clear(Raid.FindSlot(actor.InstanceID));
                break;
            case SID.ChelicResistanceDown:
                _vulnWing.Clear(Raid.FindSlot(actor.InstanceID));
                break;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch ((AID)spell.Action.ID)
        {
            case AID.Aetheronecrosis:
                ++NumCastsAetheronecrosis;
                break;
            case AID.GlossalPredation:
                ++NumCastsDualPredation;
                break;
        }
    }
}
