// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

using Minerva.Components;
namespace Minerva.Dawntrail.Savage.M11STheTyrant;

sealed class RawSteelTrophyAxe(ModuleBase module) : GenericStackSpread(module, false)
{
    public int NumCasts;
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.RawSteelTrophyAxe)
        {
            var activation = Module.CastFinishAt(spell, 0.1d);
            var party = Module.World.Party.WithoutSlot();
            var len = party.Length;
            var tankAssigned = false;

            for (var i = 0; i < len; ++i)
            {
                var p = party[i];
                if (p.IsDead)
                    continue;

                if (p.Role == Role.Tank)
                {
                    if (!tankAssigned)
                    {
                        Stacks.Add(new(p, 6f, 2, 2, activation));
                        tankAssigned = true;
                    }
                    continue;
                }
                Spreads.Add(new(p, 6f, activation));
            }
        }
    }
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.RawSteel_TankStack or (uint)AID.RawSteel_Impact)
        {
            Stacks.Clear();
            Spreads.Clear();
            NumCasts++;
        }
    }
}
sealed class RawSteelTrophyScythe(ModuleBase module) : GenericBaitAway(module)
{
    private readonly AOEShapeCone tankCone = new(60f, 45.Degrees());
    private readonly AOEShapeCone healerCone = new(60f, 15.Degrees());

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.RawSteelTrophyScythe)
        {
            var act = Module.CastFinishAt(spell, 0.1d);
            var party = Module.World.Party.WithoutSlot();
            var len = party.Length;
            var maxhealers = 0;
            for (var i = 0; i < len; ++i)
            {
                var p = party[i];
                if (p.IsDead)
                    continue;

                if (p.Role == Role.Tank)
                    CurrentBaits.Add(new(caster, p, tankCone, act));
                else if (p.Role == Role.Healer && maxhealers < 1)
                {
                    CurrentBaits.Add(new(caster, p, healerCone, act));
                    maxhealers++;
                }
            }
        }
    }
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.RawSteel_TankCones or (uint)AID.RawSteel_HeavyHitter)
        {
            CurrentBaits.Clear();
            ++NumCasts;
        }
    }
}