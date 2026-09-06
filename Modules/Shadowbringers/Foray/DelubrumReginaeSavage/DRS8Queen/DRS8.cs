// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS8Queen;

sealed class NorthswainsGlow(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.NorthswainsGlowAOE, 20f);
sealed class CleansingSlashSecond(ModuleBase module) : Components.CastCounter(module, (uint)AID.CleansingSlashSecond);
sealed class GodsSaveTheQueen(ModuleBase module) : Components.CastCounter(module, (uint)AID.GodsSaveTheQueenAOE);
sealed class JudgmentBlade(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.JudgmentBladeLAOE, (uint)AID.JudgmentBladeRAOE], new AOEShapeRect(70f, 15f));
// note: apparently there is no 'front unseen' status
sealed class QueensShot(ModuleBase module) : Components.CastWeakpoint(module, (uint)AID.QueensShot, 60f, default, (uint)SID.BackUnseen, (uint)SID.LeftUnseen, (uint)SID.RightUnseen);
sealed class TurretsTourUnseen(ModuleBase module) : Components.CastWeakpoint(module, (uint)AID.TurretsTourUnseen, new AOEShapeRect(50f, 2.5f), default, (uint)SID.BackUnseen, (uint)SID.LeftUnseen, (uint)SID.RightUnseen);

sealed class OptimalOffensive(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.OptimalOffensive, 2.5f);

// note: there are two casters (as usual in bozja content for raidwides)
sealed class OptimalOffensiveKnockback(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.OptimalOffensiveKnockback, 10f, true, 1);

sealed class OptimalPlaySword(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.OptimalPlaySword, 10f);
sealed class OptimalPlayShield(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.OptimalPlayShield, new AOEShapeDonut(5f, 60f));
sealed class OptimalPlayCone(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.OptimalPlayCone, new AOEShapeCone(60f, 135f.Degrees()));
sealed class PawnOff(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PawnOffReal, 20f);

[ModuleInfo(CFCID = 761u, NameID = 9863u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public sealed class DRS8Queen(WorldState ws, Actor primary) : Queen(ws, primary)
{
    private Actor? _warrior;
    private Actor? _soldier;
    private Actor? _gunner;
    private Actor? _knight;
    public Actor? Knight() => _knight;
    public Actor? Soldier() => _soldier;
    public Actor? Gunner() => _gunner;
    public Actor? Warrior() => _warrior;

    protected override void UpdateModule()
    {
        _warrior ??= GetActor((uint)OID.QueensWarrior);
        _soldier ??= GetActor((uint)OID.QueensSoldier);
        _gunner ??= GetActor((uint)OID.QueensGunner);
        _knight ??= GetActor((uint)OID.QueensKnight);
    }
}
