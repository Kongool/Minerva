// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRN3QueensGuard;

sealed class OptimalPlaySword(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.OptimalPlaySword, 10f);
sealed class OptimalPlayShield(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.OptimalPlayShield, new AOEShapeDonut(5f, 60f));
sealed class BloodAndBoneQueenShotUnseen(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.BloodAndBoneKnight, (uint)AID.BloodAndBoneSoldier, (uint)AID.BloodAndBoneWarrior, (uint)AID.QueensShotUnseen]);
sealed class RapidSeverShotInTheDark(ModuleBase module) : Components.SingleTargetCasts(module, [(uint)AID.RapidSeverKnight, (uint)AID.RapidSeverSoldier, (uint)AID.RapidSeverWarrior, (uint)AID.ShotInTheDark]);
sealed class Enrages(ModuleBase module) : Components.CastHints(module, [(uint)AID.BloodAndBoneKnightEnrage, (uint)AID.BloodAndBoneSoldierEnrage, (uint)AID.BloodAndBoneWarriorEnrage, (uint)AID.QueensShotEnrage], "Enrage!", true);

sealed class PawnOff(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PawnOffReal, 20f);

[ModuleInfo(CFCID = 760u, NameID = 9838u, PrimaryActorOID = (uint)OID.QueensKnight, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class DRN3QueensGuard(WorldState ws, Actor primary) : QueensGuard(ws, primary)
{
    private Actor? _warrior;
    private Actor? _soldier;
    private Actor? _gunner;

    protected override void UpdateModule()
    {
        _warrior ??= GetActor((uint)OID.QueensWarrior);
        _soldier ??= GetActor((uint)OID.QueensSoldier);
        _gunner ??= GetActor((uint)OID.QueensGunner);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actor(_warrior);
        Arena.Actor(_soldier);
        Arena.Actor(_gunner);
    }
}
