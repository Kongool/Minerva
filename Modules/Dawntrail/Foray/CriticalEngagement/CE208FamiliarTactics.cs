// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.CriticalEngagement.CE208FamiliarTactics;

public enum OID : uint
{
    ElmGigas = 0x4BD9,
    Helper = 0x233C,
    ElmGigasPuddle = 0x4BDA, // R4.000, x0 (spawn during fight)
}

public enum AID : uint
{
    AutoAttack = 50851, // ElmGigas->player, no cast, single-target
    AncientAeroIII = 47544, // ElmGigas->self, 3.5+1.5s cast, single-target
    AncientAeroIIIVisual = 48041, // Helper->self, 5.0s cast, ???
    SpinningSweep = 47541, // ElmGigas->self, 6.0s cast, range 40 120.000-degree cone
    InspiritedCrosswindsCast = 47533, // ElmGigas->self, 6.0+0.8s cast, single-target
    InspiritedCrosswinds = 47535, // 4BDA->self, 6.0s cast, range 60 width 8 cross
    InspiritedImpactCast = 47542, // ElmGigas->self, 3.0s cast, single-target
    InspiritedImpact = 47543, // Helper->self, 9.6s cast, range 25 circle
    InspiritedHurricaneCast = 47536, // ElmGigas->self, 4.3+0.7s cast, single-target
    InspiritedHurricaneCross = 47538, // Helper->self, 5.0s cast, range 60 width 10 cross
    InspiritedHurricaneCircle = 47537, // Helper->self, 5.0s cast, range 12 circle
    AncientAero = 47540, // Helper->self, 3.0s cast, range 70 width 6 rect
    InspiritedCycloneCast = 47532, // ElmGigas->self, 5.0+1.0s cast, single-target
    InspiritedCyclone = 47534, // 4BDA->self, 6.0s cast, range 12 circle
    UnbowedSpiritCast = 47530, // ElmGigas->self, 3.0+1.0s cast, single-target
    UnbowedSpirit = 47531, // Helper->self, no cast, range 4 circle
}

[SkipLocalsInit]
sealed class AncientAeroIII(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AncientAeroIII);
[SkipLocalsInit]
sealed class SpinningSweep(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SpinningSweep, new AOEShapeCone(40.0f, 60.0f.Degrees()));
[SkipLocalsInit]
sealed class InspiritedCrosswinds(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.InspiritedCrosswinds, new AOEShapeCross(60.0f, 4.0f));
[SkipLocalsInit]
sealed class InspiritedHurricaneCross(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.InspiritedHurricaneCross, new AOEShapeCross(60.0f, 5.0f));
[SkipLocalsInit]
sealed class InspiritedHurricaneCircleCyclone(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.InspiritedHurricaneCircle, (uint)AID.InspiritedCyclone], 12f);
[SkipLocalsInit]
sealed class AncientAero(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AncientAero, new AOEShapeRect(70.0f, 3.0f));

