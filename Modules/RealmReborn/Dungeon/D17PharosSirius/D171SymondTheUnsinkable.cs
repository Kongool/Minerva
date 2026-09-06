// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Dungeon.D17PharosSirius.D171SymondTheUnsinkable;

public enum OID : uint
{
    SymondTheUnsinkable = 0x8FB,
    Helper = 0x233C,
    ZombieWarHound = 0x8FC, // R1.650, x?
    CorruptedCrystal = 0x1B2, //1B2/Corrupted Crystal/
    VoidZone1 = 0x1E9058, // R0.500, x?, EventObj type : two voidzone growth first 9
    VoidZone2 = 0x1E9059, // R0.500, x?, EventObj type : two voidzone growth second 12
    VoidZone3 = 0x1E905A, // R0.500, x?, EventObj type : two voidzone growth third 15
}

public enum AID : uint
{
    AutoAttack = 870, // SymondTheUnsinkable/ZombieWarHound->player, no cast, single-target
    PiercingThrust = 1527, // SymondTheUnsinkable->player, no cast, single-target
    CorruptingShot = 1498, // SymondTheUnsinkable->self, 2.2s cast, range 30+R width 3 rect
    CorruptingSpit = 1499, // ZombieWarHound->location, 1.0s cast, range 6 circle
    CrystallineShot = 1501, // SymondTheUnsinkable->self, 2.5s cast, single-target
    CrystallineShower = 1542, // CorruptedCrystal->location, 3.5s cast, range 9 circle
    GigaSlash = 1502, // SymondTheUnsinkable->self, 4.0s cast, range 20+R circle
    CorruptingBurst = 1500, // CorruptedCrystal->player, no cast, range 6 circle
}

public enum SID : uint
{
    CorruptedCrystal = 374, // ZombieWarHound->player, extra=0x1/0x2/0x3
    VulnerabilityUp = 202, // CorruptedCrystal->player, extra=0x1/0x2
}

sealed class CorruptingShot(ModuleBase module)
    : Components.SimpleAOEs(module, (uint)AID.CorruptingShot, new AOEShapeRect(50f, 1.5f));

sealed class WarHoundAdds(ModuleBase module) : Components.Adds(module, (uint)OID.ZombieWarHound, 1);
sealed class CorruptingSpit(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CorruptingSpit, 6);

sealed class CrystallineShower(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CrystallineShower, 9);
// These are the voidzones that grow after crystalline shower lands.
sealed class VoidZone1(ModuleBase module) : Components.Voidzone(module, 9, m => m.Enemies((uint)OID.VoidZone1));
sealed class VoidZone2(ModuleBase module) : Components.Voidzone(module, 12, m => m.Enemies((uint)OID.VoidZone2));
sealed class VoidZone3(ModuleBase module) : Components.Voidzone(module, 15, m => m.Enemies((uint)OID.VoidZone3));

sealed class GigaSlash(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GigaSlash, 20);
sealed class GigaSlashKB(ModuleBase module)
    : Components.SimpleKnockbacks(module, (uint)AID.GigaSlash, 10f, shape: new AOEShapeCircle(20f));

/*
 * If you reach 3 stacks of Corrupting Crystal, run away from everyone so the explosion doesn't hit them. Since you
 * are already at 3 stacks, you can run into the Crystalline Shot puddles if necessary.
 */
sealed class CorruptingBurst(ModuleBase module) : Components.GenericBaitAway(module, centerAtTarget: true)
{
    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.CorruptedCrystal && status.Extra is 0x3)
        {
            CurrentBaits.Add(new(actor, actor, new AOEShapeCircle(6), World.FutureTime(2.5d)));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.CorruptingBurst)
            CurrentBaits.Clear();
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        base.AddHints(slot, actor, hints);
        if (IsBaitTarget(actor))
        {
            hints.Add("Bait away!");
        }
    }
}

[SkipLocalsInit]
sealed class D171SymondTheUnsinkableStates : StateMachineBuilder
{
    public D171SymondTheUnsinkableStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<CorruptingShot>()
            .ActivateOnEnter<WarHoundAdds>()
            .ActivateOnEnter<CorruptingSpit>()
            .ActivateOnEnter<CrystallineShower>()
            .ActivateOnEnter<VoidZone1>()
            .ActivateOnEnter<VoidZone2>()
            .ActivateOnEnter<VoidZone3>()
            .ActivateOnEnter<GigaSlash>()
            .ActivateOnEnter<GigaSlashKB>()
            .ActivateOnEnter<CorruptingBurst>();
    }
}

[ModuleInfo(CFCID = 17u, NameID = 2259u, PrimaryActorOID = (uint)OID.SymondTheUnsinkable, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "wen (ported from BMR)")]
[SkipLocalsInit]
// technically arena center is (42, 30, -56) if you want to visit in hyperborea.
public sealed class D171SymondTheUnsinkable(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(42f, -56f), new ArenaBoundsCircle(20f));
