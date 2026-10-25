// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.CrucibleOfTheUnbroken.SecondBoard.VoidmancerPiece;

public enum OID : uint
{
    VoidmancerPiece = 0x4C5C,
    ZombiePiece = 0x4C5D, // R0.750, x20
    Malady = 0x4C5E, // R1.000, x0 (spawn during fight)
    Helper = 0x233C,
}

public enum AID : uint
{
    AutoAttackWater = 50793, // VoidmancerPiece->player, no cast, single-target
    AutoAttackZombie = 48251, // 4C5D->player, no cast, single-target
    DeathDriveBait = 48181, // VoidmancerPiece->self, 7.0s cast, single-target
    DeathDrive = 48182, // Helper->location, 4.0s cast, range 10 circle
    DarkOrbBoss = 48185, // VoidmancerPiece->self, 6.5+1.5s cast, single-target
    DarkOrb = 48186, // Helper->self, 8.0s cast, range 18 circle
    EvilMist = 48183, // VoidmancerPiece->self, 4.0s cast, range 60 circle
    Necropurge = 48184, // 4C5E->self, 1.0s cast, range 8 circle
    Mindjack = 48187, // VoidmancerPiece->self, 4.0s cast, range 100 circle
}

public enum SID : uint
{
    WitsEnd = 5424, // 4C5D/4C5E->player, extra=0x1/0x2/0x4/0x5/0x6/0x7/0x8 - reaching 12 stacks will cause confuse on the player
    ForwardMarch = 2161, // VoidmancerPiece->player; Korha 2026-10-24 (BMR has only the two turns)
    AboutFace = 2162, // VoidmancerPiece->player; Korha 2026-10-22, twice
    LeftFace = 2163, // VoidmancerPiece->player, extra=0x0
    RightFace = 2164, // VoidmancerPiece->player, extra=0x0
    ForcedMarch = 1257, // VoidmancerPiece->player, extra=0x4
}

public enum IconID : uint
{
    DeathDrive = 707, // player->self
}

public enum TetherID : uint
{
    ZombieTether = 17, // 4C5D->player
}

sealed class DeathDrive(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DeathDrive, 10.0f);
sealed class DarkOrb(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DarkOrb, 18.0f);
sealed class Malady(ModuleBase module) : Components.Voidzone(module, 8.0f, module => module.Enemies((uint)OID.Malady).Where(z => z.EventState != 7));

sealed class DeathDriveBait(ModuleBase module) : Components.BaitAwayIcon(module, 10.0f, (uint)IconID.DeathDrive, (uint)AID.DeathDriveBait, 7.5f)
{
    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        base.DrawArenaForeground(pcSlot, pc);

        if (!IsBaitTarget(pc) || CurrentBaits.Count == 0)
        {
            return;
        }

        var bait = CurrentBaits[0];
        var baitRadius = ((AOEShapeCircle)bait.Shape).Radius;

        foreach (var zombie in Module.Enemies((uint)OID.ZombiePiece))
        {
            var reach = baitRadius + zombie.HitboxRadius;

            var onHitbox = (zombie.Position - bait.Target.Position).LengthSq() <= reach * reach;
            Arena.ZoneCircleOutline(zombie.Position, zombie.HitboxRadius, onHitbox ? Colors.Danger : Colors.Border);
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (!IsBaitTarget(actor) || CurrentBaits.Count == 0)
        {
            return;
        }

        hints.Add("Avoid intersecting zombies hitboxes!");
    }

    private readonly List<Actor> zombies = module.Enemies((uint)OID.ZombiePiece);