[SkipLocalsInit]
sealed class UnbowedSpirit(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoes = [];
    private readonly List<Actor> puddles = [];
    private bool circular;

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.ElmGigasPuddle)
        {
            if (puddles.Count == 0)
            {
                var offset = actor.Position - Module.Center;
                circular = MathF.Abs(offset.X % 10f) > 1f || MathF.Abs(offset.Z % 10f) > 1f;
            }

            puddles.Add(actor);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.InspiritedCrosswinds or (uint)AID.InspiritedCyclone)
        {
            if (puddles.Count > 0)
            {
                puddles.RemoveAt(0);
            }
        }
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        return _aoes;
    }

    /// <summary>The puddle plus the ground it is about to roll over. Drawn, and lethal now.</summary>
    private const float DrawnLead = 4f;

    /// <summary>Puddle radius, from the actor's own hitbox in the recordings.</summary>
    private const float PuddleRadius = 4.2f;

    /// <summary>
    /// How far ahead in time the dodge is told about a rolling puddle's path.
    /// <para>The drawn capsule reaches four yalms, which is about a second of warning, and a dodge takes two to three.
    /// The character walked to ground that was clear when it was chosen and occupied by the time it arrived: five
    /// puddle hits in the 09-06 pull, six in Korha's 09-07 pull and two in Saar's. The module was ported carrying the
    /// note "needs extended moving AOE support", and this is it.</para>
    /// </summary>
    private static readonly double[] LookaheadSeconds = [1.5d, 2.3d, 3.1d];

    /// <summary>
    /// How fast a puddle goes, measured over six recordings (2026-09-26 to 2026-10-11, 100,000 one-second samples):
    /// one rolling straight does 3.0 yalms a second (3.5 at the 99th percentile); one orbiting turns at a constant
    /// 0.20 radians a second (0.23 at the 99th), so 2 yalms a second on an inner ring and 6 at the rim.
    /// <para>The forecast used one length for all of them, sized for a fast one: 16 yalms in 3.1 seconds. That is
    /// nearly twice an inner orbiter's reach, and with eight of them out the arcs covered ground no puddle would get to
    /// in time. Korha, 2026-10-11: dodging Spinning Sweep near the middle, the only way out the solve had left ran
    /// across the front of an orbiter doing 1.9 yalms a second, and it caught him. It was also short for one at the rim.</para>
    /// </summary>
    private const float StraightSpeed = 3.5f;

    private const float OrbitRadiansPerSecond = 0.23f;

    /// <summary>A pulse lands this far ahead of where the puddle is drawn: 1.4-1.5 yalms at the median over the same
    /// recordings, the actor's position trailing the server's.</summary>
    private const float PulseRunsAhead = 1.5f;

    private float Speed(Actor puddle)
        => circular ? OrbitRadiansPerSecond * (puddle.Position - Module.Center).Length() : StraightSpeed;

    /// <summary>
    /// The swept shape a puddle covers over the next <paramref name="length"/> yalms of its travel. Puddles
    /// either roll straight or orbit the arena centre, which is what <see cref="circular"/> decides; an
    /// orbiting one sweeps an arc, and the same yalms of travel are fewer degrees the further out it is.
    /// </summary>
    private AOEShape SweptShape(Actor puddle, float length)
    {
        if (Parked(puddle))
        {
            return new AOEShapeCircle(PuddleRadius);
        }

        if (!circular)
        {
            return new AOEShapeCapsule(PuddleRadius, length);
        }

        var center = Module.Center;
        var offset = puddle.Position - center;
        var radius = offset.Length();
        if (radius < 0.1f)
        {
            return new AOEShapeCapsule(PuddleRadius, length);
        }

        var angleDirection = offset.Cross(puddle.Rotation.ToDirection()) > 0f;
        var span = length / radius;
        return new AOEShapeArcCapsule(PuddleRadius, (angleDirection ? -span : span).Radians(), center);
    }

    /// <summary>
    /// A puddle casting its cross or its circle has stopped for good: it stands where it is until it goes. Korha and
    /// Saar, 2026-10-11: eight puddles rolled at 3y/s until 50.0s and then stood for the whole 5.7s of Inspirited
    /// Crosswinds, each still forecasting sixteen yalms of path. With those on top of the eight crosses no ground was
    /// clear of everything, the solve fell back to a stage inside the second set of crosses (0.17s after the first),
    /// and both were hit standing where it had sent them.
    /// </summary>
    private static bool Parked(Actor puddle) => puddle.CastInfo != null;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        // the drawn capsule: the puddle and the next four yalms, dangerous right now
        base.AddAIHints(slot, actor, assignment, hints);

        var countP = puddles.Count;
        for (var i = 0; i < countP; ++i)
        {
            var puddle = puddles[i];
            if (Parked(puddle))
                continue;
            var pos = puddle.Position;
            var rot = puddle.Rotation;
            var speed = Speed(puddle);
            for (var t = 0; t < LookaheadSeconds.Length; ++t)
            {
                var seconds = LookaheadSeconds[t];
                hints.AddForbiddenZone(SweptShape(puddle, (speed * (float)seconds) + PulseRunsAhead), pos, rot, World.FutureTime(seconds));
            }
        }
    }

    public override void Update()
    {
        var countP = puddles.Count;
        if (countP == 0 && _aoes.Length != 0)
        {
            _aoes = [];
        }
        _aoes = new AOEInstance[countP];
        for (var i = 0; i < countP; ++i)
        {
            var puddle = puddles[i];
            _aoes[i] = new(this.SweptShape(puddle, DrawnLead), puddle.Position, puddle.Rotation, color: Colors.Danger);
        }
    }
}

[SkipLocalsInit]
sealed class InspiritedImpact(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.InspiritedImpact, 25f, 3);

[SkipLocalsInit]
sealed class CE208FamiliarTacticsStates : StateMachineBuilder
{
    public CE208FamiliarTacticsStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<AncientAeroIII>()
            .ActivateOnEnter<SpinningSweep>()
            .ActivateOnEnter<InspiritedCrosswinds>()
            .ActivateOnEnter<InspiritedImpact>()
            .ActivateOnEnter<InspiritedHurricaneCross>()
            .ActivateOnEnter<InspiritedHurricaneCircleCyclone>()
            .ActivateOnEnter<AncientAero>()
            .ActivateOnEnter<UnbowedSpirit>();
    }
}

// Moving-AOE support is in: UnbowedSpirit publishes the puddle's future path as timed forbidden zones
// (see its LookaheadSeconds). Still WIP for the rest -- Ancient Aero (47540) clipped both toons repeatedly on
// 09-07, twice while the dodge believed the spot was safe inside its margin.
[ModuleInfo(Group = ModuleGroup.CriticalEngagement, CFCID = 1093u, NameID = 58u, PrimaryActorOID = (uint)OID.ElmGigas, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Equilius (ported from BMR)")]
[SkipLocalsInit]
public sealed class CE208FamiliarTactics : ModuleBase
{
    public CE208FamiliarTactics(WorldState ws, Actor primary) : this(ws, primary, BuildArena()) { }

    private CE208FamiliarTactics(WorldState ws, Actor primary, (WPos center, ArenaBoundsCustom arena) a) : base(ws, primary, a.center, a.arena) { }

    private static (WPos center, ArenaBoundsCustom arena) BuildArena()
    {
        var arena = new ArenaBoundsCustom([new Polygon(new(-390f, 700f), 29.5f, 32)]);
        return (arena.Center, arena);
    }

    protected override bool CheckPull() => base.CheckPull() && Raid.Player()!.Position.InCircle(Module.Center, 30f);
}
