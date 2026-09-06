// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Quest.MSQ.Naadam;

public enum OID : uint
{
    Boss = 0x1B31,
    MagnaiTheOlder = 0x1B38, // R0.5
    StellarChuluu = 0x1B3F, // R1.8
    StellarChuluu1 = 0x1B40, // R1.8
    Grynewaht = 0x1B3A, // R0.5
    Ovoo = 0x1EA4E1,
    Helper = 0x233C,
}

public enum AID : uint
{
    ViolentEarth = 8389, // MagnaiTheOlder1->location, 3.0s cast, range 6 circle
    DispellingWind = 8394, // SaduHeavensflame->self, 3.0s cast, range 40+R width 8 rect
    Epigraph = 8339, // 1A58->self, 3.0s cast, range 45+R width 8 rect
    DiffractiveLaser = 9122, // ArmoredWeapon->location, 3.0s cast, range 5 circle
    AugmentedSuffering = 8492, // Grynewaht->self, 3.5s cast, range 6+R circle
    AugmentedUprising = 8493 // Grynewaht->self, 3.0s cast, range 8+R 120-degree cone
}

public enum SID : uint
{
    EarthenAccord = 778
}

class DiffractiveLaser(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DiffractiveLaser, 5);
class AugmentedSuffering(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AugmentedSuffering, 6.5f);
class AugmentedUprising(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AugmentedUprising, new AOEShapeCone(8.5f, 60.Degrees()));

class ViolentEarth(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ViolentEarth, 6);
class DispellingWind(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DispellingWind, new AOEShapeRect(40.5f, 4));
class Epigraph(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Epigraph, new AOEShapeRect(45, 4));

class DrawOvoo : ModuleComponent
{
    private Actor? Ovoo => World.Actors.FirstOrDefault(o => o.OID == 0x1EA4E1);

    public DrawOvoo(ModuleBase module) : base(module)
    {
        KeepOnPhaseChange = true;
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        Arena.Actor(Ovoo, Colors.Object, true);
    }
}

class ActivateOvoo(ModuleBase module) : ModuleComponent(module)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (actor.MountId == 117)
            hints.WantDismount = true;

        var beingAttacked = false;

        foreach (var e in hints.PotentialTargets)
        {
            if (e.Actor.TargetID == actor.InstanceID)
                beingAttacked = true;
            else
                e.Priority = AIHints.Enemy.PriorityForbidden;
        }

        var ovoo = World.Actors.FirstOrDefault(x => x.OID == 0x1EA4E1);
        if (!beingAttacked && (ovoo?.IsTargetable ?? false))
            hints.InteractWithTarget = ovoo;
    }
}

class ProtectOvoo(ModuleBase module) : ModuleComponent(module)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        for (var i = 0; i < hints.PotentialTargets.Count; ++i)
        {
            var e = hints.PotentialTargets[i];
            if (e.Actor.FindStatus(SID.EarthenAccord) != null)
                e.Priority = 5;
            else if (e.Actor.OID == (uint)OID.StellarChuluu)
                e.Priority = 1;
            else
                e.Priority = 0;
        }
    }
}

class ProtectSadu(ModuleBase module) : ModuleComponent(module)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var chuluu = World.Actors.Where(x => (OID)x.OID == OID.StellarChuluu1).Select(x => x.InstanceID).ToList();

        for (var i = 0; i < hints.PotentialTargets.Count; ++i)
        {
            var e = hints.PotentialTargets[i];
            if (chuluu.Contains(e.Actor.TargetID))
                e.Priority = 5;
            else if ((OID)e.Actor.OID == OID.Grynewaht)
                e.Priority = 1;
            else
                e.Priority = 0;
        }
    }
}

class OvooStates : StateMachineBuilder
{
    public OvooStates(ModuleBase module) : base(module)
    {
        bool DutyEnd() => module.World.CurrentCFCID != 246;

        TrivialPhase()
            .ActivateOnEnter<ActivateOvoo>()
            .ActivateOnEnter<DrawOvoo>()
            .Raw.Update = () => Module.World.Actors.Any(x => x.OID == (uint)OID.MagnaiTheOlder && x.IsTargetable) || DutyEnd();
        TrivialPhase(1)
            .ActivateOnEnter<ProtectOvoo>()
            .ActivateOnEnter<ActivateOvoo>()
            .ActivateOnEnter<ViolentEarth>()
            .ActivateOnEnter<DispellingWind>()
            .ActivateOnEnter<Epigraph>()
            .Raw.Update = () => Module.World.Actors.Any(x => x.OID == (uint)OID.Grynewaht && x.IsTargetable) || DutyEnd();
        TrivialPhase(2)
            .ActivateOnEnter<DiffractiveLaser>()
            .ActivateOnEnter<AugmentedSuffering>()
            .ActivateOnEnter<AugmentedUprising>()
            .ActivateOnEnter<ProtectSadu>()
            .Raw.Update = DutyEnd;
    }
}

[ModuleInfo(CFCID = 68051u, NameID = 0u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Ovoo(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(354, 296.5f), new ArenaBoundsCircle(20))
{
    protected override bool CheckPull() => Raid.Player()?.Position.InCircle(PrimaryActor.Position, 15) ?? false;

    protected override void DrawEnemies(int pcSlot, Actor pc) => Arena.Actors(World.Actors.Where(x => !x.IsAlly));
}

