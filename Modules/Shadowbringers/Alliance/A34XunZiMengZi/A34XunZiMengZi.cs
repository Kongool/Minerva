// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A34XunZiMengZi;

sealed class DeployArmaments(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.DeployArmaments1, (uint)AID.DeployArmaments2,
(uint)AID.DeployArmaments3, (uint)AID.DeployArmaments4], new AOEShapeRect(50f, 9f));
sealed class UniversalAssault(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.UniversalAssault);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 779u, CFCID = 779u, NameID = 9921u, PrimaryActorOID = (uint)OID.XunZi, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class A34XunZiMengZi(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    // the small squares are actually dodecagons (12-gon) with a radius of 4.4, but only one edge affects the arena so we approximate it with a square
    private static readonly ArenaBoundsCustom arena = new([new Square(new(800f, 800f), 24.5f)], [new Square(new(773.00928f, 826.94116f), 4.75f, 45f.Degrees()),
    new Square(new(772.99066f, 773.00934f), 4.75f, 45f.Degrees()), new Square(new(826.99066f, 772.99066f), 4.75f, 45f.Degrees()),
     new Square(new(827.00928f, 826.92249f), 4.75f, 45f.Degrees())]);
    public Actor? BossMengZi;

    protected override void UpdateModule()
    {
        BossMengZi ??= GetActor((uint)OID.MengZi);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actor(BossMengZi);
    }
}
