// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A24TheCompound2P;

sealed class CentrifugalSlice(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.CentrifugalSlice);
sealed class R012LaserAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.R012LaserAOE, 6f);
sealed class R012LaserSpread(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.R012LaserSpread, 6f);
sealed class R012LaserTB(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.R012LaserTB, 6f, tankbuster: true);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 736u, CFCID = 736u, NameID = 9646u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class A24TheCompound2P(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(200f, -700f), new ArenaBoundsSquare(30f))
{
    public Actor? BossP2;

    protected override void UpdateModule()
    {
        BossP2 ??= GetActor((uint)OID.Compound2P);
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
                Arena.Actor(BossP2);
                break;
        }
    }
}
