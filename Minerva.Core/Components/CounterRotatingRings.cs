using System.Runtime.InteropServices;

namespace Minerva.Components;

/// <summary>
/// Concentric bands of ground AOE, each with a few safe openings, that rotate — usually in opposite
/// directions — and then stop. When they stop, anything not standing in an opening dies outright.
///
/// <para><b>Why this needs its own component.</b> The obvious drawing is wrong. Showing where the openings
/// are <i>now</i> walks you into a spot that has rotated into a wall by the time the mechanic resolves, and
/// on an instant-kill that is fatal advice. Everything here is projected forward to
/// <see cref="Resolve"/>: the openings are drawn where they <i>will</i> be, and the danger zones are the
/// bands minus those. Standing still in a drawn opening is correct, which is the property the whole
/// component exists to have.</para>
///
/// <para><b>Why bands and not one shape.</b> The rings turn independently, so a position is safe only with
/// respect to the band it is standing in. Counter-rotation means the inner and outer openings sweep past
/// each other, and the moment they line up is usually short — a module that treated the whole disc as one
/// shape could not express "safe at this radius, lethal one yalm further out".</para>
///
/// <para><b>The centre is its own hazard, and it matters more than it looks.</b> These mechanics put a
/// permanently lethal disc where the boss stands. That is a trap for uptime logic rather than for dodging:
/// a melee player is steered toward the boss, the boss is in the middle, and the middle kills — which is
/// how BossmodReborn's stick-to-target range lands people inside the kill circle. <see cref="LethalCore"/>
/// is published with no activation time, so it reads as forbidden *now* rather than at
/// <see cref="Resolve"/>, and the pathfinder's uptime pass refuses those cells outright. When melee range
/// lies entirely inside it the goal is simply unreachable and the player is left standing safe, which is
/// the right answer — there is no version of this where walking in is correct.</para>
///
/// <para><b>Angular velocity.</b> Supplied per band and signed: positive turns the way angles increase,
/// negative the other. Two bands counter-rotate by carrying opposite signs. A module that can measure the
/// rate from the spinning actors should do so and feed it in, rather than hard-coding a rate that a later
/// patch can change; <see cref="MeasureAngularVelocity"/> does that from two observations.</para>
/// </summary>
public class CounterRotatingRings(ModuleBase module, uint aid = default) : GenericAOEs(module, aid, "Get to a gap before the rings stop!")
{
    /// <summary>
    /// One band: the annulus between two radii, the openings in it, and how fast it turns.
    /// </summary>
    /// <param name="InnerRadius">Inner edge; 0 makes the band a full disc.</param>
    /// <param name="OuterRadius">Outer edge.</param>
    /// <param name="SafeCenters">Bearing of each opening's centre, at <see cref="CounterRotatingRings.ReferenceTime"/>.</param>
    /// <param name="SafeHalfWidth">Half the angular width of every opening.</param>
    /// <param name="AngularVelocity">Signed turn rate; the sign is the direction.</param>
    public readonly record struct Band(
        float InnerRadius,
        float OuterRadius,
        Angle[] SafeCenters,
        Angle SafeHalfWidth,
        Angle AngularVelocity);

    public readonly List<Band> Bands = [];

    /// <summary>Centre the bands turn about. Defaults to the arena centre when left unset.</summary>
    /// <remarks>Not named Center: that is the module geometry every component inherits, and shadowing it
    /// would leave this component unable to reach the arena centre it falls back to.</remarks>
    public WPos? CenterOverride;

    /// <summary>When rotation stops and the mechanic resolves. Nothing is drawn until this is set.</summary>
    public DateTime Resolve;

    /// <summary>
    /// Radius of the permanently lethal disc at <see cref="CenterOverride"/>, or 0 if the fight has none.
    /// <para>Published with no activation time on purpose: it is dangerous continuously, not at
    /// <see cref="Resolve"/>. That is what stops the uptime pass parking a melee player on top of the boss
    /// while the rings are still turning.</para>
    /// </summary>
    public float LethalCore;

    /// <summary>
    /// The moment <see cref="Band.SafeCenters"/> describes. Openings are advanced by
    /// <c>AngularVelocity * (Resolve - ReferenceTime)</c>. Set it to the time the bearings were observed;
    /// leaving it at default means they describe the rings as they will be at
    /// <see cref="WorldState.CurrentTime"/> when first built, which is rarely what a module means.
    /// </summary>
    public DateTime ReferenceTime;

    private readonly List<AOEInstance> aoes = [];
    private readonly List<(WPos Center, float Inner, float Outer, Angle Dir, Angle HalfWidth)> safeSpots = [];

    /// <summary>The openings, as they will be when the mechanic resolves. Empty until <see cref="Resolve"/> is set.</summary>
    public IReadOnlyList<(WPos Center, float Inner, float Outer, Angle Dir, Angle HalfWidth)> SafeSpots => this.safeSpots;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(this.aoes);

