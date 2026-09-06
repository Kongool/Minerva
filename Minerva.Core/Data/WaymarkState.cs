namespace Minerva;

/// <summary>The eight field markers, in the game's order: A-D then 1-4.</summary>
public enum Waymark : byte
{
    A, B, C, D, N1, N2, N3, N4, Count,
}

/// <summary>
/// Where the party's field markers are placed. Ported from BossmodReborn's <c>WaymarkState</c> (BSD-3; see
/// THIRD-PARTY-NOTICES.txt).
///
/// <para>Markers are how a raid writes its plan onto the floor — "stack on A", "towers on 1 and 2" — so a
/// module that reads them resolves a mechanic the way that particular party actually plays it, rather than
/// the way its author did. That is the difference between advice you can follow and advice you have to
/// ignore.</para>
///
/// <para>A marker that is not placed reads as null rather than as the origin. A module asking for an
/// unplaced marker gets nothing and skips its hint, which is right: (0,0) is a real spot on many arenas and
/// would send people to a corner.</para>
/// </summary>
public sealed class WaymarkState
{
    private BitMask set;
    private readonly Vector3[] positions = new Vector3[(int)Waymark.Count];

    /// <summary>Whether any marker is placed at all.</summary>
    public bool AnyWaymarks => this.set.Any();

    public Vector3? this[Waymark wm]
    {
        get => (int)wm >= 0 && (int)wm < (int)Waymark.Count && this.set[(int)wm] ? this.positions[(int)wm] : null;
        set
        {
            if ((int)wm < 0 || (int)wm >= (int)Waymark.Count)
                return;
            this.set[(int)wm] = value != null;
            this.positions[(int)wm] = value ?? default;
        }
    }

    /// <summary>Marker by index, as ported modules address it.</summary>
    public Vector3? GetFieldMark(int id) => this[(Waymark)id];

    public List<WorldState.Operation> CompareToInitial()
    {
        List<WorldState.Operation> ops = [];
        for (var i = 0; i < (int)Waymark.Count; ++i)
            if (this.set[i])
                ops.Add(new OpWaymarkChange((Waymark)i, this.positions[i]));
        return ops;
    }

    public readonly Event<OpWaymarkChange> Changed = new();

    public sealed class OpWaymarkChange(Waymark id, Vector3? pos) : WorldState.Operation
    {
        public readonly Waymark ID = id;
        public readonly Vector3? Pos = pos;

        protected override void Exec(WorldState ws)
        {
            ws.Waymarks[this.ID] = this.Pos;
            ws.Waymarks.Changed.Fire(this);
        }

        public override void Write(OperationOutput o)
        {
            if (this.Pos is { } p)
                o.Tag("WAY+").Emit((uint)this.ID).Emit(p.X).Emit(p.Y).Emit(p.Z);
            else
                o.Tag("WAY-").Emit((uint)this.ID);
        }
    }
}
