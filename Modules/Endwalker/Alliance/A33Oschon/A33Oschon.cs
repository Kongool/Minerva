// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A33Oschon;

class P1SuddenDownpour(ModuleBase module) : Components.CastCounter(module, (uint)AID.SuddenDownpourAOE);

class TrekShot(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(65f, 60f.Degrees()));
class P1TrekShotN(ModuleBase module) : TrekShot(module, (uint)AID.TrekShotNAOE);
class P1TrekShotS(ModuleBase module) : TrekShot(module, (uint)AID.TrekShotSAOE);

class SoaringMinuet(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(65f, 135f.Degrees()));
class P1SoaringMinuet1(ModuleBase module) : SoaringMinuet(module, (uint)AID.SoaringMinuet1);
class P1SoaringMinuet2(ModuleBase module) : SoaringMinuet(module, (uint)AID.SoaringMinuet2);

class P1Arrow(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.ArrowP1AOE, 6f);
class P1Downhill(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DownhillP1AOE, 6f);
class P2MovingMountains(ModuleBase module) : Components.CastCounter(module, (uint)AID.MovingMountains);
class P2PeakPeril(ModuleBase module) : Components.CastCounter(module, (uint)AID.PeakPeril);
class P2Shockwave(ModuleBase module) : Components.CastCounter(module, (uint)AID.Shockwave);
class P2SuddenDownpour(ModuleBase module) : Components.CastCounter(module, (uint)AID.P2SuddenDownpourAOE);

class P2PitonPull(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PitonPullAOE, 22f);
class P2Altitude(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AltitudeAOE, 6f);
class P2Arrow(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.ArrowP2AOE, 10f);

[ModuleInfo(CFCID = 962u, NameID = 11300u, PrimaryActorOID = (uint)OID.BossP1, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus, LTS (ported from BMR)")]
public class A33Oschon(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(default, 750f), new ArenaBoundsSquare(25f))
{
    private Actor? _bossP2;

    public Actor? BossP1() => PrimaryActor;
    public Actor? BossP2() => _bossP2;

    protected override void UpdateModule()
    {
        if (StateMachine.ActivePhaseIndex == 1)
        {
            _bossP2 ??= GetActor((uint)OID.BossP2);
        }
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actor(_bossP2);
    }
}
