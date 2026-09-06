// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Quest.Job.Viper.FangsOfTheViper;

public enum OID : uint
{
    Boss = 0x429F, // R0.5
    FawningPeiste = 0x42A1, // R2.34
    FawningRaptor = 0x42A3, // R1.82
    FawningWivre = 0x42A2, // R3.9
    WanderingGowrow = 0x42A0, // R4.0
    Helper = 0x43A3
}

public enum AID : uint
{
    AutoAttack1 = 6497, // FawningPeiste/FawningRaptor/WanderingGowrow->player, no cast, single-target
    AutoAttack2 = 6498, // FawningWivre->player, no cast, single-target
    Visual1 = 37699, // Helper->self, no cast, single-target
    Visual2 = 37698, // Helper->self, no cast, single-target
    Visual3 = 37697, // WanderingGowrow->self, no cast, single-target

    BurningCyclone = 37703, // FawningPeiste->self, 5.0s cast, range 6 120-degree cone
    FoulBreath = 39263, // FawningRaptor->self, 5.0s cast, range 7 90-degree cone
    BrowHorn = 37704, // FawningWivre->self, 5.0s cast, range 6 width 4 rect
    Firebreathe = 37700, // WanderingGowrow->self, 5.0s cast, range 60 90-degree cone
    RightSidedShockwave = 37701, // WanderingGowrow->self, 4.0s cast, range 20 180-degree cone
    LeftSidedShockwave = 37702 // WanderingGowrow->self, 4.0s cast, range 20 180-degree cone
}

sealed class BurningCyclone(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BurningCyclone, new AOEShapeCone(6f, 60f.Degrees()));
sealed class FoulBreath(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FoulBreath, new AOEShapeCone(7f, 45f.Degrees()));
sealed class BrowHorn(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BrowHorn, new AOEShapeRect(6f, 2f));
sealed class Firebreathe(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Firebreathe, new AOEShapeCone(60f, 45f.Degrees()));
sealed class Shockwave(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.RightSidedShockwave, (uint)AID.LeftSidedShockwave], new AOEShapeCone(20f, 90f.Degrees()));

sealed class FangsOfTheViperStates : StateMachineBuilder
{
    public FangsOfTheViperStates(FangsOfTheViper module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<BurningCyclone>()
            .ActivateOnEnter<FoulBreath>()
            .ActivateOnEnter<BrowHorn>()
            .ActivateOnEnter<Firebreathe>()
            .ActivateOnEnter<Shockwave>()
            .Raw.Update = () => (module.BossGowrow?.IsDead ?? false) || module.PrimaryActor.IsDeadOrDestroyed;
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, GroupID = 70385u, CFCID = 70385u, NameID = 12825u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class FangsOfTheViper(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(264f, 480f), 19.5f, 20)]);
    private static readonly uint[] all = [(uint)OID.FawningWivre, (uint)OID.FawningPeiste, (uint)OID.FawningRaptor, (uint)OID.WanderingGowrow];

    public Actor? BossGowrow;

    protected override void UpdateModule()
    {
        BossGowrow ??= GetActor((uint)OID.WanderingGowrow);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(this, all);
    }

    protected override bool CheckPull() => Raid.Player()!.InCombat;
}
