// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Dungeon.D02TamTaraDeepcroft.D022GalvanthTheDominator;

public enum OID : uint
{
    Boss = 0x4C, // R1.95,
    InconspicuousImp = 0x7D, // R0.45
    SkeletonSoldier = 0x7E, // R0.75
    DeepcroftMiteling = 0x7F // R1.8
}

public enum AID : uint
{
    AutoAttack = 870, // Boss->player, no cast

    Water = 971, // Boss->player, 1.0s cast, single target
    DrainTouch = 988, // Boss->player, no cast, single target
    MindBlast = 987, // Boss->self, 5.0s cast, range 8+R (9.95) circle
    HellSlash = 341 // SkeletonSoldier->player, no cast, single target
}

class MindBlast(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MindBlast, new AOEShapeCircle(9.95f));

class InconspicuousImp(ModuleBase module) : ModuleComponent(module)
{
    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var imps = Module.Enemies((uint)OID.InconspicuousImp);
        var count = imps.Count;
        for (var i = 0; i < count; ++i)
        {
            if (!imps[i].IsDead)
            {
                hints.Add("Kill the imps!");
                return;
            }
        }
    }
}

class D022GalvanthTheDominatorStates : StateMachineBuilder
{
    public D022GalvanthTheDominatorStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<MindBlast>()
            .ActivateOnEnter<InconspicuousImp>();
    }
}

[ModuleInfo(CFCID = 2u, NameID = 73u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Chuggalo (ported from BMR)")]
public class D022GalvanthTheDominator(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-52.765f, -12.789f), new ArenaBoundsCircle(18f))
{
    private static readonly uint[] trash = [(uint)OID.InconspicuousImp, (uint)OID.DeepcroftMiteling, (uint)OID.SkeletonSoldier];

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.InconspicuousImp => 1,
                _ => 0
            };
        }
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(this, trash);
    }
}
