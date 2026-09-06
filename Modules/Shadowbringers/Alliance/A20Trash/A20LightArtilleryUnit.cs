// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A20LightArtilleryUnit;

public enum OID : uint
{
    Boss = 0x2EE4, // R4.5, x3
    YorhaCloseCombatUnitBlade = 0x2EE7, // R0.5, x3
    YorhaCloseCombatUnitSpear = 0x2EE5, // R0.5, x3
    YorhaCloseCombatUnitMartial = 0x2EE6, // R0.5, x3
    FireVoidzone = 0x1E8D9B, // R0.5
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target

    SpearJab = 21405, // YorhaCloseCombatUnitSpear->player, no cast, single-target
    StrikingBlow = 21409, // YorhaCloseCombatUnitMartial->player, no cast, single-target
    BladeFlurry = 21413, // YorhaCloseCombatUnitBlade->player, no cast, single-target
    SpearSequence = 21406, // YorhaCloseCombatUnitSpear->player, no cast, single-target
    DancingBlade = 21414, // YorhaCloseCombatUnitBlade->player, no cast, single-target
    SequentialBlows = 21410, // YorhaCloseCombatUnitMartial->player, no cast, single-target
    SeamlessForm = 21407, // YorhaCloseCombatUnitSpear->player, no cast, single-target
    PalmQuake = 21411, // YorhaCloseCombatUnitMartial->player, no cast, single-target
    BalancedEdge = 21415, // YorhaCloseCombatUnitBlade->player, no cast, single-target
    PointedSpearing = 21408, // YorhaCloseCombatUnitSpear->player, no cast, single-target
    TopplingStrike = 21412, // YorhaCloseCombatUnitMartial->player, no cast, single-target
    WhirlingAssault = 21416, // YorhaCloseCombatUnitBlade->player, no cast, single-target
    WeightedBlade = 21417, // YorhaCloseCombatUnitBlade->player, no cast, single-target

    ManeuverMartialArm = 21011, // Boss->player, 5.0s cast, single-target, tankbuster
    ManeuverLongBarreledLaser = 21010, // Boss->self, 4.0s cast, range 70 width 8 rect
    AuthorizationNoRestrictions = 21006, // Boss->self, 2.0s cast, single-target
    SurfaceMissileImpact = 21007, // Helper->location, 4.0s cast, range 6 circle
    HomingMissileImpact = 21008, // Helper->players, 6.0s cast, range 6 circle, spread
    BossDefeatedVisual = 21012, // Boss->self, no cast, single-target
    InitiateSelfDestruct = 21013, // Boss->self, no cast, single-target
    ManeuverVoltArray = 21009 // Boss->self, 5.0s cast, range 100 circle, interruptible
}

sealed class HomingMissileImpact(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.HomingMissileImpact, 6f)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        if (IsBaitTarget(actor))
        {
            hints.AddForbiddenZone(new SDRect(Center, new WDir(default, 1f), 23f, 23f, 23f));
        }
    }
}

sealed class FireVoidzone(ModuleBase module) : Components.Voidzone(module, 6f, GetVoidzones) // both Homing Missile Impact and Surface Missile Impact spawn these voidzones
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.FireVoidzone);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (z.EventState != 7)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }
}
sealed class VoltArray(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.ManeuverVoltArray, showNameInHint: true);
sealed class LongBarreledLaser(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ManeuverLongBarreledLaser, new AOEShapeRect(70f, 4f));
sealed class SurfaceMissileImpact(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SurfaceMissileImpact, 6f);
sealed class ManeuverMartialArm(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.ManeuverMartialArm);
sealed class InitiateSelfDestruct(ModuleBase module) : Components.CastCounter(module, (uint)AID.InitiateSelfDestruct);

sealed class A20LightArtilleryUnitStates : StateMachineBuilder
{
    private InitiateSelfDestruct? _count;

    public A20LightArtilleryUnitStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<FireVoidzone>()
            .ActivateOnEnter<SurfaceMissileImpact>()
            .ActivateOnEnter<HomingMissileImpact>()
            .ActivateOnEnter<LongBarreledLaser>()
            .ActivateOnEnter<VoltArray>()
            .ActivateOnEnter<ManeuverMartialArm>()
            .ActivateOnEnter<InitiateSelfDestruct>()
            .Raw.Update = () =>
            {
                _count ??= module.FindComponent<InitiateSelfDestruct>();
                if (_count!.NumCasts == 3)
                {
                    return true;
                }
                var enemies = module.Enemies((uint)OID.Boss);
                var count = enemies.Count;
                for (var i = 0; i < count; ++i)
                {
                    var e = enemies[i];
                    if (!e.IsDestroyed)
                    {
                        return false;
                    }
                }
                return true;
            };
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 736u, CFCID = 736u, NameID = 9652u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class A20LightArtilleryUnit(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-230f, -15f), new ArenaBoundsSquare(24.5f))
{
    private static readonly uint[] all = [(uint)OID.Boss, (uint)OID.YorhaCloseCombatUnitMartial, (uint)OID.YorhaCloseCombatUnitBlade, (uint)OID.YorhaCloseCombatUnitSpear];

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(this, all);
    }
}
