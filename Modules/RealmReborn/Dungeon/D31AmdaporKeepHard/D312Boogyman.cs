// Ported from Veyn's bossmod (awgil/ffxiv_bossmod, BSD-3; see THIRD-PARTY-NOTICES.txt). BossmodReborn has no
// module for Amdapor Keep (Hard) at all; this is the one boss Veyn's project covers there.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Dungeon.D31AmdaporKeepHard.D312Boogyman;

public enum OID : uint
{
    Luminescence = 0xD64, // R1.000, x1 (spawn during fight)
    Boss = 0xD62, // R1.800, x1
}

public enum SID : uint
{
    Invisible = 616, // Boss->Boss, extra=0x0
    Irradiated = 617, // none->player, extra=0x0
}

// The boss turns invisible; a Luminescence orb grants Irradiated, which lets you see (and hit) him. Not
// irradiated: walk to an orb. Irradiated: stay on the invisible boss.
class InvisibilityMechanic(ModuleBase module) : ModuleComponent(module)
{
    private readonly List<Actor> invisibles = [];
    private BitMask irradiated;
    private readonly HashSet<Actor> luminescences = [];
    private WPos? lastLuminescence;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (this.invisibles.Count == 0)
            return;
        if (this.irradiated[slot])
        {
            hints.GoalZones.Add(AIHints.GoalSingleTarget(this.invisibles[0], 1f));
            return;
        }
        if (this.luminescences.Count == 0)
        {
            if (this.lastLuminescence is { } last)
                hints.GoalZones.Add(AIHints.GoalProximity(last, 0.25f, 10f));
            return;
        }
        foreach (var luminescence in this.luminescences)
        {
            hints.GoalZones.Add(AIHints.GoalProximity(luminescence.Position, 0.5f, 10f));
            hints.SetPriority(luminescence, 1);
        }
    }

    public override void OnActorCreated(Actor actor)
    {
        if ((OID)actor.OID == OID.Luminescence)
            this.luminescences.Add(actor);
    }

    public override void OnActorDestroyed(Actor actor)
    {
        if ((OID)actor.OID == OID.Luminescence)
        {
            this.lastLuminescence = actor.Position;
            this.luminescences.Remove(actor);
        }
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.Invisible)
            this.invisibles.Add(actor);
        else if (status.ID == (uint)SID.Irradiated && this.Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
            this.irradiated.Set(slot);
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.Invisible && actor == this.Module.PrimaryActor)
            this.invisibles.Remove(actor);
        else if (status.ID == (uint)SID.Irradiated && this.Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
            this.irradiated.Clear(slot);
    }
}

class D312BoogymanStates : StateMachineBuilder
{
    public D312BoogymanStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<InvisibilityMechanic>();
    }
}

[ModuleInfo(CFCID = 29u, NameID = 3274u, PrimaryActorOID = (uint)OID.Boss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from Veyn's bossmod (awgil)")]
public class D312Boogyman(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(200f, -150f), new ArenaBoundsSquare(20f));
