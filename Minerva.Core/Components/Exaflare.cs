using System.Runtime.InteropServices;

namespace Minerva.Components;

/// <summary>
/// An "exaflare": one or more lines of same-shaped AOEs that march across the arena, each explosion
/// stepping a fixed distance along the line at a fixed cadence. The imminent explosion of each line is
/// drawn as dangerous; a few upcoming steps are previewed but not counted as immediate danger. Authors
/// drive it directly with <see cref="Lines"/>, or use <see cref="SimpleExaflare"/> for the common
/// cast-driven case.
/// </summary>
public class Exaflare(ModuleBase module, AOEShape shape) : GenericAOEs(module, warningText: "GTFO from exaflare!")
{
    /// <summary>One marching line: its next explosion point, step vector, cadence and steps remaining.</summary>
    public sealed class Line(WPos next, WDir advance, DateTime nextExplosion, double timeToMove, int explosionsLeft, int maxShown)
    {
        public WPos Next = next;
        public WDir Advance = advance;
        public DateTime NextExplosion = nextExplosion;
        public double TimeToMove = timeToMove;
        public int ExplosionsLeft = explosionsLeft;
        public int MaxShown = maxShown; // how many upcoming steps to preview
    }

    public readonly AOEShape Shape = shape;
    public readonly List<Line> Lines = [];
    private readonly List<AOEInstance> active = [];

    public Exaflare(ModuleBase module, float radius) : this(module, new AOEShapeCircle(radius)) { }

    public bool Active => this.Lines.Count > 0;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        this.active.Clear();
        foreach (var l in this.Lines)
        {
            if (l.ExplosionsLeft <= 0)
                continue;
            // the imminent explosion at the current position is the real danger
            this.active.Add(new AOEInstance(this.Shape, l.Next, default, l.NextExplosion));
            // preview a few upcoming steps marching along Advance (drawn, but not "you're in danger" yet)
            var preview = Math.Min(l.ExplosionsLeft, l.MaxShown);
            var pos = l.Next;
            var time = l.NextExplosion;
            for (var j = 1; j < preview; ++j)
            {
                pos += l.Advance;
                time = time.AddSeconds(l.TimeToMove);
                this.active.Add(new AOEInstance(this.Shape, pos, default, time, risky: false));
            }
        }
        return CollectionsMarshal.AsSpan(this.active);
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
                    this.Lines.Remove(l);
                return;
            }
        }
    }
}
