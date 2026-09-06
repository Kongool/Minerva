// Ported from Veyn's bossmod (awgil/ffxiv_bossmod, BSD-3; see THIRD-PARTY-NOTICES.txt). BossmodReborn has no
// module for this encounter (its Labyrinth numbering skips it); the A14 here is Veyn's, not BossmodReborn's.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A14AllaganBomb;

public enum OID : uint
{
    Boss = 0x967, // R1.500-4.500, x1
    Vassago = 0x96A, // R4.600, x3
    AllaganBalloon = 0x968, // R2.400, x0 (spawn during fight)
    AllaganNapalm = 0x969, // R2.400, x0 (spawn during fight)
}

public enum AID : uint
{
    AutoAttack = 1461, // Vassago->player, no cast, single-target
    GrimHalo = 1773, // Vassago->self, no cast, range 6+R circle
    GrimCleaver = 1772, // Vassago->location, no cast, range 1 circle
    GrimFate = 1775, // Vassago->self, no cast, range 8+R ?-degree cone
    AutoAttackBalloon = 1459, // AllaganBalloon->player, no cast, single-target
    CorruptedTail = 1771, // Vassago->player, no cast, single-target
}

class Adds(ModuleBase module) : Components.AddsMulti(module, [(uint)OID.Vassago, (uint)OID.AllaganBalloon, (uint)OID.AllaganNapalm]);

// the bomb cannot be hurt while any Vassago lives
class BossPrio(ModuleBase module) : Components.GenericInvincible(module)
{
    private readonly Actor[] one = new Actor[1];

    protected override ReadOnlySpan<Actor> ForbiddenTargets(int slot, Actor actor)
    {
        if (!this.Module.Enemies((uint)OID.Vassago).Any(v => !v.IsDead))
            return [];
        this.one[0] = this.Module.PrimaryActor;
        return this.one;
    }
}

class A14AllaganBombStates : StateMachineBuilder
{
    public A14AllaganBombStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<BossPrio>()
            .ActivateOnEnter<Adds>();
    }
}

[ModuleInfo(CFCID = 92u, NameID = 1873u, PrimaryActorOID = (uint)OID.Boss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from Veyn's bossmod (awgil)")]
public class A14AllaganBomb(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-110f, -165.6f), new ArenaBoundsCircle(33.2f));
