// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M07SBruteAbombinator;

sealed class QuarrySwamp(ModuleBase module) : Components.CastLineOfSightAOE(module, (uint)AID.QuarrySwamp, 60f)
{
    public override ReadOnlySpan<Actor> BlockerActors() => CollectionsMarshal.AsSpan(Module.Enemies((uint)OID.BloomingAbomination));

    public override void Update()
    {
        if (Casters.Count != 0 && BlockerActors().Length != 0)
        {
            Safezones.Clear();
            Refresh();
            AddSafezone(Module.CastFinishAt(Casters[0].CastInfo));
        }
    }
}

sealed class SporeSac(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SporeSac, 8f);
sealed class Pollen(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Pollen, 8f);
sealed class RootsOfEvil(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RootsOfEvil, 12f);
sealed class CrossingCrosswinds(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CrossingCrosswinds, new AOEShapeCross(50f, 5f));
sealed class WindingWildwinds(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WindingWildwinds, new AOEShapeDonut(5f, 60f));

sealed class AddInterruptHint(ModuleBase module) : ModuleComponent(module)
{
    private readonly List<Actor> castersDonut = [];
    private readonly List<Actor> castersCross = [];

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var countD = castersDonut.Count;
        for (var i = 0; i < countD; ++i)
        {
            var e = hints.FindEnemy(castersDonut[i]);
            e?.ShouldBeInterrupted = true;
        }
        if (countD != 0)
            return;
        var countC = castersCross.Count;
        for (var i = 0; i < countC; ++i)
        {
            var e = hints.FindEnemy(castersCross[i]);
            e?.ShouldBeInterrupted = true;
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        base.OnCastStarted(caster, spell);
        if (spell.Action.ID == (uint)AID.WindingWildwinds)
        {
            castersDonut.Add(caster);
        }
        else if (spell.Action.ID == (uint)AID.CrossingCrosswinds)
        {
            castersCross.Add(caster);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        base.OnCastFinished(caster, spell);
        if (spell.Action.ID == (uint)AID.WindingWildwinds)
        {
            castersDonut.Remove(caster);
        }
        else if (spell.Action.ID == (uint)AID.CrossingCrosswinds)
        {
            castersCross.Remove(caster);
        }
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (castersDonut.Count != 0)
        {
            hints.Add("Interrupt donut caster!");
            return;
        }

        if (castersCross.Count != 0)
        {
            hints.Add("Interrupt cross caster!");
        }
    }
}
