// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Ultimate.TEA;

[SkipLocalsInit]
sealed class P1FluidSwing(ModuleBase module) : Components.Cleave(module, (uint)AID.FluidSwing, new AOEShapeCone(11.5f, 45f.Degrees()));
[SkipLocalsInit]
sealed class P1FluidStrike(ModuleBase module) : Components.Cleave(module, (uint)AID.FluidStrike, new AOEShapeCone(11.6f, 45f.Degrees()), [(uint)OID.LiquidHand]);
[SkipLocalsInit]
sealed class P1Sluice(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Sluice, 5f);
[SkipLocalsInit]
sealed class P1Splash(ModuleBase module) : Components.CastCounter(module, (uint)AID.Splash);
[SkipLocalsInit]
sealed class P1Drainage(ModuleBase module) : Components.TankbusterTether(module, (uint)AID.DrainageP1, (uint)TetherID.Drainage, 6f);

[SkipLocalsInit]
sealed class P2JKick(ModuleBase module) : Components.CastCounter(module, (uint)AID.JKick);
[SkipLocalsInit]
sealed class P2HawkBlasterOpticalSight(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HawkBlasterP2, 10f);
[SkipLocalsInit]
sealed class P2Photon(ModuleBase module) : Components.CastCounter(module, (uint)AID.PhotonAOE);
[SkipLocalsInit]
sealed class P2SpinCrusher(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SpinCrusher, new AOEShapeCone(10f, 45f.Degrees()));
[SkipLocalsInit]
sealed class P2Drainage(ModuleBase module) : Components.Voidzone(module, 8f, GetVoidzones) // TODO: verify distance
{
    private static List<Actor> GetVoidzones(ModuleBase module) => module.Enemies((uint)OID.LiquidRage);
}

[SkipLocalsInit]
sealed class P2PropellerWind(ModuleBase module) : Components.CastLineOfSightAOE(module, (uint)AID.PropellerWind, 50f)
{
    public override ReadOnlySpan<Actor> BlockerActors() => CollectionsMarshal.AsSpan(Module.Enemies((uint)OID.GelidGaol));
}

[SkipLocalsInit]
sealed class P2DoubleRocketPunch(ModuleBase module) : Components.CastSharedTankbuster(module, (uint)AID.DoubleRocketPunch, 3f);
[SkipLocalsInit]
sealed class P3ChasteningHeat(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.ChasteningHeat, 5f, tankbuster: true);
[SkipLocalsInit]
sealed class P3DivineSpear(ModuleBase module) : Components.Cleave(module, (uint)AID.DivineSpear, new AOEShapeCone(24.2f, 45f.Degrees()), [(uint)OID.AlexanderPrime]);
[SkipLocalsInit]
sealed class P3DivineJudgmentRaidwide(ModuleBase module) : Components.CastCounter(module, (uint)AID.DivineJudgmentRaidwide);

[SkipLocalsInit]
sealed class P4IrresistibleGrace(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.IrresistibleGrace, 6f, 8, 8);

[ModuleInfo(CFCID = 694u, NameID = 9042u, PrimaryActorOID = (uint)OID.BossP1, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
[SkipLocalsInit]
public sealed class TEA : ModuleBase
{
    public Actor? LiquidHand2;
    public Actor? BossP1() => PrimaryActor;
    public Actor? LiquidHand() => LiquidHand2;

    private Actor? _bruteJustice;
    private Actor? _cruiseChaser;
    public Actor? BruteJustice() => _bruteJustice;
    public Actor? CruiseChaser() => _cruiseChaser;

    private Actor? _alexPrime;
    private readonly List<Actor> _trueHeart;
    public Actor? AlexPrime() => _alexPrime;
    public Actor? TrueHeart() => _trueHeart.Count != 0 ? _trueHeart[0] : null;

    private Actor? _perfectAlex;
    public Actor? PerfectAlex() => _perfectAlex;

    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(100f, 100f), 20f, 48)]);

    public TEA(WorldState ws, Actor primary) : base(ws, primary, arena.Center, arena)
    {
        _trueHeart = Enemies((uint)OID.TrueHeart);
    }

    public override bool ShouldPrioritizeAllEnemies => true;

    protected override void UpdateModule()
    {
        LiquidHand2 ??= GetActor((uint)OID.LiquidHand);
        _bruteJustice ??= GetActor((uint)OID.BruteJustice);
        _cruiseChaser ??= GetActor((uint)OID.CruiseChaser);
        _alexPrime ??= GetActor((uint)OID.AlexanderPrime);
        _perfectAlex ??= GetActor((uint)OID.PerfectAlexander);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        switch (StateMachine.ActivePhaseIndex)
        {
            case -1:
            case 0:
                Arena.Actor(PrimaryActor);
                Arena.Actor(LiquidHand2);
                break;
            case 1:
                Arena.Actor(_bruteJustice, allowDeadAndUntargetable: true);
                Arena.Actor(_cruiseChaser, allowDeadAndUntargetable: true);
                break;
            case 2:
                Arena.Actor(_alexPrime);
                Arena.Actor(TrueHeart());
                Arena.Actor(_bruteJustice);
                Arena.Actor(_cruiseChaser);
                break;
            case 3:
                Arena.Actor(_perfectAlex);
                break;
        }
    }
}
