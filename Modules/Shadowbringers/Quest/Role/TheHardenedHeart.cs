// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Quest.Role.TheHardenedHeart;

public enum OID : uint
{
    Boss = 0x2919,
    Helper = 0x233C,
}

public enum AID : uint
{
    SanctifiedFireIII = 18090, // 2922/2923->players/2917/2915/2914, 8.0s cast, range 6 circle
    TwistedTalent1 = 13637, // Helper->player/2916/2914/2915/2917, 5.0s cast, range 5 circle
    AbyssalCharge1 = 15539, // 25BB->self, 3.0s cast, range 40+R width 4 rect
    DeadlyBite = 15543, // 291D/291C->player/2914, no cast, single-target
    RustingClaw = 15540, // 291B/291A->self, 5.0s cast, range 8+R ?-degree cone
}

class SanctifiedFireIII(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.SanctifiedFireIII, 6f)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == StackAction && World.Actors.Find(spell.TargetID) is Actor stackTarget && stackTarget.OID == 0x2915)
            AddStack(stackTarget, Module.CastFinishAt(spell));
    }
}

class TwistedTalent(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.TwistedTalent1, 5f);
class AbyssalCharge(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AbyssalCharge1, new AOEShapeRect(40f, 2f));

class TankbusterTether(ModuleBase module) : ModuleComponent(module)
{
    private record class Tether(Actor Source, Actor Target, DateTime Activation);
    private Tether? DwarfTether;

    private bool Danger => DwarfTether?.Target.OID == 0x2917;

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID == 84 && World.Actors.Find(tether.Target) is Actor target)
            DwarfTether = new(source, target, DwarfTether?.Activation ?? World.FutureTime(10));
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (DwarfTether?.Target.OID == 0x2917)
            hints.AddForbiddenZone(new SDInvertedRect(DwarfTether.Source.Position, DwarfTether.Target.Position, 1), DwarfTether.Activation);
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (Danger)
            hints.Add("Take tether!");
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        if (DwarfTether is Tether t)
            Arena.AddLine(t.Source.Position, t.Target.Position, Colors.Danger);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.DeadlyBite)
            DwarfTether = null;
    }
}

class BrandenAI(ModuleBase module) : ModuleComponent(module)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var h = hints.PotentialTargets[i];
            h.Priority = h.Actor.FindStatus(775u) == null ? (h.Actor.TargetID == actor.InstanceID ? 2 : 1) : 0;
            if (h.Actor.OID is not (0x291D or 0x2919) && h.Actor.CastInfo == null)
            {
                h.DesiredPosition = Center;
                if (h.Actor.TargetID == actor.InstanceID && !h.Actor.Position.InCircle(Center, 5))
                    hints.ForcedTarget = h.Actor;
            }
        }
    }
}

class RustingClaw(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RustingClaw, new AOEShapeCone(10.3f, 45.Degrees()));

class TadricTheVaingloriousStates : StateMachineBuilder
{
    public TadricTheVaingloriousStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<BrandenAI>()
            .ActivateOnEnter<SanctifiedFireIII>()
            .ActivateOnEnter<TwistedTalent>()
            .ActivateOnEnter<AbyssalCharge>()
            .ActivateOnEnter<TankbusterTether>()
            .ActivateOnEnter<RustingClaw>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 68783u, NameID = 8339u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class TadricTheVainglorious(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsSquare(20f))
{
    protected override void DrawEnemies(int pcSlot, Actor pc) => Arena.Actors(World.Actors.Where(x => !x.IsAlly));
}
