// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Ultimate.DSW2;

sealed class P2AscalonsMercyConcealed(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AscalonsMercyConcealedAOE, new AOEShapeCone(50f, 15f.Degrees()));

sealed class AscalonMight(ModuleBase module) : Components.Cleave(module, (uint)AID.AscalonsMight, new AOEShapeCone(50f, 30f.Degrees()), [(uint)OID.BossP2, (uint)OID.BossP5]);

sealed class P2UltimateEnd(ModuleBase module) : Components.CastCounter(module, (uint)AID.UltimateEndAOE);
sealed class P3Drachenlance(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DrachenlanceAOE, new AOEShapeCone(13f, 45f.Degrees()));
sealed class P3SoulTether(ModuleBase module) : Components.TankbusterTether(module, (uint)AID.SoulTether, (uint)TetherID.HolyShieldBash, 5f);
sealed class P4Resentment(ModuleBase module) : Components.CastCounter(module, (uint)AID.Resentment);
sealed class P5TwistingDive(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TwistingDive, new AOEShapeRect(60f, 5f));

sealed class P5Cauterize(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.Cauterize1, (uint)AID.Cauterize2], new AOEShapeRect(48f, 10f));

sealed class P5SpearOfTheFury(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SpearOfTheFuryP5, new AOEShapeRect(50f, 5f));
sealed class P5Surrender(ModuleBase module) : Components.CastCounter(module, (uint)AID.Surrender);
sealed class P6SwirlingBlizzard(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SwirlingBlizzard, new AOEShapeDonut(20f, 35f));
sealed class P7Shockwave(ModuleBase module) : Components.CastCounter(module, (uint)AID.ShockwaveP7);
sealed class P7AlternativeEnd(ModuleBase module) : Components.CastCounter(module, (uint)AID.AlternativeEnd);

[ModuleInfo(CFCID = 788u, NameID = 11319u, PrimaryActorOID = (uint)OID.BossP2, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public sealed class DSW2(WorldState ws, Actor primary) : ModuleBase(ws, primary, BoundsCircle.Center, BoundsCircle)
{
    public static readonly ArenaBoundsCustom BoundsCircle = new([new Polygon(new(100f, 100f), 21f, 48)]) { IsCircle = true };

    private Actor? _bossP3;
    private Actor? _leftEyeP4;
    private Actor? _rightEyeP4;
    private Actor? _nidhoggP4;
    public Actor? _SerCharibert;
    private Actor? _spear;
    private Actor? _bossP5;
    public Actor? _NidhoggP6;
    public Actor? _HraesvelgrP6;
    private Actor? _bossP7;
    public Actor? ArenaFeatures;
    public Actor? BossP3() => _bossP3;
    public Actor? LeftEyeP4() => _leftEyeP4;
    public Actor? RightEyeP4() => _rightEyeP4;
    public Actor? NidhoggP4() => _nidhoggP4;
    public Actor? SerCharibert() => _SerCharibert;
    public Actor? Spear() => _spear;
    public Actor? BossP5() => _bossP5;
    public Actor? NidhoggP6() => _NidhoggP6;
    public Actor? HraesvelgrP6() => _HraesvelgrP6;
    public Actor? BossP7() => _bossP7;

    public override bool ShouldPrioritizeAllEnemies => true;

    protected override void UpdateModule()
    {
        switch (StateMachine.ActivePhaseIndex)
        {
            case 0:
                ArenaFeatures ??= GetActor((uint)OID.ArenaFeatures);
                break;
            case 1:
                _bossP3 ??= GetActor((uint)OID.BossP3);
                break;
            case 2:
                _leftEyeP4 ??= GetActor((uint)OID.LeftEye);
                _rightEyeP4 ??= GetActor((uint)OID.RightEye);
                _nidhoggP4 ??= GetActor((uint)OID.NidhoggP4);
                break;
            case 3:
                _SerCharibert ??= GetActor((uint)OID.SerCharibert);
                _spear ??= GetActor((uint)OID.SpearOfTheFury);
                break;
            case 4:
                _bossP5 ??= GetActor((uint)OID.BossP5);
                break;
            case 5:
                _NidhoggP6 ??= GetActor((uint)OID.NidhoggP6);
                _HraesvelgrP6 ??= GetActor((uint)OID.HraesvelgrP6);
                break;
            case 6:
                _bossP7 ??= GetActor((uint)OID.DragonKingThordan);
                break;
        }
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor, allowDeadAndUntargetable: true);
        Arena.Actor(_bossP3);
        Arena.Actor(_leftEyeP4);
        Arena.Actor(_rightEyeP4);
        Arena.Actor(_nidhoggP4);
        Arena.Actor(_SerCharibert);
        Arena.Actor(_spear);
        Arena.Actor(_bossP5);
        Arena.Actor(_NidhoggP6);
        Arena.Actor(_HraesvelgrP6);
        Arena.Actor(_bossP7);
    }
}
