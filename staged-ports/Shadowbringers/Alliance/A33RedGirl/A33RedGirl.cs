// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A33RedGirl;

sealed class CrueltyP1(ModuleBase module) : Components.RaidwideCastDelay(module, (uint)AID.CrueltyVisualP1, (uint)AID.Cruelty, 0.1d);
sealed class CrueltyP2(ModuleBase module) : Components.RaidwideCastDelay(module, (uint)AID.CrueltyVisualP2, (uint)AID.Cruelty, 0.1d);
sealed class SublimeTranscendence(ModuleBase module) : Components.RaidwideCastDelay(module, (uint)AID.SublimeTranscendenceVisual, (uint)AID.SublimeTranscendence, 0.1d);
sealed class ManipulateEnergy(ModuleBase module) : Components.BaitAwayIcon(module, 3f, (uint)IconID.ManipulateEnergy, (uint)AID.ManipulateEnergy, tankbuster: true);

sealed class GenerateBarrier1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GenerateBarrierVisual1, new AOEShapeRect(18f, 1.5f));
sealed class GenerateBarrier2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GenerateBarrierVisual2, new AOEShapeRect(24f, 1.5f));
sealed class GenerateBarrier3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GenerateBarrierVisual3, new AOEShapeRect(12f, 1.5f));
sealed class GenerateBarrier4(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GenerateBarrierVisual4, new AOEShapeRect(6f, 1.5f));

sealed class Explosion(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Explosion, 9f, riskyWithSecondsLeft: 5d);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 779u, CFCID = 779u, NameID = 9920u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class A33RedGirl(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(845f, -851f), new ArenaBoundsSquare(24.5f))
{
    public Actor? BossP2;
    public Actor? RedSphere;

    protected override void UpdateModule()
    {
        RedSphere ??= GetActor((uint)OID.RedSphere);
        if (StateMachine.ActivePhaseIndex >= 1)
        {
            BossP2 ??= GetActor((uint)OID.BossP2);
        }
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        switch (StateMachine.ActivePhaseIndex)
        {
            case -1:
            case 0:
                Arena.Actor(PrimaryActor);
                break;
            case 1:
                Arena.Actor(RedSphere);
                Arena.Actors(Enemies((uint)OID.BlackPylon));
                Arena.Actors(Enemies((uint)OID.WhitePylon));
                Arena.Actors(Enemies((uint)OID.BlackWall), Colors.Object);
                Arena.Actors(Enemies((uint)OID.WhiteWall), Colors.Object);
                break;
            case 2:
                Arena.Actor(BossP2);
                break;
        }
    }
}
