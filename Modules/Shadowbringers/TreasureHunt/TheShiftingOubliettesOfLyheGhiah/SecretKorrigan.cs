// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.TreasureHunt.ShiftingOubliettesOfLyheGhiah.SecretKorrigan;

public enum OID : uint
{
    SecretKorrigan = 0x3022, //R=2.85
    SecretMandragora = 0x301C, //R=0.84
    SecretQueen = 0x3021, // R0.84, icon 5, needs to be killed in order from 1 to 5 for maximum rewards
    SecretGarlic = 0x301F, // R0.84, icon 3, needs to be killed in order from 1 to 5 for maximum rewards
    SecretTomato = 0x3020, // R0.84, icon 4, needs to be killed in order from 1 to 5 for maximum rewards
    SecretOnion = 0x301D, // R0.84, icon 1, needs to be killed in order from 1 to 5 for maximum rewards
    SecretEgg = 0x301E, // R0.84, icon 2, needs to be killed in order from 1 to 5 for maximum rewards
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 872, // SecretKorrigan/SecretMandragora/Mandragoras->player, no cast, single-target

    Hypnotize = 21674, // SecretKorrigan->self, 4.0s cast, range 40 circle
    LeafDagger = 21675, // SecretKorrigan->location, 2.5s cast, range 3 circle
    SaibaiMandragora = 21676, // SecretKorrigan->self, 3.0s cast, single-target
    Ram = 21673, // SecretKorrigan->player, 3.0s cast, single-target

    Pollen = 6452, // SecretQueen->self, 3.5s cast, range 6+R circle
    TearyTwirl = 6448, // SecretOnion->self, 3.5s cast, range 6+R circle
    HeirloomScream = 6451, // SecretTomato->self, 3.5s cast, range 6+R circle
    PluckAndPrune = 6449, // SecretEgg->self, 3.5s cast, range 6+R circle
    PungentPirouette = 6450, // SecretGarlic->self, 3.5s cast, range 6+R circle
    Telega = 9630 // Mandragoras->self, no cast, single-target, bonus adds disappear
}

sealed class Hypnotize(ModuleBase module) : Components.CastGaze(module, (uint)AID.Hypnotize);
sealed class Ram(ModuleBase module) : Components.SingleTargetDelayableCast(module, (uint)AID.Ram);
sealed class SaibaiMandragora(ModuleBase module) : Components.CastHint(module, (uint)AID.SaibaiMandragora, "Calls adds");
sealed class LeafDagger(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LeafDagger, 3f);

sealed class MandragoraAOEs(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.PluckAndPrune, (uint)AID.TearyTwirl,
(uint)AID.HeirloomScream, (uint)AID.PungentPirouette, (uint)AID.Pollen], 6.84f);

sealed class SecretKorriganStates : StateMachineBuilder
{
    public SecretKorriganStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Hypnotize>()
            .ActivateOnEnter<LeafDagger>()
            .ActivateOnEnter<SaibaiMandragora>()
            .ActivateOnEnter<Ram>()
            .ActivateOnEnter<MandragoraAOEs>()
            .Raw.Update = () => AllDeadOrDestroyed(SecretKorrigan.All);
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 745u, CFCID = 745u, NameID = 9806u, PrimaryActorOID = (uint)OID.SecretKorrigan, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class SecretKorrigan : THTemplate
{
    public SecretKorrigan(WorldState ws, Actor primary) : base(ws, primary)
    {
        secretMandragoras = Enemies((uint)OID.SecretMandragora);
    }
    private readonly List<Actor> secretMandragoras;

    private static readonly uint[] bonusAdds = [(uint)OID.SecretEgg, (uint)OID.SecretGarlic, (uint)OID.SecretOnion, (uint)OID.SecretTomato,
    (uint)OID.SecretQueen];
    public static readonly uint[] All = [(uint)OID.SecretKorrigan, (uint)OID.SecretMandragora, .. bonusAdds];

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(secretMandragoras);
        Arena.Actors(this, bonusAdds, Colors.Vulnerable);
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.SecretOnion => 6,
                (uint)OID.SecretEgg => 5,
                (uint)OID.SecretGarlic => 4,
                (uint)OID.SecretTomato => 3,
                (uint)OID.SecretQueen => 2,
                (uint)OID.SecretMandragora => 1,
                _ => 0
            };
        }
    }
}
