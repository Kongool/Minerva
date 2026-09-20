// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A34Promathia;

sealed class EmptySalvation(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.EmptySalvation);

sealed class Explosion(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Explosion, 16f);

// Wheel of Impregnability: a 13-yalm circle on the boss that fires 12.7s after the boss's cast starts
// (2026-09-06 recording, four instances of it and the donut: 12.68, 12.66, 12.58, 12.72). The port computed
// the activation as "10.5s from now" on every frame, so the dodge never saw it as urgent and never moved.
sealed class WheelofImpregnability(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.WheelOfImpregnabilityFire)
{
    private DateTime _activation;
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (_activation == default)
            return [];
        return new AOEInstance[] { new(new AOEShapeCircle(13f), Module.PrimaryActor.Position, default, _activation) };
    }
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.WheelOfImpregnabilityCast)
            _activation = World.FutureTime(12.7d);
    }
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.WheelOfImpregnabilityFire)
        {
            ++NumCasts;
            _activation = default;
        }
    }
}

// Bastion of Twilight: an 8-50 donut on the boss, on the same 12.7s clock. While an Explosion is being cast
// the donut's hole sits under the explosions, so the donut is drawn but not forbidden (Veyn's rule): the
// dodge clears the explosions first, then has the six seconds after they land to get into the hole. The
// port also watched the Wheel's action here, a copy-paste slip.
sealed class BastionOfTwilight(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.BastionOfTwilightFire)
{
    private DateTime _activation;
    private bool _risky = true;
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (_activation == default)
            return [];
        return new AOEInstance[] { new(new AOEShapeDonut(8f, 50f), Module.PrimaryActor.Position, default, _activation, risky: _risky) };
    }
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.BastionOfTwilightCast)
            _activation = World.FutureTime(12.7d);
        else if (spell.Action.ID == (uint)AID.Explosion)
            _risky = false;
    }
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.BastionOfTwilightFire)
        {
            ++NumCasts;
            _activation = default;
        }
        else if (spell.Action.ID == (uint)AID.Explosion)
            _risky = true;
    }
}

sealed class PestilentPenance(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PestilentPenance, new AOEShapeRect(50f, 25f));

sealed class Comet(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.Comet, (uint)AID.Comet, 6f, 4d)
{
    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.Meteor1 or (uint)AID.Meteor2) // Hey guess what?  The tankbuster in meteor is actually just Comet again!
        {
            Spreads.Clear();
        }
    }
}

sealed class FalseGenesis(ModuleBase module) : ModuleComponent(module)
{
    private bool _casting = false;

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        if (!_casting)
        {
            return;
        }
        Arena.ZoneRect(new(-820f, -807f), default(Angle), 6.5f, 6.5f, 6.5f, Colors.SafeFromAOE);
        Arena.ZoneRect(new(-808.724f, -826.5f), 120.Degrees(), 6.5f, 6.5f, 6.5f, Colors.SafeFromAOE);
        Arena.ZoneRect(new(-831.258f, -826.5f), -120.Degrees(), 6.5f, 6.5f, 6.5f, Colors.SafeFromAOE);
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (!_casting)
        {
            return;
        }
        var pos = actor.Position;
        if (!pos.InRect(new(-820f, -807f), default(Angle), 6.5f, 6.5f, 6.5f) || pos.InRect(new(-808.724f, -826.5f), 120.Degrees(), 6.5f, 6.5f, 6.5f) || pos.InRect(new(-831.258f, -826.5f), -120.Degrees(), 6.5f, 6.5f, 6.5f))
        {
            hints.Add("Move to platform spawn!");
        }
    }
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.FalseGenesis)
        {
            _casting = true;
        }
    }
    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.FalseGenesis)
        {
            _casting = false;
        }
    }
}

sealed class ArenaChanges(ModuleBase module) : ModuleComponent(module)
{
    private static readonly WPos _arenacentre = new(-820f, -820f);
    private static readonly ArenaBoundsCircle _startingboundscircle = new(25f);

    /// <summary>Half-side of one platform.</summary>
    private const float PlatformHalf = 6.5f;

    /// <summary>
    /// The three platforms False Genesis leaves behind. One table, used for the bounds that replace the
    /// floor, for the ground the dodge is told to be standing on before they do, and for the outline drawn
    /// while the cast runs -- so the warning and the floor can never describe different platforms.
    /// </summary>
    private static readonly (WPos Centre, Angle Rotation)[] Platforms =
    [
        (new(-820f, -807f), default),
        (new(-808.724f, -826.5f), 120f.Degrees()),
        (new(-831.258f, -826.5f), -120f.Degrees()),
    ];

