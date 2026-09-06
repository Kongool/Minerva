// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M03SBruteBomber;

sealed class BombarianSpecial(ModuleBase module) : Components.UniformStackSpread(module, 5f, 5f, 2, 2)
{
    public enum Mechanic { None, Spread, Pairs }

    public Mechanic CurMechanic;

    public void Show(float delay)
    {
        switch (CurMechanic)
        {
            case Mechanic.Spread:
                AddSpreads(Raid.WithoutSlot(true, true, true), World.FutureTime(delay));
                break;
            case Mechanic.Pairs:
                // TODO: can target any role
                AddStacks(Raid.WithoutSlot(true, true, true).Where(p => p.Class.IsSupport()), World.FutureTime(delay));
                break;
        }
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (CurMechanic != Mechanic.None)
            hints.Add(CurMechanic.ToString());
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        var mechanic = spell.Action.ID switch
        {
            (uint)AID.OctoboomBombarianSpecial => Mechanic.Spread,
            (uint)AID.QuadroboomBombarianSpecial => Mechanic.Pairs,
            _ => Mechanic.None
        };
        if (mechanic != Mechanic.None)
            CurMechanic = mechanic;
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.BombariboomSpread or (uint)AID.BombariboomPair)
        {
            Spreads.Clear();
            Stacks.Clear();
            CurMechanic = Mechanic.None;
        }
    }
}

sealed class BombarianSpecialRaidwide(ModuleBase module) : Components.CastCounterMulti(module, [(uint)AID.BombarianSpecialRaidwide1,
(uint)AID.BombarianSpecialRaidwide2, (uint)AID.BombarianSpecialRaidwide3, (uint)AID.BombarianSpecialRaidwide4,
(uint)AID.BombarianSpecialRaidwide5, (uint)AID.BombarianSpecialRaidwide6, (uint)AID.SpecialBombarianSpecialRaidwide1,
(uint)AID.SpecialBombarianSpecialRaidwide2, (uint)AID.SpecialBombarianSpecialRaidwide3,
(uint)AID.SpecialBombarianSpecialRaidwide4, (uint)AID.SpecialBombarianSpecialRaidwide5, (uint)AID.SpecialBombarianSpecialRaidwide6]);

abstract class SpecialOut(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, 10);
sealed class BombarianSpecialOut(ModuleBase module) : SpecialOut(module, (uint)AID.BombarianSpecialOut);
sealed class SpecialBombarianSpecialOut(ModuleBase module) : SpecialOut(module, (uint)AID.SpecialBombarianSpecialOut);

abstract class SpecialIn(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeDonut(6, 40));
sealed class BombarianSpecialIn(ModuleBase module) : SpecialIn(module, (uint)AID.BombarianSpecialIn);
sealed class SpecialBombarianSpecialIn(ModuleBase module) : SpecialIn(module, (uint)AID.SpecialBombarianSpecialIn);

sealed class BombarianSpecialAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BombarianSpecialAOE, 8);
sealed class BombarianSpecialKnockback(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.BombarianSpecialKnockback, 10);
