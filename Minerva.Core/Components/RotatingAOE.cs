using System.Runtime.InteropServices;

namespace Minerva.Components;

/// <summary>
/// A sequence of same-origin AOEs (typically cones) whose rotation steps by a fixed increment each cast —
/// e.g. a sweeping laser. The author adds a <see cref="Sequence"/> and advances it as each cast resolves;
/// this shows the imminent cast plus the next few. Ported from BossmodReborn's GenericRotatingAOE (BSD-3;
/// see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class GenericRotatingAOE(ModuleBase module) : GenericAOEs(module)
{
    public struct Sequence(AOEShape shape, WPos origin, Angle rotation, Angle increment, DateTime nextActivation, double secondsBetweenActivations, int numRemainingCasts, int maxShownAOEs = 2, ulong actorID = default)
    {
        public AOEShape Shape = shape;
        public WPos Origin = origin;
        public Angle Rotation = rotation;
        public Angle Increment = increment;
        public DateTime NextActivation = nextActivation;
        public double SecondsBetweenActivations = secondsBetweenActivations;
        public int NumRemainingCasts = numRemainingCasts;
        public int MaxShownAOEs = maxShownAOEs;
        public ulong ActorID = actorID;
    }

    public readonly List<Sequence> Sequences = [];
    public uint ImminentColor = Colors.AOEImminent;
    public uint FutureColor = Colors.AOE;
    protected readonly List<AOEInstance> Aoes = [];
    protected int lastVersion, lastCount;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(this.Aoes);

    public override void Update()
    {
        var count = this.Sequences.Count;
        if (this.lastCount == count && this.lastVersion == this.NumCasts)
            return;
        this.lastCount = count;
        this.lastVersion = this.NumCasts;
        this.Aoes.Clear();
        if (count == 0)
            return;

        var now = this.World.CurrentTime;
        var sequences = CollectionsMarshal.AsSpan(this.Sequences);
        foreach (ref var s in sequences)
        {
            var num = Math.Min(s.NumRemainingCasts, s.MaxShownAOEs);
            var rot = s.Rotation;
            var time = s.NextActivation > now ? s.NextActivation : now;
            for (var i = 1; i < num; ++i)
            {
                rot += s.Increment;
                time = time.AddSeconds(s.SecondsBetweenActivations);
                this.Aoes.Add(new AOEInstance(s.Shape, s.Origin, rot, time, this.FutureColor));
            }
            if (s.NumRemainingCasts != 0)
                this.Aoes.Add(new AOEInstance(s.Shape, s.Origin, s.Rotation, s.NextActivation, s.NumRemainingCasts > 1 ? this.ImminentColor : this.FutureColor));
        }
    }

    public void AdvanceSequence(int index, DateTime currentTime, bool removeWhenFinished = true)
    {
        ++this.NumCasts;
        if (index < 0 || index >= this.Sequences.Count)
            return;
        var sequences = CollectionsMarshal.AsSpan(this.Sequences);
        ref var s = ref sequences[index];
        if (--s.NumRemainingCasts <= 0 && removeWhenFinished)
        {
            this.Sequences.RemoveAt(index);
        }
        else
        {
            s.Rotation += s.Increment;
            s.NextActivation = currentTime.AddSeconds(s.SecondsBetweenActivations);
        }
    }

    public bool AdvanceSequence(WPos origin, Angle rotation, DateTime currentTime, bool removeWhenFinished = true)
    {
        for (var i = 0; i < this.Sequences.Count; ++i)
        {
            var s = this.Sequences[i];
            if (s.Origin.AlmostEqual(origin, 1f) && s.Rotation.AlmostEqual(rotation, 0.05f))
            {
                this.AdvanceSequence(i, currentTime, removeWhenFinished);
                return true;
            }
        }
        return false;
    }

    public bool AdvanceSequence(ulong instanceID, DateTime currentTime, bool removeWhenFinished = true)
    {
        for (var i = 0; i < this.Sequences.Count; ++i)
        {
            if (this.Sequences[i].ActorID == instanceID)
            {
                this.AdvanceSequence(i, currentTime, removeWhenFinished);
                return true;
            }
        }
        return false;
    }
}
