// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.CriticalEngagement.CE13KillItWithFire;

public enum OID : uint
{
    Boss = 0x2E2F, // R2.25
    RottenMandragora = 0x2E30, // R1.05
    Pheromones = 0x2E31, // R1.5
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 6499, // Boss->player, no cast, single-target
    Teleport = 20513, // Boss->location, no cast, single-target, teleport

    HarvestFestival = 20511, // Boss->self, 4.0s cast, single-target, visual (summon mandragoras)
    PheromonesVisual1 = 20512, // RottenMandragora->self, no cast, single-target, visual (???)
    PheromonesVisual2 = 20514, // RottenMandragora->location, no cast, single-target, visual (???)
    RancidPheromones = 20515, // RottenMandragora->self, no cast, single-target, visual (???)
    Heartbreak = 20516, // Pheromones->self, no cast, range 4 circle when pheromone is touched
    DeadLeaves = 20517, // Boss->self, 4.0s cast, range 30 circle, visual (recolors)
    TenderAnaphylaxis = 20518, // Helper->self, 4.0s cast, range 30 90-degree cone
    JealousAnaphylaxis = 20519, // Helper->self, 4.0s cast, range 30 90-degree cone
    AnaphylacticShock = 20520, // Helper->self, 4.0s cast, range 30 width 2 rect aoe (borders)
    SplashBomb = 20521, // Boss->self, 4.0s cast, single-target, visual (puddles)
    SplashBombAOE = 20522, // Helper->self, 4.0s cast, range 6 circle puddle
    SplashGrenade = 20523, // Boss->self, 5.0s cast, single-target, visual (stack)
    SplashGrenadeAOE = 20524, // Helper->players, 5.0s cast, range 6 circle stack
    PlayfulBreeze = 20525, // Boss->self, 4.0s cast, single-target, visual (raidwide)
    PlayfulBreezeAOE = 20526, // Helper->self, 4.0s cast, range 60 circle raidwide
    Budbutt = 20527 // Boss->player, 4.0s cast, single-target, tankbuster
}

public enum SID : uint
{
    TenderAnaphylaxis = 2301, // Helper->player, extra=0x0
    JealousAnaphylaxis = 2302 // Helper->player, extra=0x0
}

sealed class Pheromones(ModuleBase module) : Components.Voidzone(module, 4f, GetVoidzones, 3f)
{
    private static List<Actor> GetVoidzones(ModuleBase module) => module.Enemies((uint)OID.Pheromones);
}

/// <summary>
/// Dead Leaves: the floor turns into four quadrants, two Tender and two Jealous, and whichever one you are standing on
/// when it resolves gives you its colour. Take the other colour next time and it barely scratches; take the same one
/// again and it hits hard. So each player is shown only the quadrants of the colour they already carry -- those are the
/// ones to leave -- and nothing at all before their first colour, when any quadrant will do.
///
/// <para>This is BossmodReborn's model, restored. It was replaced on 2026-09-17 by drawing every cone for everybody, on
/// two readings of that day's recording that the 2026-09-25 pull disproves. Both are worth recording, because both
/// look right from a distance:</para>
///
/// <para><b>"The colour expires before the next round, so it decides nothing."</b> It does not expire; it is replaced.
/// A colour is held until the next round swaps it, and the swap and the loss happen on the same tick -- which is what
/// read as a nine-second timer, since rounds one and two are nine seconds apart. Rosa and Xia then held Tender for 70.4
/// seconds, round two to round three, and Saar held it for 80.4 across a round in which it got no new colour. Everyone
/// still carries their colour through the whole four-second cast of the next round, which is the warning.</para>
///
/// <para><b>"The cones are sixty degrees wide."</b> They are ninety: quadrants at plus and minus 45 and 135, covering
/// the floor with nothing between them. Colours were handed out at 32 to 37 degrees off a cone's centre in every round,
/// past the thirty a sixty-degree cone allows. What was measured on 09-17 was damage, and a player who switches colour
/// takes none, so the hits that were counted stopped short of the real edge.</para>
///
/// <para>The cost of the 09-17 version was two deaths on 2026-09-25. With every quadrant drawn the whole disc read as
/// danger, and the dodge could not tell a harmless quadrant of the other colour from the border lines between them
/// (Anaphylactic Shock, 33 to 38 thousand). Korha died at round two to a border plus Tender after Tender; Saar at round
/// three, the same way. Same colour again hit for 11.5 to 17 thousand; the other colour for none, or about three.</para>
/// </summary>
sealed class DeadLeaves(ModuleBase module) : Components.GenericAOEs(module, default, "Go to different color!")
{
    private static readonly AOEShapeCone Quadrant = new(30f, 45f.Degrees());

    private BitMask tender;
    private BitMask jealous;
    private readonly List<AOEInstance> tenderAOEs = [];
    private readonly List<AOEInstance> jealousAOEs = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
        => CollectionsMarshal.AsSpan(this.tender[slot] ? this.tenderAOEs : this.jealous[slot] ? this.jealousAOEs : []);

    public override void OnStatusGain(Actor actor, ref ActorStatus status) => this.Mark(actor, status.ID, true);

    public override void OnStatusLose(Actor actor, ref ActorStatus status) => this.Mark(actor, status.ID, false);

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        var list = cast.Action.ID switch
        {
            (uint)AID.TenderAnaphylaxis => this.tenderAOEs,
            (uint)AID.JealousAnaphylaxis => this.jealousAOEs,
            _ => null,
        };
        var origin = cast.LocXZ != default ? cast.LocXZ : caster.Position;
        list?.Add(new(Quadrant, origin, cast.Rotation, Module.CastFinishAt(cast)));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID is (uint)AID.TenderAnaphylaxis or (uint)AID.JealousAnaphylaxis)
        {
            this.tenderAOEs.Clear();
            this.jealousAOEs.Clear();
        }
    }

    private void Mark(Actor actor, uint status, bool held)
    {
        var slot = Raid.FindSlot(actor.InstanceID);
        if (slot < 0)
            return;
        if (status == (uint)SID.TenderAnaphylaxis)
            this.tender[slot] = held;
        else if (status == (uint)SID.JealousAnaphylaxis)
            this.jealous[slot] = held;
    }
}

sealed class AnaphylacticShock(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AnaphylacticShock, new AOEShapeRect(30f, 1f));
sealed class SplashBomb(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SplashBombAOE, 6f);
sealed class SplashGrenade(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.SplashGrenadeAOE, 6f, 8);
sealed class PlayfulBreeze(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.PlayfulBreeze);
sealed class Budbutt(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Budbutt);

sealed class CE13KillItWithFireStates : StateMachineBuilder
{
    public CE13KillItWithFireStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Pheromones>()
            .ActivateOnEnter<DeadLeaves>()
            .ActivateOnEnter<AnaphylacticShock>()
            .ActivateOnEnter<SplashBomb>()
            .ActivateOnEnter<SplashGrenade>()
            .ActivateOnEnter<PlayfulBreeze>()
            .ActivateOnEnter<Budbutt>();
    }
}

[ModuleInfo(Group = ModuleGroup.CriticalEngagement, CFCID = 735u, NameID = 1u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")] // bnpcname=9391
public sealed class CE13KillItWithFire(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(-90f, 700f), 29.5f, 32)]);

    protected override bool CheckPull() => base.CheckPull() && Raid.Player()!.Position.InCircle(Center, 30f);
}