    /// <summary>
    /// Drop Death Drive where it raises the fewest zombies. About 4.5s after it lands it raises every corpse its circle
    /// touches: all eleven Death Drives of Korha's four pulls (2026-10-22, 10-24), not one corpse outside it. With twenty
    /// corpses on the floor there is often no spot clear of all of them, so: of the floor clear of everything else
    /// drawn, the spot touching the fewest corpses, nearest the baiter, and every corpse that spot does not touch is
    /// ground the baiter keeps off until the drop. 2026-10-24: Korha killed three zombies at (134,9), the drop on the
    /// same spot raised seven, and they confused and killed her.
    /// </summary>
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        var mine = this.ActiveBaitsOn(actor);
        if (mine.Count == 0 || mine[0].Shape is not AOEShapeCircle circle)
            return;
        var corpses = this.zombies.FindAll(z => z.IsDead);
        if (corpses.Count == 0 || this.BestDrop(slot, actor, corpses, circle.Radius) is not { } drop)
            return;
        foreach (var c in corpses)
        {
            var reach = circle.Radius + c.HitboxRadius;
            if ((c.Position - drop).Length() > reach + DropRoom)
                hints.AddForbiddenZone(new AOEShapeCircle(reach), c.Position, default, mine[0].Activation);
        }
    }

    /// <summary>Room left clear around the chosen drop, in yalms. The dodge keeps its safety margin from every forbidden
    /// edge and stands on a grid; a spot in a gap between two corpses' circles thinner than that is no spot at all, and
    /// with none the dodge stood still on the corpses it was meant to leave (replayed, 2026-10-24). A corpse this close is
    /// counted as raised: wherever in the room the baiter ends up, nothing else is.</summary>
    private const float DropRoom = 2f;

    private WPos? BestDrop(int slot, Actor actor, List<Actor> corpses, float radius)
    {
        WPos? best = null;
        var bestCount = int.MaxValue;
        var bestDistance = float.MaxValue;
        var center = this.Module.Center;
        var extent = this.Module.Bounds.Extent;
        for (var x = -extent + 0.5f; x < extent; x += 1f)
        {
            for (var z = -extent + 0.5f; z < extent; z += 1f)
            {
                var p = new WPos(center.X + x, center.Z + z);
                if (!this.Module.Bounds.Contains(center, p) || this.InSomethingDrawn(slot, actor, p))
                    continue;
                var count = 0;
                foreach (var c in corpses)
                    if ((c.Position - p).Length() <= radius + c.HitboxRadius + DropRoom)
                        ++count;
                var distance = (p - actor.Position).LengthSq();
                if (count < bestCount || (count == bestCount && distance < bestDistance))
                {
                    best = p;
                    bestCount = count;
                    bestDistance = distance;
                }
            }
        }
        return best;
    }

    private bool InSomethingDrawn(int slot, Actor actor, WPos p)
    {
        foreach (var component in this.Module.Components)
        {
            ReadOnlySpan<AOEInstance> active;
            if (component is Components.GenericAOEs aoes)
                active = aoes.ActiveAOEs(slot, actor);
            else if (component is Components.Voidzone puddles)   // the Maladies
                active = puddles.ActiveAOEs(slot, actor);
            else
                continue;
            foreach (ref readonly var aoe in active)
                if (aoe.Risky && aoe.Check(p))
                    return true;
        }
        return false;
    }
}

// All three Mindjacks recorded so far were Forward March or About Face, which BMR's version (and this one, until
// 2026-10-24) did not list, so no walk was ever aimed: Korha marched 18.7y off the east edge into the bleeding
// wall at 211 HP and died five seconds later.
sealed class Mindjack(ModuleBase module) : Components.StatusDrivenForcedMarch(module, 3.0f, (uint)SID.ForwardMarch, (uint)SID.AboutFace, (uint)SID.LeftFace, (uint)SID.RightFace);

sealed class VoidmancerPieceStates : StateMachineBuilder
{
    public VoidmancerPieceStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<DeathDriveBait>()
            .ActivateOnEnter<DeathDrive>()
            .ActivateOnEnter<DarkOrb>()
            .ActivateOnEnter<Malady>()
            .ActivateOnEnter<Mindjack>();
    }
}

[ModuleInfo(CFCID = 1089u, NameID = 14552u, PrimaryActorOID = (uint)OID.VoidmancerPiece, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Equilius (ported from BMR)")]
public sealed class VoidmancerPiece(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(120f, 0f), new ArenaBoundsRect(20f, 20f))
{

    public override bool ShouldPrioritizeAllEnemies => true;

    // Living zombies before the boss. Each zombie hit stacks Wits End and twelve stacks confuse; with no healing, their
    // chip damage is what killed Korha on 2026-10-24. A zombie has 1,344 HP to the boss's 16,010, and its corpse stays
    // down unless a Death Drive lands on it (see DeathDriveBait).
    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        foreach (var e in hints.PotentialTargets)
            if (e.Actor.OID == (uint)OID.ZombiePiece && !e.Actor.IsDead)
                e.Priority = 1;
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.ZombiePiece));
    }
}