    private static readonly ArenaBoundsCustom _splitbounds = BuildSplitBounds();
    private static readonly ShapeDistance _offThePlatforms = BuildOffThePlatforms();

    private static ArenaBoundsCustom BuildSplitBounds()
    {
        var shapes = new Shape[Platforms.Length];
        for (var i = 0; i < Platforms.Length; ++i)
            shapes[i] = new Square(Platforms[i].Centre, PlatformHalf, Platforms[i].Rotation);
        return new ArenaBoundsCustom(shapes);
    }

    /// <summary>
    /// Everything that is not a platform, as one zone. A real signed distance rather than a containment
    /// test, so a cell just inside a ledge reads as marginal and the dodge keeps the safety margin it is
    /// configured with away from the drop, without the platform being hand-shrunk to fake it.
    /// </summary>
    private static ShapeDistance BuildOffThePlatforms()
    {
        var zones = new ShapeDistance[Platforms.Length];
        for (var i = 0; i < Platforms.Length; ++i)
            zones[i] = new SDRect(Platforms[i].Centre, Platforms[i].Rotation, PlatformHalf, PlatformHalf, PlatformHalf);
        return new SDOutsideOfUnion(zones);
    }

    /// <summary>When the floor goes, or default when it is not going anywhere.</summary>
    private DateTime falls;

    private bool Pending => this.falls != default;

    /// <summary>
    /// The floor does not shrink at a moment the dodge can work out for itself -- the character is standing
    /// on ground that stops existing when False Genesis finishes. Swapping the bounds on the cast end is
    /// right and too late: by then anyone off a platform is already falling.
    ///
    /// <para>The cast runs 9.7 seconds (2026-09-20: 119.3s to 128.8s), so while it runs everything that is
    /// not a platform is forbidden ground with the cast end as its activation. The dodge then walks across
    /// during the cast and the swap lands under the character rather than out from under it -- the same
    /// answer as Ultima's shelter, which walks in with two seconds to spare.</para>
    /// </summary>
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.FalseGenesis)
            this.falls = Module.CastFinishAt(spell);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.FalseGenesis)
        {
            this.falls = default;   // the bounds say it now; the warning has done its job
            Module.Bounds = _splitbounds;
            Module.Center = _splitbounds.Center;
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x0C && state == 0x00080004)
        {
            this.falls = default;
            Module.Bounds = _startingboundscircle;
            Module.Center = _arenacentre;
        }
    }

    private static bool OnAPlatform(WPos p)
    {
        for (var i = 0; i < Platforms.Length; ++i)
            if (p.InSquare(Platforms[i].Centre, PlatformHalf, Platforms[i].Rotation))
                return true;
        return false;
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (this.Pending)
            hints.AddForbiddenZone(_offThePlatforms, this.falls);
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (this.Pending)
            hints.Add("Get on a platform!", !OnAPlatform(actor.Position));
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (!this.Pending)
            return;
        for (var i = 0; i < Platforms.Length; ++i)
            Arena.OutlineShape(new AOEShapeRect(PlatformHalf, PlatformHalf, PlatformHalf), Platforms[i].Centre, Platforms[i].Rotation, Colors.Safe, 2f);
    }
}

/// <summary>The floor going out from under the alliance: a 25-yalm circle on the arena centre, which is
/// every inch of the old floor. Nobody dodges it -- the platforms are about not falling afterwards, not
/// about this hit -- so it is recorded as the raidwide it is rather than left for the dodge to be blamed
/// for.</summary>
sealed class FalseGenesisHit(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.FalseGenesis1);

sealed class MemoryReceptacle(ModuleBase module) : Components.Adds(module, (uint)OID.MemoryReceptacle);

