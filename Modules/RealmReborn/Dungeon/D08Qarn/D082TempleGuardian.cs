// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Dungeon.D08Qarn.D082TempleGuardian;

public enum OID : uint
{
    Boss = 0x477C, // R2.2
    GolemSoulstone = 0x477D // R2.2
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target

    BoulderClap = 42234, // Boss->self, 2.5s cast, range 12+R 120-degree cone
    TrueGrit = 42235, // Boss->self, 3.0s cast, range 12+R 120-degree cone
    Rockslide = 42236, // Boss->self, 2.5s cast, range 14+R width 8 rect
    StoneSkull = 42237, // Boss->player, no cast, single-target
    Obliterate = 42238 // Boss->self, 2.0s cast, range 60 circle
}

sealed class BoulderClapTrueGrit(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.BoulderClap, (uint)AID.TrueGrit], new AOEShapeCone(14.2f, 60.Degrees()));
sealed class Rockslide(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Rockslide, new AOEShapeRect(16.2f, 4f));
sealed class Obliterate(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Obliterate);

sealed class D082TempleGuardianStates : StateMachineBuilder
{
    public D082TempleGuardianStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<BoulderClapTrueGrit>()
            .ActivateOnEnter<Rockslide>()
            .ActivateOnEnter<Obliterate>();
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 9u, CFCID = 9u, NameID = 1569u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus, Chuggalo (ported from BMR)")]
public sealed class D082TempleGuardian(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    public static readonly ArenaBoundsCustom arena = new([new PolygonCustom([new(66.5f, -33.7f), new(58.6f, -25), new(51.4f, -22.5f),
    new(39.3f, -16.5f), new(36.6f, -5), new(39.3f, 5.7f), new(41.1f, 16),
    new(56.5f, 14.8f), new(63.6f, 7.1f), new(64.7f, 3.3f), new(70.3f, -3.9f), new(72.6f, -33.3f)])]);

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.GolemSoulstone => 1,
                _ => 0
            };
        }
    }
}
