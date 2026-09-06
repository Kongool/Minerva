// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Extreme.Ex6Byakko;

class UnrelentingAnguish(ModuleBase module) : Components.Voidzone(module, 2f, GetVoidzones, 2f)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.AratamaForce);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (!z.IsDead)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }
}

class OminousWind(ModuleBase module) : ModuleComponent(module)
{
    public BitMask Targets;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (Targets[slot])
        {
            var party = Raid.WithSlot(false, true, true);
            var len = party.Length;
            for (var i = 0; i < len; ++i)
            {
                ref readonly var p = ref party[i];
                if (p.Item1 == slot)
                    continue;
                if (Targets[i] && p.Item2.Position.InCircle(actor.Position, 6f))
                {
                    hints.Add("GTFO from other bubble!");
                    return;
                }
            }
        }
    }

    public override PlayerPriority CalcPriority(int pcSlot, Actor pc, int playerSlot, Actor player, ref uint customColor) => Targets[pcSlot] && Targets[playerSlot] ? PlayerPriority.Danger : PlayerPriority.Irrelevant;

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (Targets[pcSlot])
        {
            var party = Raid.WithSlot(false, true, true);
            var len = party.Length;
            for (var i = 0; i < len; ++i)
            {
                ref readonly var p = ref party[i];
                if (p.Item1 == pcSlot)
                {
                    continue;
                }
                if (Targets[p.Item1])
                {
                    Arena.ZoneCircleOutline(p.Item2.Position, 6f);
                }
            }
        }
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.OminousWind)
        {
            Targets.Set(Raid.FindSlot(actor.InstanceID));
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.OminousWind)
        {
            Targets.Clear(Raid.FindSlot(actor.InstanceID));
        }
    }
}

class GaleForce(ModuleBase module) : Components.BaitAwayIcon(module, 6f, (uint)IconID.Bombogenesis, (uint)AID.GaleForce, 8.1f);

class VacuumClaw(ModuleBase module) : Components.Voidzone(module, 12f, GetVoidzones)
{
    public bool Active = true;

    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.VacuumClaw);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (!z.IsDead)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.VacuumClaw)
            if (++NumCasts == 13 * GetVoidzones(Module).Length) // 1-3 voidzones depending on player count
                Active = false;
    }
}