sealed class EmptyBeleaguer(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.EmptyBeleaguer, 6f);
sealed class AuroralDrape(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AuroralDrape, new AOEShapeRect(7f, 3.5f));
sealed class WindsOfPromyvion(ModuleBase module) : Components.GenericRotatingAOE(module)
{
    private readonly AOEShapeRect _rect = new(16f, 3f);
    private Actor? _caster;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.WindsLeft)
        {
            Sequences.Add(new(_rect, actor.Position, actor.Rotation, 30.Degrees(), World.FutureTime(4.5d), 1.4d, 4));
            _caster = actor;
        }
        if (iconID == (uint)IconID.WindsRight)
        {
            Sequences.Add(new(_rect, actor.Position, actor.Rotation, -30.Degrees(), World.FutureTime(4.5d), 1.4d, 4));
            _caster = actor;
        }
    }
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.WindsOfPromyvionCast or (uint)AID.WindsOfPromyvionSpam)
        {
            AdvanceSequence(0, World.CurrentTime);
        }
    }
    public override void OnActorRenderflagsChange(Actor actor, int renderflags)
    {
        if (_caster != null)
        {
            if (actor.InstanceID == _caster.InstanceID && renderflags == 16384)
            {
                Sequences.Clear();
                Update();
            }
        }
    }
}
sealed class EmptySeed(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.EmptySeed, 10f, shape: new AOEShapeCircle(10f))
{
    private Angle PlatformOrientation(WPos p)
    {
        if (p.Z > Center.Z)
        {
            return default;
        }
        if (p.X < Center.X)
        {
            return -120.Degrees();
        }

        return 120.Degrees();
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
        {
            var firstcornerdir = PlatformOrientation(caster.Position) + 45.Degrees();
            var walls = new SafeWall[8];
            var dist = 3.5f;
            for (var i = 0; i < 4; i++)
            {
                var z = i * 2;
                var angle = firstcornerdir + i * 90f.Degrees();
                var corner = caster.Position + angle.ToDirection() * 8.9f; // This isn't 5e!  Diagonals are Longer!
                var end1 = corner + (angle + 225.Degrees()).ToDirection() * dist; // 180+45, turn around, then turn one way or the other.
                var end2 = corner + (angle - 225.Degrees()).ToDirection() * dist;
                walls[z] = new(corner, end1);
                walls[z + 1] = new(corner, end2);
            }

            var minDist = KnockbackKind == Kind.TowardsOrigin ? (MinDistance + (MinDistanceBetweenHitboxes ? Raid.Player()!.HitboxRadius + caster.HitboxRadius : default)) : MinDistance;
            Casters.Add(new(spell.LocXZ, Distance, Module.CastFinishAt(spell), Shape, spell.Rotation, KnockbackKind, minDist, walls, caster.InstanceID, IgnoreImmunes));
        }
    }
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count == 0)
            return;
        for (var i = 0; i < Casters.Count; i++)
        {
            var source = Casters[i].Origin;
            if (actor.DistanceToPoint(source) < 9f)
            {
                var firstcornerdir = PlatformOrientation(source) + 45.Degrees();
                var angle = firstcornerdir;
                var corner = source + angle.ToDirection() * 9f;
                hints.AddForbiddenZone(new SDInvertedCone(corner, 5f, angle + 180.Degrees(), 22.5f.Degrees()));
            }
        }
    }
}

sealed class MalevolentBlessingCone(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MalevolentBlessingCone, new AOEShapeCone(40f, 11.5f.Degrees()));

sealed class MalevolentBlessingRect(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MalevolentBlessingRect, new AOEShapeRect(50f, 25f));

sealed class PestilentPenanceLink(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PestilentPenanceLink, new AOEShapeRect(50f, 2.5f));

sealed class InfernalDeliveranceTower(ModuleBase module) : Components.CastTowers(module, (uint)AID.InfernalDeliveranceTower, 4f, 1, 8);

sealed class InfernalDeliveranceAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.InfernalDeliveranceAOE, 8f);

sealed class Meteor(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.Meteor, (uint)AID.Meteor2, 6f, 5f);  // this is the regular spreads only, Tankbuster is handled in Comet.

sealed class DeadlyRebirth(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.DeadlyRebirth);

// The raidwide also shoves everyone 20 yalms along the boss's facing (2026-09-06 recording: 16 yalms in
// 0.3s, Down for the Count on landing). Forbid the ground the shove would carry off the arena, Veyn's way:
// stay inside the arena's circle shifted 20 yalms against the push.
sealed class DeadlyRebirthKnockback(ModuleBase module) : Components.GenericKnockback(module, (uint)AID.DeadlyRebirth1)
{
    private DateTime _activation;
    private Angle _direction;

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor)
    {
        if (_activation == default)
            return [];
        return new Knockback[] { new(Module.PrimaryActor.Position, 20f, _activation, direction: _direction, kind: Kind.DirForward) };
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (_activation != default)
            hints.AddForbiddenZone(new SDInvertedCircle(Module.Center - _direction.ToDirection() * 20f, Module.Bounds.Radius), _activation);
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.DeadlyRebirth)
        {
            _activation = Module.CastFinishAt(spell, 2d);
            _direction = spell.Rotation;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.DeadlyRebirth1)
        {
            ++NumCasts;
            _activation = default;
        }
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1117u, CFCID = 1117u, NameID = 14779u, PrimaryActorOID = (uint)OID.Promathia, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team, HerStolenLight (ported from BMR)")]

public sealed class A34Promathia(WorldState ws, Actor primary) : ModuleBase(ws, primary, ArenaCenter, new ArenaBoundsCircle(25f))
{
    public static readonly WPos ArenaCenter = new(-820f, -820f);
}
