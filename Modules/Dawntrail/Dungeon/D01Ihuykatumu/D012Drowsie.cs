// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt) — Modules/Dawntrail/Dungeon/D01Ihuykatumu/
// D012Drowsie.cs, by The Combat Reborn Team (Malediktus, LTS). Mechanical port to Minerva's API:
//   - namespace BossMod.* -> Minerva.*, base class BossModule -> ModuleBase.
//   - [ModuleInfo]: GroupType/GroupID -> CFCID, BossModuleInfo.Maturity -> ModuleMaturity.
//   - ArenaBoundsCustom (polygon + bridge cut-outs) approximated as a 19.5y circle (Minerva has no custom
//     mesh bounds yet); DrawEnemies override dropped (default draws the primary). Everything else is 1:1 —
//     SimpleAOEs/SingleTargetCast/SpreadFromCastTargets and the AOE shapes all match Minerva as-is.
using System;
using Minerva;

namespace Minerva.Dawntrail.Dungeon.D012Drowsie;

public enum OID : uint
{
    Boss = 0x4195, // R5.0
    IhuykatumuIvy = 0x419C, // R4.2-8.4
    BlueClot = 0x4197, // R2.0
    GreenClot = 0x4196, // R3.5
    RedClot = 0x4198, // R1.3
    Mimiclot1 = 0x419B, Mimiclot2 = 0x41A0, Mimiclot3 = 0x4199,
    Mimiclot4 = 0x41A1, Mimiclot5 = 0x419A, Mimiclot6 = 0x419F,
    Helper = 0x233C,
}

public enum AID : uint
{
    Uppercut = 39132, // Boss->player, 5.0s cast, single-target
    Arise = 36478, // IhuykatumuIvy->self, 3.0s cast, range 8 circle
    Wallop1 = 36479, // IhuykatumuIvy->self, 7.0s cast, range 40 width 10 rect
    Wallop2 = 36482, // IhuykatumuIvy->self, 7.0s cast, range 40 width 16 rect
    Sneeze = 36475, // Boss->self, 5.0s cast, range 60 150-degree cone
    FlagrantSpread1 = 36522, // Mimiclot5/Mimiclot2->player, 5.0s cast, range 6 circle
    FlagrantSpread2 = 36485, // Mimiclot3/Mimiclot6->self, 5.0s cast, range 6 circle
}

sealed class Uppercut(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Uppercut);
sealed class Arise(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Arise, 8f);
sealed class Wallop1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Wallop1, new AOEShapeRect(40f, 5f));
sealed class Wallop2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Wallop2, new AOEShapeRect(40f, 8f));
sealed class SelfTargetSneezeedAOEs(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Sneeze, new AOEShapeCone(60f, 75f.Degrees()));
sealed class FlagrantSpread1(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.FlagrantSpread1, 6f);
sealed class FlagrantSpread2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FlagrantSpread2, 6f);

sealed class D012DrowsieStates : StateMachineBuilder
{
    public D012DrowsieStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<Uppercut>()
            .ActivateOnEnter<Arise>()
            .ActivateOnEnter<Wallop1>()
            .ActivateOnEnter<Wallop2>()
            .ActivateOnEnter<SelfTargetSneezeedAOEs>()
            .ActivateOnEnter<FlagrantSpread1>()
            .ActivateOnEnter<FlagrantSpread2>();
    }
}

[ModuleInfo(CFCID = 826u, NameID = 12716u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn (The Combat Reborn Team: Malediktus, LTS)")]
public sealed class D012Drowsie(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(80f, 53f), new ArenaBoundsCircle(19.5f)); // ArenaBoundsCustom approximated
