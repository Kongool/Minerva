// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C03AAI.C032Lala;

// TODO: we could detect aoe positions slightly earlier, when golems spawn
abstract class ConstructiveFigure(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeRect(50f, 4f));
sealed class NConstructiveFigure(ModuleBase module) : ConstructiveFigure(module, (uint)AID.NAero);
sealed class SConstructiveFigure(ModuleBase module) : ConstructiveFigure(module, (uint)AID.SAero);

sealed class ArcanePoint(ModuleBase module) : ModuleComponent(module)
{
    public int NumCasts;
    private readonly ArcanePlot? _plot = module.FindComponent<ArcanePlot>();

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (NumCasts > 0)
            return;
        var spot = CurrentSafeSpot(actor.Position);
        if (spot != null && Raid.WithoutSlot(false, true, true).Exclude(actor).Any(p => CurrentSafeSpot(p.Position) == spot))
            hints.Add("Spread on different squares!");
    }

    public override PlayerPriority CalcPriority(int pcSlot, Actor pc, int playerSlot, Actor player, ref uint customColor)
    {
        return PlayerPriority.Interesting;
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        if (NumCasts > 0)
            return;
        var spot = CurrentSafeSpot(pc.Position);
        if (spot != null)
            ArcaneArrayPlot.Shape.Draw(Arena, spot.Value, default, Colors.SafeFromAOE);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.NPowerfulLight or (uint)AID.SPowerfulLight)
        {
            ++NumCasts;
            _plot?.AddAOE(caster.Position, default);
        }
    }

    public WPos? CurrentSafeSpot(WPos pos)
    {
        if (_plot == null)
            return null;
        var index = _plot.SafeZoneCenters.FindIndex(p => ArcaneArrayPlot.Shape.Check(pos, p, default));
        return index >= 0 ? _plot.SafeZoneCenters[index] : null;
    }
}

abstract class ExplosiveTheorem(ModuleBase module, uint aid) : Components.SpreadFromCastTargets(module, aid, 8f);
sealed class NExplosiveTheorem(ModuleBase module) : ExplosiveTheorem(module, (uint)AID.NExplosiveTheoremAOE);
sealed class SExplosiveTheorem(ModuleBase module) : ExplosiveTheorem(module, (uint)AID.SExplosiveTheoremAOE);

abstract class TelluricTheorem(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, 8f);
sealed class NTelluricTheorem(ModuleBase module) : TelluricTheorem(module, (uint)AID.NTelluricTheorem);
sealed class STelluricTheorem(ModuleBase module) : TelluricTheorem(module, (uint)AID.STelluricTheorem);
