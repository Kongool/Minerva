// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS2StygimolochWarrior;

sealed class FocusedTremorLarge(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FocusedTremorAOELarge, new AOEShapeRect(20f, 10f), 2);
sealed class ForcefulStrike(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ForcefulStrike, new AOEShapeRect(44f, 24f));

// combined with flailing strike, first bait should be into first square
sealed class FocusedTremorSmall : Components.SimpleAOEs
{
    public FocusedTremorSmall(ModuleBase module) : base(module, (uint)AID.FocusedTremorAOESmall, new AOEShapeRect(10f, 5f), 1)
    {
        Color = Colors.SafeFromAOE;
        Risky = false;
    }

    public void Activate()
    {
        Color = Colors.AOE;
        Risky = true;
        MaxCasts = 3;
    }
}

sealed class FlailingStrikeBait(ModuleBase module) : Components.BaitAwayTethers(module, new AOEShapeCone(40f, 30f.Degrees()), (uint)TetherID.FlailingStrike);

sealed class FlailingStrike(ModuleBase module) : Components.GenericRotatingAOE(module)
{
    private static readonly AOEShapeCone _shape = new(60f, 30f.Degrees());

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.FlailingStrikeFirst)
        {
            Sequences.Add(new(_shape, spell.LocXZ, spell.Rotation, 60f.Degrees(), Module.CastFinishAt(spell), 1.6f, 6, 3));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.FlailingStrikeRest)
        {
            AdvanceSequence(0, World.CurrentTime);
        }
    }
}
