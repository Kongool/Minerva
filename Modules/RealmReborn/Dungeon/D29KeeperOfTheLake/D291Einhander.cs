// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Dungeon.D29TheKeeperoftheLake.D291Einhander;

public enum OID : uint
{
    Boss = 0x3927, // R2.6
    Astraea = 0x3928, // R2.0
    AuxiliaryCeruleumTank = 0x3929 // R1.5
}

public enum AID : uint
{
    AutoAttack = 870, // Boss->player, no cast, single-target
    Teleport = 29646, // Boss->location, no cast, single-target

    AeroBlast = 29273, // Boss->self, 4.0s cast, range 40 circle
    ResoundingScreech = 29270, // Boss->self, 3.0s cast, single-target
    MarkXLIQuickFiringCannon = 29271, // Boss->self, 5.0s cast, range 40 width 4 rect
    CeruleumExplosion = 29275, // AuxiliaryCeruleumTank->self, 8.0s cast, range 12 circle
    HeavySwing = 29620, // Boss->player, 5.0s cast, single-target
    MarkXLIIIMiniCannon = 29272 // Boss->location, 5.0s cast, range 31 circle, damage fall off AOE
}

class AeroBlast(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AeroBlast);
class MarkXLIQuickFiringCannon(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MarkXLIQuickFiringCannon, new AOEShapeRect(40f, 2f));
class CeruleumExplosion(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CeruleumExplosion, 12f);
class HeavySwing(ModuleBase module) : Components.SingleTargetDelayableCast(module, (uint)AID.HeavySwing);
class MarkXLIIIMiniCannon(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MarkXLIIIMiniCannon, 15f);

class D291EinhanderStates : StateMachineBuilder
{
    public D291EinhanderStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<AeroBlast>()
            .ActivateOnEnter<MarkXLIQuickFiringCannon>()
            .ActivateOnEnter<MarkXLIIIMiniCannon>()
            .ActivateOnEnter<CeruleumExplosion>()
            .ActivateOnEnter<HeavySwing>();
    }
}

[ModuleInfo(CFCID = 32u, NameID = 3369u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public class D291Einhander(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Circle(new(18.75f, -16.95f), 19.5f)], [new Rectangle(new(36.824f, -25.291f), 20, 1.25f, -67.333f.Degrees()), new Rectangle(new(1, -8.1f), 20, 1.4f, -65f.Degrees())]);
}
