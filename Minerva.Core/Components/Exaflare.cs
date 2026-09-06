using System.Runtime.InteropServices;

namespace Minerva.Components;

/// <summary>
/// An "exaflare": one or more lines of same-shaped AOEs that march across the arena, each explosion
/// stepping a fixed distance along the line at a fixed cadence. The imminent explosion of each line is
/// drawn as dangerous; a few upcoming steps are previewed but not counted as immediate danger. Authors
/// drive it directly with <see cref="Lines"/>, or use <see cref="SimpleExaflare"/> for the common
/// cast-driven case.
/// </summary>
public class Exaflare(ModuleBase module, AOEShape shape, uint aid = default) : GenericAOEs(module, aid, warningText: "GTFO from exaflare!")
{
    /// <summary>A circular exaflare, the common case.</summary>
    public Exaflare(ModuleBase module, float radius, uint aid = default) : this(module, new AOEShapeCircle(radius), aid) { }

    // Cache counters a subclass bumps when it rebuilds its AOE list, spelled as BossmodReborn spells them
    // (lowercase, protected) so a ported override compiles unedited. Owned entirely by the subclass: the
    // base never reads them, it only has to declare them.
#pragma warning disable SA1300, IDE1006 // BossmodReborn member spelling, deliberate
    protected int currentVersion, lastVersion, lastCount;
#pragma warning restore SA1300, IDE1006

    /// <summary>Colour for the line that is about to go off, and for the ones after it. Modules
    /// override them where a fight distinguishes its exaflares by colour.</summary>
    public uint ImminentColor = Colors.Danger;
    public uint FutureColor = Colors.AOE;

    /// <summary>One marching line: its next explosion point, step vector, cadence and steps remaining.</summary>
    public sealed class Line(WPos next, WDir advance, DateTime nextExplosion, double timeToMove, int explosionsLeft, int maxShownExplosions, Angle rotation = default)
    {
        public WPos Next = next;
        public WDir Advance = advance;
        public DateTime NextExplosion = nextExplosion;
        public double TimeToMove = timeToMove;
        public int ExplosionsLeft = explosionsLeft;
        public int MaxShown = maxShownExplosions; // how many upcoming steps to preview

        /// <summary>BossmodReborn's name for <see cref="MaxShown"/>; its modules read it by this name.</summary>
        /// <summary>How many future explosions to draw. Settable because a module tunes it per fight —
        /// a long exaflare line drawn in full is unreadable, a short one drawn partially is a trap.</summary>
        public int MaxShownExplosions { get => this.MaxShown; set => this.MaxShown = value; }
        public Angle Rotation = rotation; // facing for directional shapes (rects/cones); irrelevant for circles
    }

    public readonly AOEShape Shape = shape;
    public readonly List<Line> Lines = [];
    private readonly List<AOEInstance> active = [];

    public Exaflare(ModuleBase module, float radius) : this(module, new AOEShapeCircle(radius)) { }

    public bool Active => this.Lines.Count > 0;

    /// <summary>
    /// The next explosion of each of the first <paramref name="count"/> lines, as (where, when, facing).
    ///
    /// <para>Modules that draw an exaflare themselves — usually to merge it with another mechanic — want
    /// the imminent step of every line at once rather than the flattened AOE list. A line with nothing
    /// left contributes a default entry so the caller can index by line number, which is what makes the
    /// merge readable at the call site.</para>
    /// </summary>
    protected (WPos, DateTime, Angle)[] ImminentAOEs(int count)
    {
        var result = new (WPos, DateTime, Angle)[count];
        for (var i = 0; i < count && i < this.Lines.Count; ++i)
        {
            var l = this.Lines[i];
            if (l.ExplosionsLeft != 0)
                result[i] = (l.Next.Quantized(), l.NextExplosion, l.Rotation);
        }
        return result;
    }

    /// <summary>
    /// Where the first <paramref name="count"/> lines will explode next, with when and at what facing.
    ///
    /// <para>Skips each line's IMMINENT explosion and reports only the ones after it — the component
    /// already draws the imminent one, so a subclass rebuilding its AOE list from this would otherwise
    /// double it. Times are clamped to now, because a line whose next hop is already overdue should read as
    /// "about to happen" rather than as a moment in the past.</para>
    /// </summary>
    protected List<(WPos Position, DateTime Time, Angle Rotation)> FutureAOEs(int count)
    {
        var res = new List<(WPos, DateTime, Angle)>(count);
        var now = this.World.CurrentTime;
        for (var i = 0; i < count && i < this.Lines.Count; ++i)
        {
            var l = this.Lines[i];
            var shown = Math.Min(l.ExplosionsLeft, l.MaxShown);
            var pos = l.Next;
            var time = l.NextExplosion > now ? l.NextExplosion : now;
            for (var j = 1; j < shown; ++j)
            {
                pos += l.Advance;
                time = time.AddSeconds(l.TimeToMove);
                res.Add((pos.Quantized(), time, l.Rotation));
            }
        }

        return res;
    }

