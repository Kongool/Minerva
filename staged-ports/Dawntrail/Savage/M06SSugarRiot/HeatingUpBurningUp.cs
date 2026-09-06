// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M06SSugarRiot;

sealed class HeatingBurningUp(ModuleBase module) : Components.GenericStackSpread(module, true, true)
{
    private readonly (Actor, DateTime)[] defamation = new (Actor, DateTime)[8];
    private (Actor, DateTime)? _stack;
    public BitMask DefamationStatus;
    public int NumCasts;

    public override void Update()
    {
        if (Spreads.Count < 2 || Stacks.Count == 0)
        {
            BitMask forbidden = default;
            var act = World.FutureTime(8d);
            for (var i = 0; i < 8; ++i)
            {
                if (!DefamationStatus[i])
                    continue;
                ref var def = ref defamation[i];
                if (def != default && Math.Max(0d, (def.Item2 - World.CurrentTime).TotalSeconds) < 8d)
                {
                    Spreads.Add(new(def.Item1, 15f, act));
                    forbidden[i] = true;
                    DefamationStatus[i] = false;
                }
            }
            if (_stack is (Actor, DateTime) stack && Math.Max(0d, (stack.Item2 - World.CurrentTime).TotalSeconds) < 8d)
            {
                _stack = null;
                Stacks.Add(new(stack.Item1, 6f, 6, 6, act, forbidden));
            }
        }
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.HeatingUp)
        {
            var slot = Raid.FindSlot(actor.InstanceID);
            DefamationStatus[slot] = true;
            defamation[slot] = (actor, status.ExpireAt);
        }
        else if (status.ID == (uint)SID.BurningUp)
        {
            _stack = (actor, status.ExpireAt);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.Brulee or (uint)AID.CrowdBrulee)
        {
            ++NumCasts;
            Stacks.Clear();
            Spreads.Clear();
        }
    }
}