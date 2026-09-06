// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P8S1Hephaistos;

class RearingRampageSecond(ModuleBase module) : Components.CastCounter(module, (uint)AID.RearingRampageSecond);
class RearingRampageLast(ModuleBase module) : Components.CastCounter(module, (uint)AID.RearingRampageLast);

class UpliftStompDead : Components.UniformStackSpread
{
    public int NumUplifts;
    public int NumStomps;
    public int[] OrderPerSlot = new int[PartyState.MaxPartySize]; // 0 means not yet known

    public UpliftStompDead(ModuleBase module) : base(module, 6, 6, 2, 2, true)
    {
        AddSpreads(Raid.WithoutSlot(true, true, true));
    }

    public override void Update()
    {
        if (Spreads.Count == 0)
        {
            Stacks.Clear();
            if (Raid.WithoutSlot().Farthest(Module.PrimaryActor.Position) is var target && target != null)
                AddStack(target);
        }
        base.Update();
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (OrderPerSlot[slot] > 0)
        {
            hints.Add($"Bait order: {OrderPerSlot[slot]}", false);
        }

        if (Spreads.Count > 0)
        {
            // default implementation is fine during uplifts
            base.AddHints(slot, actor, hints);
        }
        else
        {
            // custom hints for baiting stomps
            var isBaiting = Stacks.Any(s => actor.Position.InCircle(s.Target.Position, s.Radius));
            var shouldBait = OrderPerSlot[slot] == NumStomps + 1;
            hints.Add(shouldBait ? "Bait jump!" : "Avoid jump!", isBaiting != shouldBait);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch ((AID)spell.Action.ID)
        {
            case AID.Uplift:
                Spreads.RemoveAll(s => s.Target.InstanceID == spell.MainTargetID);
                var slot = Raid.FindSlot(spell.MainTargetID);
                if (slot >= 0)
                {
                    OrderPerSlot[slot] = 4 - Spreads.Count / 2;
                }
                ++NumUplifts;
                break;
            case AID.StompDeadAOE:
                ++NumStomps;
                break;
        }
    }
}
