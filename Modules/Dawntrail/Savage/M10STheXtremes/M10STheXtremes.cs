// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M10STheXtremes;

// damage falloff based on proximity
sealed class XtremeSpectacular(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.XtremeSpectacular, new AOEShapeRect(50f, 18f));
sealed class XtremeSpectacularLast(ModuleBase module) : Components.CastCounter(module, (uint)AID.XtremeSpectacularLast);
sealed class SteamBurst(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SteamBurst, 9f);
sealed class DebuffTracker(ModuleBase module) : ModuleComponent(module)
{
    public BitMask FirePlayers = default;
    public BitMask WaterPlayers = default;

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID is (uint)SID.Watersnaking or (uint)SID.XtremeWatersnaking or (uint)SID.Firesnaking or (uint)SID.XtremeFiresnaking)
        {
            var slot = Raid.FindSlot(actor.InstanceID);
            if (slot == -1)
                return;

            if (status.ID is (uint)SID.Watersnaking or (uint)SID.XtremeWatersnaking)
            {
                WaterPlayers.Set(slot);
            }
            else
            {
                FirePlayers.Set(slot);
            }
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID is (uint)SID.Watersnaking or (uint)SID.XtremeWatersnaking or (uint)SID.Firesnaking or (uint)SID.XtremeFiresnaking)
        {
            var slot = Raid.FindSlot(actor.InstanceID);
            if (slot == -1)
                return;

            if (status.ID is (uint)SID.Watersnaking or (uint)SID.XtremeWatersnaking)
            {
                WaterPlayers.Clear(slot);
            }
            else
            {
                FirePlayers.Clear(slot);
            }
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (FirePlayers[slot])
        {
            hints.Add("Fire debuff", false);
        }
        else if (WaterPlayers[slot])
        {
            hints.Add("Water debuff", false);
        }
    }

    public Actor[] GetWaterActors() => GetActors(WaterPlayers);
    public Actor[] GetFireActors() => GetActors(FirePlayers);

    private Actor[] GetActors(BitMask mask)
    {
        if (mask.None())
            return [];

        var masked = Raid.WithSlot(false, true, true).IncludedInMask(mask).ToArray();
        var len = masked.Length;

        var actors = new Actor[len];
        for (var i = 0; i < len; i++)
        {
            actors[i] = masked[i].Item2;
        }

        return actors;
    }
}
sealed class Watersnaking(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Watersnaking);
sealed class Firesnaking(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Firesnaking);
sealed class ScathingSteam(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ScathingSteam);
sealed class XtremeWatersnaking(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.XtremeWatersnaking);
sealed class XtremeFiresnaking(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.XtremeFiresnaking);
sealed class Bailout(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.Bailout1, (uint)AID.Bailout2], 15f);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1071u, CFCID = 1071u, NameID = 14370u, PrimaryActorOID = (uint)OID.RedHot, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "gynorhino (ported from BMR)")]
public sealed class M10STheXtremes(WorldState ws, Actor primary) : ModuleBase(ws, primary, ArenaCenter, ArenaBounds)
{
    public static readonly WPos ArenaCenter = new(100f, 100f);
    public static readonly ArenaBoundsSquare ArenaBounds = new(20f);
    public static readonly uint[] Bosses = [(uint)OID.RedHot, (uint)OID.DeepBlue];

    private Actor? _deepBlue;
    private Actor? _wateryGrave;
    public Actor? DeepBlue() => _deepBlue;
    public Actor? WateryGrave() => _wateryGrave;

    protected override void UpdateModule()
    {
        _deepBlue ??= GetActor((uint)OID.DeepBlue);
        _wateryGrave ??= GetActor((uint)OID.WateryGrave);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actor(_deepBlue);
    }
}
