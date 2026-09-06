// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V2MountRokkon.V25Enenra;

sealed class PipeCleaner(ModuleBase module) : Components.BaitAwayTethers(module, new AOEShapeRect(60f, 5f), (uint)TetherID.PipeCleaner);
sealed class Uplift(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Uplift, 6f);
sealed class Snuff(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.Snuff, 6f, tankbuster: true);

sealed class Smoldering(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Smoldering, 8f, 8);
sealed class FlagrantCombustion(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.FlagrantCombustion);
sealed class SmokeRings(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SmokeRings, 16f);
sealed class ClearingSmoke(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.ClearingSmoke, 16f, stopAfterWall: true)
{
    private readonly Smoldering _aoe = module.FindComponent<Smoldering>()!;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0 && _aoe.Casters.Count != 0)
        {
            hints.AddForbiddenZone(new SDInvertedCircle(Center, 4f), Casters.Ref(0).Activation);
        }
    }
}

sealed class StringRock(ModuleBase module) : Components.ConcentricAOEs(module, _shapes)
{
    private static readonly AOEShape[] _shapes = [new AOEShapeCircle(6f), new AOEShapeDonut(6f, 12f), new AOEShapeDonut(12f, 18f), new AOEShapeDonut(18f, 24f)];

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.KiseruClamor)
        {
            AddSequence(spell.LocXZ, Module.CastFinishAt(spell));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (Sequences.Count != 0)
        {
            var order = spell.Action.ID switch
            {
                (uint)AID.KiseruClamor => 0,
                (uint)AID.BedrockUplift1 => 1,
                (uint)AID.BedrockUplift2 => 2,
                (uint)AID.BedrockUplift3 => 3,
                _ => -1
            };
            AdvanceSequence(order, spell.LocXZ, World.FutureTime(2d));
        }
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 945u, CFCID = 945u, NameID = 12393u, PrimaryActorOID = (uint)OID.Enenra, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class V25Enenra(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(900f, -900f), StartingBounds)
{
    public static readonly ArenaBoundsCircle StartingBounds = new(20.5f);
    public static readonly ArenaBoundsCircle DefaultBounds = new(20f);

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.EnenraClone));
    }
}
