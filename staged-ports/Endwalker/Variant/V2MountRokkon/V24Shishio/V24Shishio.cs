// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V2MountRokkon.V24Shishio;

sealed class NoblePursuit(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.NoblePursuit, 6f);
sealed class Enkyo(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Enkyo);

abstract class CloudToCloud : Components.SimpleAOEs
{
    protected CloudToCloud(ModuleBase module, uint aid, float halfWidth, int dangerCount) : base(module, aid, new AOEShapeRect(100f, halfWidth))
    {
        MaxDangerColor = dangerCount;
    }
}
sealed class CloudToCloud1(ModuleBase module) : CloudToCloud(module, (uint)AID.CloudToCloud1, 1f, 6);
sealed class CloudToCloud2(ModuleBase module) : CloudToCloud(module, (uint)AID.CloudToCloud2, 3f, 4);
sealed class CloudToCloud3(ModuleBase module) : CloudToCloud(module, (uint)AID.CloudToCloud3, 6f, 2);

sealed class SplittingCry(ModuleBase module) : Components.BaitAwayIcon(module, new AOEShapeRect(60f, 7f), (uint)IconID.Tankbuster, (uint)AID.SplittingCry, 5d, tankbuster: true);

sealed class ThunderVortex(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThunderVortex, new AOEShapeDonut(8f, 30f));
sealed class UnsagelySpinYokiThunderOneTwoThreefold(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.UnsagelySpin, (uint)AID.Yoki,
(uint)AID.ThunderOnefold, (uint)AID.ThunderTwofold, (uint)AID.ThunderThreefold], 6f);
sealed class Rush(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.Rush, 4f);
sealed class Vasoconstrictor(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Vasoconstrictor, 5f);

sealed class RightLeftSwipe(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.RightSwipe, (uint)AID.LeftSwipe], new AOEShapeCone(40f, 90f.Degrees()));

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 945u, CFCID = 945u, NameID = 12428u, PrimaryActorOID = (uint)OID.Shishio, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class V24Shishio(WorldState ws, Actor primary) : ModuleBase(ws, primary, ArenaCenter, new ArenaBoundsSquare(20f))
{
    public static readonly WPos ArenaCenter = new(-40f, -300f);
    public static readonly ArenaBoundsCustom CircleBounds = new([new Circle(ArenaCenter, 20f)], [new Rectangle(ArenaCenter + new WDir(-20f, default), 0.5f, 20f),
    new Rectangle(ArenaCenter + new WDir(20f, default), 0.5f, 20f), new Rectangle(ArenaCenter + new WDir(default, 20f), 20f, 0.5f), new Rectangle(ArenaCenter + new WDir(default, -20f), 20f, 0.5f)]);
}
