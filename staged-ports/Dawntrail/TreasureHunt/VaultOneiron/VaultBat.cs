// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.TreasureHunt.VaultOneiron.VaultBat;

public enum OID : uint
{
    VaultGemkeeper = 0x489E, // R0.9
    VaultAcrobat = 0x489F, // R1.2
    VaultBat = 0x48A0, // R1.6

    VaultOnion = 0x48B9, // R0.84, icon 1, needs to be killed in order from 1 to 5 for maximum rewards
    VaultEggplant = 0x48BA, // R0.84, icon 2, needs to be killed in order from 1 to 5 for maximum rewards
    VaultGarlic = 0x48BB, // R0.84, icon 3, needs to be killed in order from 1 to 5 for maximum rewards
    VaultTomato = 0x48BC, // R0.84, icon 4, needs to be killed in order from 1 to 5 for maximum rewards
    VaultQueen = 0x48BD, // R0.84, icon 5, needs to be killed in order from 1 to 5 for maximum rewards
    GoldyCat = 0x48B7, // R1.87
    Vaultkeeper = 0x48B8 // R2.0
}

public enum AID : uint
{
    AutoAttack1 = 872, // VaultGemkeeper/VaultAcrobat/VaultBat->player, no cast, single-target

    FastBoulder = 43627, // VaultGemkeeper->location, 3.0s cast, range 3 circle
    Catchoo = 43629, // VaultAcrobat->self, 4.0s cast, range 40 width 8 rect
    ColdSpot = 43631, // VaultBat->location, 3.0s cast, range 6 circle
    PhantomLight = 43630, // VaultBat->self/player, 5.0s cast, range 60 width 8 rect, tankbuster
    ChillingDraft = 43632, // VaultBat->self, 4.0s cast, range 40 45-degree cone
    ParanormalActivity = 43633, // VaultBat->self, 5.0s cast, range 100 circle, raidwide
    Acrocatics = 43628, // VaultAcrobat->self, 3.0s cast, range 6 circle

    AutoAttack2 = 871, // Vaultkeeper->player, no cast, single-target
    Thunderlance = 43727, // Vaultkeeper->self, 3.5s cast, range 20 width 3 rect
    LanceSwing = 43726, // Vaultkeeper->self, 4.0s cast, range 8 circle
    TearyTwirl = 32301, // VaultOnion->self, 3.5s cast, range 7 circle
    PluckAndPrune = 32302, // VaultEggplant->self, 3.5s cast, range 7 circle
    PungentPirouette = 32303, // VaultGarlic->self, 3.5s cast, range 7 circle
    HeirloomScream = 32304, // VaultTomato->self, 3.5s cast, range 7 circle
    Pollen = 32305, // VaultQueen->self, 3.5s cast, range 7 circle
    Telega = 9630 // BonusAdds->self, no cast, single-target, bonus adds disappear
}

sealed class PhantomLight(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.PhantomLight, new AOEShapeRect(60f, 4f), tankbuster: true, endsOnCastEvent: true);
sealed class FastBoulder(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FastBoulder, 3f);
sealed class ChillingDraft(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ChillingDraft, new AOEShapeCone(40f, 22.5f.Degrees()));
sealed class AcrocaticsColdSpot(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.Acrocatics, (uint)AID.ColdSpot], 6f);
sealed class Catchoo(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Catchoo, new AOEShapeRect(40f, 4f));
sealed class ParanormalActivity(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ParanormalActivity);

sealed class Thunderlance(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Thunderlance, new AOEShapeRect(20f, 1.5f));
sealed class LanceSwing(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LanceSwing, 8f);
sealed class MandragoraAOEs(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.PluckAndPrune, (uint)AID.TearyTwirl,
(uint)AID.HeirloomScream, (uint)AID.PungentPirouette, (uint)AID.Pollen], 7f);

sealed class VaultBatStates : StateMachineBuilder
{
    public VaultBatStates(VaultBat module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<PhantomLight>()
            .ActivateOnEnter<FastBoulder>()
            .ActivateOnEnter<ChillingDraft>()
            .ActivateOnEnter<AcrocaticsColdSpot>()
            .ActivateOnEnter<Catchoo>()
            .ActivateOnEnter<ParanormalActivity>()
            .ActivateOnEnter<Thunderlance>()
            .ActivateOnEnter<LanceSwing>()
            .ActivateOnEnter<MandragoraAOEs>()
            .Raw.Update = () => AllDestroyed(VaultBat.All) && (module.BossBat?.IsDeadOrDestroyed ?? true);
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1060u, CFCID = 1060u, NameID = 13992u, PrimaryActorOID = (uint)OID.VaultGemkeeper, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class VaultBat(WorldState ws, Actor primary) : SharedBoundsBoss(ws, primary)
{
    private static readonly uint[] bonusAdds = [(uint)OID.VaultOnion, (uint)OID.VaultTomato, (uint)OID.VaultGarlic, (uint)OID.VaultEggplant, (uint)OID.VaultQueen, (uint)OID.Vaultkeeper, (uint)OID.GoldyCat];
    private static readonly uint[] trash = [(uint)OID.VaultGemkeeper, (uint)OID.VaultAcrobat];
    public static readonly uint[] All = [.. trash, .. bonusAdds];

    public Actor? BossBat;

    protected override void UpdateModule()
    {
        BossBat ??= GetActor((uint)OID.VaultBat);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(BossBat);
        var m = this;
        Arena.Actors(m, trash);
        Arena.Actors(m, bonusAdds, Colors.Vulnerable);
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.VaultOnion => 6,
                (uint)OID.VaultEggplant => 5,
                (uint)OID.VaultGarlic => 4,
                (uint)OID.VaultTomato => 3,
                (uint)OID.VaultQueen or (uint)OID.VaultBat => 2,
                (uint)OID.Vaultkeeper => 1,
                _ => 0
            };
        }
    }
}
