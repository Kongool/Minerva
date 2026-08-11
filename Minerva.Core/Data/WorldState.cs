namespace Minerva;

/// <summary>
/// The parts of the game world Minerva cares about, as a plain data model with no knowledge of
/// Dalamud. It is mutated exclusively through <see cref="Operation"/>s: each op applies a change
/// and is announced on <see cref="Modified"/>, which (a) lets any system observe state changes
/// and (b) makes the entire state stream recordable and replayable. A separate game-sync layer
/// (Phase 2) produces the op stream live; a replay reader (Phase 4) produces the same op stream
/// from a file. Neither this class nor its ops know which.
/// </summary>
public sealed class WorldState
{
    // --- state ---
    public readonly ulong QPF;
    public readonly string GameVersion;
    public FrameState Frame;
    public ushort CurrentZone;
    public ushort CurrentCFCID;
    public readonly Dictionary<string, string> RSVEntries = [];
    public readonly ActorState Actors = new();
    public readonly PartyState Party;

    public DateTime CurrentTime => this.Frame.Timestamp;
    public DateTime FutureTime(double deltaSeconds) => this.Frame.Timestamp.AddSeconds(deltaSeconds);

    public WorldState(ulong qpf, string gameVersion)
    {
        this.QPF = qpf;
        this.GameVersion = gameVersion;
        this.Party = new(this.Actors);
    }

    // --- modification ---
    public readonly Event<Operation> Modified = new();

    /// <summary>A single, self-applying, self-serializing change to the world state.</summary>
    public abstract class Operation
    {
        /// <summary>Filled with <see cref="CurrentTime"/> when executed; convenient for replays.</summary>
        public DateTime Timestamp;

        internal void Execute(WorldState ws)
        {
            this.Exec(ws);
            this.Timestamp = ws.CurrentTime;
        }

        protected abstract void Exec(WorldState ws);

        /// <summary>Serialize this op (see <see cref="OperationOutput"/>).</summary>
        public abstract void Write(OperationOutput o);

        public override string ToString()
        {
            var o = new OperationOutput();
            this.Write(o);
            return o.ToString();
        }
    }

    public void Execute(Operation op)
    {
        op.Execute(this);
        this.Modified.Fire(op);
    }

    /// <summary>Ops that would rebuild the current state from a default-constructed one (snapshot).</summary>
    public List<Operation> CompareToInitial()
    {
        List<Operation> ops = [];
        if (this.CurrentTime != default)
            ops.Add(new OpFrameStart(this.Frame, default));
        if (this.CurrentZone != default || this.CurrentCFCID != default)
            ops.Add(new OpZoneChange(this.CurrentZone, this.CurrentCFCID));
        foreach (var (k, v) in this.RSVEntries)
            ops.Add(new OpRSVData(k, v));
        ops.AddRange(this.Actors.CompareToInitial());
        ops.AddRange(this.Party.CompareToInitial());
        return ops;
    }

    // --- global operations ---
    public readonly Event<OpFrameStart> FrameStarted = new();
    public sealed class OpFrameStart(in FrameState frame, TimeSpan prevUpdateTime) : Operation
    {
        public readonly FrameState Frame = frame;
        public readonly TimeSpan PrevUpdateTime = prevUpdateTime;

        protected override void Exec(WorldState ws)
        {
            ws.Frame = this.Frame;
            ws.Actors.Tick(this.Frame);
            ws.FrameStarted.Fire(this);
        }
        public override void Write(OperationOutput o) => o.Tag("FRAM").Emit(this.PrevUpdateTime.TotalMilliseconds.ToString("f3")).Emit((ulong)this.Frame.Timestamp.Ticks).Emit(this.Frame.QPC).Emit(this.Frame.Index).Emit(this.Frame.Duration);
    }

    public readonly Event<OpZoneChange> CurrentZoneChanged = new();
    public sealed class OpZoneChange(ushort zone, ushort cfcID) : Operation
    {
        public readonly ushort Zone = zone;
        public readonly ushort CFCID = cfcID;

        protected override void Exec(WorldState ws)
        {
            ws.CurrentZone = this.Zone;
            ws.CurrentCFCID = this.CFCID;
            ws.CurrentZoneChanged.Fire(this);
        }
        public override void Write(OperationOutput o) => o.Tag("ZONE").Emit((uint)this.Zone).Emit((uint)this.CFCID);
    }

    /// <summary>
    /// Arena environment change (ENVC packet): a slot <paramref name="Index"/> takes a new
    /// <paramref name="State"/>. Transient — many mechanics key off these (moving walls, tile
    /// hazards, arena reshaping). Event-only.
    /// </summary>
    public readonly Event<OpMapEffect> MapEffect = new();
    public sealed class OpMapEffect(byte index, uint state) : Operation
    {
        public readonly byte Index = index;
        public readonly uint State = state;

        protected override void Exec(WorldState ws) => ws.MapEffect.Fire(this);
        public override void Write(OperationOutput o) => o.Tag("ENVC").Emit((uint)this.Index, "X2").Emit(this.State, "X8");
    }

    /// <summary>Director state update (boss-fight controller): ids + up to four params. Event-only.</summary>
    public readonly Event<OpDirectorUpdate> DirectorUpdate = new();
    public sealed class OpDirectorUpdate(uint directorID, uint updateID, uint p1, uint p2, uint p3, uint p4) : Operation
    {
        public readonly uint DirectorID = directorID;
        public readonly uint UpdateID = updateID;
        public readonly uint Param1 = p1;
        public readonly uint Param2 = p2;
        public readonly uint Param3 = p3;
        public readonly uint Param4 = p4;

        protected override void Exec(WorldState ws) => ws.DirectorUpdate.Fire(this);
        public override void Write(OperationOutput o) => o.Tag("DIRU").Emit(this.DirectorID, "X8").Emit(this.UpdateID, "X8").Emit(this.Param1, "X8").Emit(this.Param2, "X8").Emit(this.Param3, "X8").Emit(this.Param4, "X8");
    }

    /// <summary>Restricted-string-value data: resolves an RSV key to its literal (some action/status names arrive this way).</summary>
    public readonly Event<OpRSVData> RSVDataReceived = new();
    public sealed class OpRSVData(string key, string value) : Operation
    {
        public readonly string Key = key;
        public readonly string Value = value;

        protected override void Exec(WorldState ws)
        {
            ws.RSVEntries[this.Key] = this.Value;
            ws.RSVDataReceived.Fire(this);
        }
        public override void Write(OperationOutput o) => o.Tag("RSV ").Emit(this.Key).Emit(this.Value);
    }
}
