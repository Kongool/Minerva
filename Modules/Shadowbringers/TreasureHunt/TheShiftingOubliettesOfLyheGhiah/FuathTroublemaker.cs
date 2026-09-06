// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.TreasureHunt.ShiftingOubliettesOfLyheGhiah.FuathTroublemaker;

public enum OID : uint
{
    FuathTroublemaker = 0x302F, //R=3.25
    FuathTrickster = 0x3033, // R0.750
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 21733, // FuathTroublemaker->player, no cast, single-target

    FrigidNeedleVisual = 21739, // FuathTroublemaker->self, 3.0s cast, single-target
    FrigidNeedle = 21740, // Helper->self, 3.0s cast, range 40 width 5 cross
    SpittleVisual = 21735, // FuathTroublemaker->self, no cast, single-target
    Spittle = 21736, // Helper->location, 4.0s cast, range 8 circle
    CroakingChorus = 21738, // FuathTroublemaker->self, 3.0s cast, single-target, calls adds
    ToyHammer = 21734, // FuathTroublemaker->player, 4.0s cast, single-target
    Hydrocannon = 21737, // FuathTroublemaker->players, 5.0s cast, range 6 circle

    Telega = 9630 // FuathTrickster->self, no cast, single-target, bonus adds disappear
}

sealed class CroakingChorus(ModuleBase module) : Components.CastHint(module, (uint)AID.CroakingChorus, "Calls adds");
sealed class FrigidNeedle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FrigidNeedle, new AOEShapeCross(40f, 2.5f));
sealed class Spittle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Spittle, 8f);
sealed class ToyHammer(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.ToyHammer);
sealed class Hydrocannon(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.Hydrocannon, 6f, 8, 8);

sealed class FuathTroublemakerStates : StateMachineBuilder
{
    public FuathTroublemakerStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<CroakingChorus>()
            .ActivateOnEnter<ToyHammer>()
            .ActivateOnEnter<Spittle>()
            .ActivateOnEnter<Hydrocannon>()
            .ActivateOnEnter<FrigidNeedle>()
            .Raw.Update = () => AllDeadOrDestroyed(FuathTroublemaker.All);
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 745u, CFCID = 745u, NameID = 9786u, PrimaryActorOID = (uint)OID.FuathTroublemaker, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class FuathTroublemaker : THTemplate
{
    public FuathTroublemaker(WorldState ws, Actor primary) : base(ws, primary)
    {
        tricksters = Enemies((uint)OID.FuathTrickster);
    }

    private readonly List<Actor> tricksters;
    public static readonly uint[] All = [(uint)OID.FuathTroublemaker, (uint)OID.FuathTrickster];

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(tricksters, Colors.Vulnerable);
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.FuathTrickster => 1,
                _ => 0
            };
        }
    }
}