    /// <summary>
    /// A precomputed AOE list. Null means "derive it from <see cref="Lines"/>", which is the normal case.
    ///
    /// <para>BossmodReborn's Exaflare caches into a field and returns it; Minerva recomputes on demand. A
    /// ported subclass that overrides <c>Update</c> to build a merged future/imminent list assigns this
    /// instead, and it wins — without it the assignment would compile against nothing and the subclass's
    /// whole computation would be silently discarded.</para>
    /// </summary>
    protected AOEInstance[]? Aoes;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (this.Aoes != null)
            return this.Aoes;

        this.active.Clear();
        foreach (var l in this.Lines)
        {
            if (l.ExplosionsLeft <= 0)
                continue;
            // the imminent explosion at the current position is the real danger
            this.active.Add(new AOEInstance(this.Shape, l.Next, l.Rotation, l.NextExplosion));
            // preview a few upcoming steps marching along Advance (drawn, but not "you're in danger" yet)
            var preview = Math.Min(l.ExplosionsLeft, l.MaxShown);
            var pos = l.Next;
            var time = l.NextExplosion;
            for (var j = 1; j < preview; ++j)
            {
                pos += l.Advance;
                time = time.AddSeconds(l.TimeToMove);
                this.active.Add(new AOEInstance(this.Shape, pos, l.Rotation, time, risky: false));
            }
        }
        return CollectionsMarshal.AsSpan(this.active);
    }

    /// <summary>Step <paramref name="line"/> one explosion forward from an observed position, rather
    /// than from its own bookkeeping — use this when the game tells you where the step actually landed.
    /// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).</summary>
    protected void AdvanceLine(Line line, WPos pos)
    {
        line.Next = pos + line.Advance;
        line.NextExplosion = this.World.FutureTime(line.TimeToMove);
        line.ExplosionsLeft--;
    }

    /// <summary>Step <paramref name="line"/> one explosion forward along its path.</summary>
    protected void Advance(Line line)
    {
        line.Next += line.Advance;
        line.NextExplosion = this.World.FutureTime(line.TimeToMove);
        line.ExplosionsLeft--;
    }
}

/// <summary>
/// Cast-driven exaflare: the first explosion of each line casts <paramref name="aidFirst"/>, every
/// subsequent step casts <paramref name="aidRest"/>. Each first cast spawns a line advancing
/// <paramref name="distance"/> along the caster's facing; matching casts step the nearest line.
/// </summary>
public class SimpleExaflare(ModuleBase module, AOEShape shape, uint aidFirst, uint aidRest, float distance, double timeToMove, int explosionsLeft, int maxShown = 3, bool locationBased = true)
    : Exaflare(module, shape)
{
    public readonly uint AidFirst = aidFirst;
    public readonly uint AidRest = aidRest;
    public readonly float Distance = distance;
    public readonly double TimeToMove = timeToMove;
    public readonly int ExplosionsLeft = explosionsLeft;
    public readonly int MaxShown = maxShown;
    public readonly bool LocationBased = locationBased;

    public SimpleExaflare(ModuleBase module, float radius, uint aidFirst, uint aidRest, float distance, double timeToMove, int explosionsLeft, int maxShown = 3, bool locationBased = true)
        : this(module, new AOEShapeCircle(radius), aidFirst, aidRest, distance, timeToMove, explosionsLeft, maxShown, locationBased) { }

    /// <summary>How many exaflare lines have run to completion. Modules gate a later mechanic on it —
    /// "after the third line finishes" is a phase boundary a state machine cannot see.</summary>
    public int NumLinesFinished;

    private WPos CastPos(Actor caster, ActorCastInfo cast) => this.LocationBased && cast.LocXZ != default ? cast.LocXZ : caster.Position;

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.AidFirst)
            this.Lines.Add(new Line(this.CastPos(caster, cast), this.Distance * caster.Rotation.ToDirection(),
                this.Module.CastFinishAt(cast), this.TimeToMove, this.ExplosionsLeft, this.MaxShown));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        var id = cast.Action.ID;
        if (id != this.AidFirst && id != this.AidRest)
            return;
        var pos = this.CastPos(caster, cast);
        foreach (var l in this.Lines)
        {
            if (l.Next.AlmostEqual(pos, 1f))
            {
                this.Advance(l);
                if (l.ExplosionsLeft <= 0)
                {
                    this.Lines.Remove(l);
                    ++this.NumLinesFinished;
                }
                return;
            }
        }
    }
}
