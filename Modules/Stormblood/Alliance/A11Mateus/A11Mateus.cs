// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Alliance.A11Mateus;

class HypothermalCombustion(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HypothermalCombustion, 9.04f);
class DarkBlizzardIII(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.DarkBlizzardIII, 5f);
class Chill(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Chill, new AOEShapeCone(41f, 10f.Degrees()));
class BlizzardIV(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BlizzardIV, 15f);

class FlashFreeze(ModuleBase module) : Components.Cleave(module, (uint)AID.FlashFreeze, new AOEShapeCone(16.5f, 60f.Degrees()), activeWhileCasting: false)
{
    private bool active = true;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (active)
            base.AddHints(slot, actor, hints);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (active)
            base.AddAIHints(slot, actor, assignment, hints);
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (active)
            base.DrawArenaForeground(pcSlot, pc);
    }

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.Totema)
            active = true;
    }

    public override void OnActorDestroyed(Actor actor)
    {
        if (actor.OID == (uint)OID.Totema)
            active = false;
    }
}

[ModuleInfo(CFCID = 281u, NameID = 6929u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public class A11Mateus(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-320f, 240f), new ArenaBoundsSquare(29.5f))
{
    private static readonly uint[] trash = [(uint)OID.IceAzer, (uint)OID.IceSlave, (uint)OID.FlumeToad, (uint)OID.AquaSphere, (uint)OID.AzureGuard, (uint)OID.BlizzardIII];

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(this, trash);
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.Boss => 0,
                _ => 1
            };
        }
    }
}
