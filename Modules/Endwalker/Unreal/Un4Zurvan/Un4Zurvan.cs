// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Unreal.Un4Zurvan;

abstract class MetalCutter(ModuleBase module, uint aid, OID oid) : Components.Cleave(module, aid, new AOEShapeCone(37.44f, 45f.Degrees()), [(uint)oid]);
class P1MetalCutter(ModuleBase module) : MetalCutter(module, (uint)AID.MetalCutterP1, OID.BossP1);

class P1FlareStar(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FlareStarAOE, 6f);
class P1Purge(ModuleBase module) : Components.CastCounter(module, (uint)AID.Purge);
class P2MetalCutter(ModuleBase module) : MetalCutter(module, (uint)AID.MetalCutterP2, OID.BossP2);
class P2IcyVoidzone(ModuleBase module) : Components.Voidzone(module, 5f, m => m.Enemies((uint)OID.IcyVoidzone).Where(z => z.EventState != 7));
class P2BitingHalberd(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BitingHalberd, new AOEShapeCone(55.27f, 135f.Degrees()));
class P2TailEnd(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TailEnd, 15f);
class P2Ciclicle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Ciclicle, new AOEShapeDonut(10f, 20f)); // TODO: verify inner radius
class P2SouthernCross(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SouthernCrossAOE, 6f);
class P2SouthernCrossVoidzone(ModuleBase module) : Components.Voidzone(module, 6f, m => m.Enemies((uint)OID.SouthernCrossVoidzone).Where(z => z.EventState != 7));
class P2WaveCannon(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.WaveCannonSolo, new AOEShapeRect(55.27f, 5f));
class P2TyrfingFire(ModuleBase module) : Components.Cleave(module, (uint)AID.TyrfingFire, new AOEShapeCircle(5), [(uint)OID.BossP2], originAtTarget: true);

[ModuleInfo(Group = ModuleGroup.RemovedUnreal, CFCID = 951u, NameID = 5567u, PrimaryActorOID = (uint)OID.BossP1, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Un4Zurvan(WorldState ws, Actor primary) : ModuleBase(ws, primary, default, new ArenaBoundsCircle(20))
{
    private Actor? _bossP2;

    public Actor? BossP1() => PrimaryActor.IsDestroyed ? null : PrimaryActor;
    public Actor? BossP2() => _bossP2;

    protected override void UpdateModule()
    {
        _bossP2 ??= GetActor((uint)OID.BossP2);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actor(_bossP2);
    }
}