    /// <summary>
    /// Turn rate implied by seeing an actor at two bearings a known time apart, taking the shorter way
    /// round. A module sampling a spinning actor over consecutive frames gets the live rate from this
    /// instead of assuming one.
    /// <para>Only valid while the ring turns less than half a revolution between samples — past that, the
    /// shorter way round is the wrong way, and the result is a confident answer pointing backwards. Sample
    /// often.</para>
    /// </summary>
    public static Angle MeasureAngularVelocity(Angle before, Angle after, double seconds)
        => seconds > 0d ? new Angle((after - before).Normalized().Rad / (float)seconds) : default;

    public override void Update()
    {
        this.aoes.Clear();
        this.safeSpots.Clear();
        if (this.Resolve == default || this.Bands.Count == 0)
            return;

        var center = this.CenterOverride ?? this.Module.Center;
        var reference = this.ReferenceTime == default ? this.World.CurrentTime : this.ReferenceTime;
        var secondsOfTurn = (this.Resolve - reference).TotalSeconds;

        if (this.LethalCore > 0f)
        {
            // No activation: forbidden now, not when the rings stop.
            var core = new AOEShapeCircle(this.LethalCore);
            this.aoes.Add(new AOEInstance(core, center, default, default, shapeDistance: core.Distance(center, default)));
        }

        foreach (var band in this.Bands)
        {
            if (band.SafeCenters.Length == 0 || band.OuterRadius <= band.InnerRadius)
                continue;

            // where each opening will have turned to by the time it stops
            var drift = new Angle(band.AngularVelocity.Rad * (float)secondsOfTurn);
            var openings = new Angle[band.SafeCenters.Length];
            for (var i = 0; i < openings.Length; ++i)
                openings[i] = (band.SafeCenters[i] + drift).Normalized();

            foreach (var o in openings)
                this.safeSpots.Add((center, band.InnerRadius, band.OuterRadius, o, band.SafeHalfWidth));

            foreach (var (from, to) in Gaps(openings, band.SafeHalfWidth))
            {
                var half = new Angle((to - from).Rad * 0.5f);
                var mid = new Angle(from.Rad + half.Rad);
                var shape = new AOEShapeDonutSector(band.InnerRadius, band.OuterRadius, half);
                this.aoes.Add(new AOEInstance(shape, center, mid, this.Resolve, shapeDistance: shape.Distance(center, mid)));
            }
        }
    }

    /// <summary>
    /// The dangerous arcs: everything between one opening's trailing edge and the next one's leading edge,
    /// walking round once.
    /// <para>Returns nothing when the openings are wide enough to overlap each other, because then the band
    /// really is open all the way round and there is no wall to draw. A band declared with no openings at
    /// all is skipped by the caller rather than treated as lethal everywhere: that state means the module
    /// has not worked out where the gaps are yet, and inventing a wipe zone from missing data would be
    /// worse than drawing nothing.</para>
    /// </summary>
    private static List<(Angle From, Angle To)> Gaps(Angle[] openings, Angle halfWidth)
    {
        List<(Angle, Angle)> gaps = [];
        var sorted = new float[openings.Length];
        for (var i = 0; i < openings.Length; ++i)
            sorted[i] = openings[i].Normalized().Rad;
        Array.Sort(sorted);

        for (var i = 0; i < sorted.Length; ++i)
        {
            var from = sorted[i] + halfWidth.Rad;
            var to = sorted[(i + 1) % sorted.Length] - halfWidth.Rad;
            if (i == sorted.Length - 1)
                to += Angle.DoublePI; // last gap wraps past the seam back to the first opening

            var span = to - from;
            if (span <= 0f)
                continue; // openings overlap here: no wall between them
            gaps.Add((new Angle(from), new Angle(to)));
        }

        return gaps;
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);

        // Pull toward an opening rather than merely away from the walls. Without this the dodge will happily
        // settle just outside the outer ring, which is safe here but usually is not in the fight this
        // mechanic belongs to -- and on a wipe mechanic "somewhere else" is not advice.
        foreach (var (center, inner, outer, dir, halfWidth) in this.safeSpots)
        {
            // Aim at the middle of the opening, but never nearer the centre than the lethal core -- a goal
            // zone overlapping it would be pulling toward the one place that is certain death.
            var band = MathF.Max(inner, this.LethalCore);
            if (outer <= band)
                continue;
            var mid = (band + outer) * 0.5f;
            hints.GoalZones.Add(AIHints.GoalDonut(center, band, outer, 0.5f));
            hints.GoalZones.Add(AIHints.GoalSingleTarget(center + (dir.ToDirection() * mid), MathF.Max(halfWidth.Rad * mid, 1f), 1f));
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        // The openings, where they will be when it stops. Drawn on top so they read as the destination
        // rather than as one more thing on the floor.
        foreach (var (center, inner, outer, dir, halfWidth) in this.safeSpots)
            this.Arena.ZoneCone(center, inner, outer, dir, halfWidth, Colors.SafeFromAOE);
    }
}
