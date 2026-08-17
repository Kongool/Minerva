using System.Runtime.InteropServices;

namespace Minerva.Components;

/// <summary>
/// A bullseye of concentric shapes fired from one origin — typically a small circle, then donuts of
/// growing inner radius — that resolve one ring at a time from the centre outward. The author adds a
/// sequence when the mechanic starts and advances it as each ring goes off. By default only the ring
/// about to resolve is treated as dangerous (safe rings are hidden); pass <paramref name="showAll"/>
/// to preview the whole set with only the next ring flagged risky.
/// </summary>
public class ConcentricAOEs(ModuleBase module, AOEShape[] shapes, bool showAll = false) : GenericAOEs(module, warningText: "Wrong ring — move!")
{
    /// <summary>One in-flight bullseye: where it's centred and how many of its rings have resolved.</summary>
    public sealed class Sequence(WPos origin, Angle rotation = default, DateTime nextActivation = default)
    {
        public WPos Origin = origin;
        public Angle Rotation = rotation;
        public DateTime NextActivation = nextActivation;
        public int Done; // rings already resolved -> index of the next shape to go off
    }

    public readonly AOEShape[] Shapes = shapes;
    public readonly bool ShowAll = showAll;
    public readonly List<Sequence> Sequences = [];
    private readonly List<AOEInstance> active = [];

    public bool Any => this.Sequences.Count > 0;

    /// <summary>Start a new bullseye centred at <paramref name="origin"/>.</summary>
    public void AddSequence(WPos origin, DateTime nextActivation = default, Angle rotation = default)
        => this.Sequences.Add(new Sequence(origin, rotation, nextActivation));

    /// <summary>
    /// Mark the current ring of the sequence at <paramref name="origin"/> resolved and move to the
    /// next; the sequence is dropped once its last ring has gone off. Returns false if no sequence
    /// matched (so a caller can tell an unexpected cast from an expected advance).
    /// </summary>
    public bool AdvanceSequence(WPos origin, DateTime nextActivation = default)
    {
        foreach (var s in this.Sequences)
        {
            if (s.Origin.AlmostEqual(origin, 1f))
            {
                s.NextActivation = nextActivation;
                if (++s.Done >= this.Shapes.Length)
                    this.Sequences.Remove(s);
                return true;
            }
        }
        return false;
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        this.active.Clear();
        foreach (var s in this.Sequences)
        {
            if (s.Done >= this.Shapes.Length)
                continue;
            if (this.ShowAll)
                for (var i = s.Done; i < this.Shapes.Length; ++i)
                    this.active.Add(new AOEInstance(this.Shapes[i], s.Origin, s.Rotation, s.NextActivation, risky: i == s.Done));
            else
                this.active.Add(new AOEInstance(this.Shapes[s.Done], s.Origin, s.Rotation, s.NextActivation));
        }
        return CollectionsMarshal.AsSpan(this.active);
    }
}
