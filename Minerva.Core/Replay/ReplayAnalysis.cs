using System.Globalization;
using System.Text;
using Minerva.Generation;

namespace Minerva;

/// <summary>
/// Observes a replayed <see cref="WorldState"/> and mines — and correlates — the raw material for a
/// boss module. Beyond the basic cast/shape facts, it tracks per-action player-target sets and
/// simultaneity, the overhead icon / tether seen just before each cast, object lifetimes (to flag
/// voidzone-style hazards), and boss-swap phase boundaries. <see cref="BuildGenerationInput"/>
/// turns all of that into structured facts the generator classifies into components. Fully offline
/// and deterministic — no LLM, no network.
/// </summary>
public sealed class ReplayAnalysis
{
    private const float BossHitbox = 7f;      // enemies at least this big count as a "boss form" (phase marker)
    private const double CorrelationWindow = 8d; // seconds an icon/tether can precede its cast
    private const double VoidzoneMinLifetime = 4d;
    private const float StackRadius = 6f;        // how close other players must be to count as "stacked"
    private const int StackClusterMin = 2;       // this many other players piled on the target => stack, not spread
    private const double KnockbackCheckDelay = 1d; // seconds after resolution to measure a push
    private const float KnockbackMinDistance = 3f; // players must be shoved at least this far to count
    private const float ConcentricOriginEps = 1f;  // same-origin tolerance for a concentric bullseye
    private const double ConcentricMaxGap = 2.5d;  // max seconds between rings of one bullseye
    private const double ArenaMarkerMinTime = 5d;    // an environment object appearing after this may mark an arena change
    private const double MapEffectPhaseMinTime = 5d; // ignore map effects during fight setup (decorations)
    private const double PhaseCoincidenceWindow = 3d; // a map effect this close to a targetable/HP boundary is the same phase

    private sealed class ObjInfo
    {
        public string Name = "";
        public float HitboxRadius;
        public int Count;
        public int Casts;
        public ActorType Type;
        public double FirstSeen;
        public double LastSeen;
    }

    private sealed class ActInfo
    {
        public uint CasterOID;
        public string CasterName = "";
        public TargetKind Target;
        public float CastTime;
        public int Count;
        public readonly HashSet<ulong> PlayerTargets = [];
        public readonly Dictionary<long, int> PerBucketCasts = []; // time-bucket -> casts (simultaneity)
        public uint PrecedingIcon;
        public uint PrecedingTether;
        public double FirstSeen;
        // spatial spread of location-targeted casts — a marching line (exaflare) covers ground, a fixed puddle doesn't
        public float LocMinX = float.MaxValue, LocMaxX = float.MinValue, LocMinZ = float.MaxValue, LocMaxZ = float.MinValue;
        public int LocSamples;

        public float LocationSpread => this.LocSamples >= 2
            ? MathF.Sqrt((this.LocMaxX - this.LocMinX) * (this.LocMaxX - this.LocMinX) + (this.LocMaxZ - this.LocMinZ) * (this.LocMaxZ - this.LocMinZ))
            : 0f;

        // correlated at resolution (cast finish): stack clustering, gaze facing, knockback push
        public int MaxCluster;          // most other players piled on the target at any resolution (stack signal)
        public int GazeVotes;           // resolutions where most players faced away from the caster
        public int ResolveSamples;      // self/ground resolutions observed (denominator for the gaze vote)
        public float KnockbackDistance; // measured radial shove, 0 = none
        public bool Concentric;         // one ring of a same-origin bullseye
    }

    private readonly WorldState ws;
    private readonly Dictionary<uint, ObjInfo> objects = [];
    private readonly Dictionary<uint, ActInfo> actions = [];
    private readonly SortedSet<uint> statuses = [];
    private readonly SortedSet<uint> icons = [];
    private readonly SortedSet<uint> tethers = [];
    private readonly Dictionary<byte, SortedSet<uint>> mapEffects = [];
    // per map-effect index: how many times it fired, and the state/time of its first firing (for one-shot phase detection)
    private readonly Dictionary<byte, (int count, uint firstState, double firstTime)> mapEffectTimeline = [];
    private readonly List<(uint oid, double time, PhaseTrigger trigger, float hp)> bossTimeline = [];
    private readonly HashSet<uint> bossFormsSeen = [];        // boss OIDs that have appeared (targetable) at least once
    private readonly Dictionary<uint, float> untargetableHP = []; // boss OID -> HP fraction when it last went untargetable

    private uint lastIcon;
    private double lastIconTime = double.NegativeInfinity;
    private uint lastTether;
    private double lastTetherTime = double.NegativeInfinity;

    // every ground-targeted cast's (origin, aid, time) — swept into concentric bullseye runs at build time
    private readonly List<(WPos origin, uint aid, double time)> castTimeline = [];
    // deferred knockback measurements: snapshot player distances at resolution, compare a beat later
    private readonly List<(uint aid, WPos origin, double due, Dictionary<ulong, float> before)> knockbackChecks = [];

    private float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
    private double sumX, sumZ;
    private int posSamples;
    private DateTime firstTime, lastTime;

    public static ReplayAnalysis Attach(WorldState ws) => new(ws);

    private ReplayAnalysis(WorldState ws)
    {
        this.ws = ws;
        ws.Actors.Added.Subscribe(this.OnAdded);
        ws.Actors.Removed.Subscribe(this.OnRemoved);
        ws.Actors.IsTargetableChanged.Subscribe(this.OnTargetable);
        ws.Actors.CastStarted.Subscribe(this.OnCastStarted);
        ws.Actors.CastFinished.Subscribe(this.OnCastFinished);
        ws.Actors.StatusGain.Subscribe((a, i) => this.statuses.Add(a.Statuses[i].ID));
        ws.Actors.Tethered.Subscribe(this.OnTethered);
        ws.Actors.IconAppeared.Subscribe(this.OnIcon);
        ws.MapEffect.Subscribe(this.OnMapEffect);
        ws.FrameStarted.Subscribe(this.OnFrame);
    }

    private double Now => (this.ws.CurrentTime - this.firstTime).TotalSeconds;

    private void OnFrame(WorldState.OpFrameStart op)
    {
        if (this.firstTime == default)
            this.firstTime = op.Frame.Timestamp;
        this.lastTime = op.Frame.Timestamp;
        this.ProcessKnockbackChecks((op.Frame.Timestamp - this.firstTime).TotalSeconds);
    }

    // resolve any knockback snapshots whose delay has elapsed: a shove pushes several players outward together
    private void ProcessKnockbackChecks(double now)
    {
        for (var i = this.knockbackChecks.Count - 1; i >= 0; --i)
        {
            var chk = this.knockbackChecks[i];
            if (now < chk.due)
                continue;
            this.knockbackChecks.RemoveAt(i);

            var deltas = new List<float>();
            foreach (var (id, distBefore) in chk.before)
            {
                var a = this.ws.Actors.Find(id);
                if (a != null)
                    deltas.Add((a.Position - chk.origin).Length() - distBefore);
            }
            if (deltas.Count < 2)
                continue;
            deltas.Sort();
            var median = deltas[deltas.Count / 2];
            var consistent = 0;
            foreach (var d in deltas)
                if (d >= KnockbackMinDistance && MathF.Abs(d - median) <= 2f)
                    consistent++;
            if (median >= KnockbackMinDistance && consistent >= 2 && this.actions.TryGetValue(chk.aid, out var info))
                info.KnockbackDistance = MathF.Max(info.KnockbackDistance, MathF.Round(median));
        }
    }

    private void OnAdded(Actor a)
    {
        if (a.Type is ActorType.Player or ActorType.Pet or ActorType.Chocobo)
            return;
        if (!this.objects.TryGetValue(a.OID, out var info))
            this.objects[a.OID] = info = new ObjInfo { Name = a.Name, HitboxRadius = a.HitboxRadius, Type = a.Type, FirstSeen = this.Now };
        info.Count++;
        info.LastSeen = this.Now;
    }

    private void OnRemoved(Actor a)
    {
        if (this.objects.TryGetValue(a.OID, out var info))
            info.LastSeen = this.Now;
    }

    private void OnTargetable(Actor a)
    {
        if (a.Type != ActorType.Enemy || a.HitboxRadius < BossHitbox)
            return;
        if (a.IsTargetable)
        {
            if (this.bossFormsSeen.Add(a.OID))
                this.bossTimeline.Add((a.OID, this.Now, PhaseTrigger.Targetable, 0f)); // a new boss form appeared
            else if (this.untargetableHP.Remove(a.OID, out var hp))
                this.bossTimeline.Add((a.OID, this.Now, PhaseTrigger.PrimaryHP, hp)); // same boss returned -> HP gate at hp
        }
        else
        {
            this.untargetableHP[a.OID] = a.HPRatio; // remember the HP it disengaged at, in case it comes back
        }
    }

    private void OnIcon(Actor actor, ActorIconEvent e)
    {
        this.icons.Add(e.IconID);
        this.lastIcon = e.IconID;
        this.lastIconTime = this.Now;
    }

    private void OnTethered(Actor a)
    {
        this.tethers.Add(a.Tether.ID);
        this.lastTether = a.Tether.ID;
        this.lastTetherTime = this.Now;
    }

    private void OnMapEffect(WorldState.OpMapEffect op)
    {
        if (!this.mapEffects.TryGetValue(op.Index, out var set))
            this.mapEffects[op.Index] = set = [];
        set.Add(op.State);

        // track occurrences so a one-shot mid-fight arena change can be a phase boundary
        this.mapEffectTimeline[op.Index] = this.mapEffectTimeline.TryGetValue(op.Index, out var info)
            ? (info.count + 1, info.firstState, info.firstTime)
            : (1, op.State, this.Now);
    }

    private void OnCastStarted(Actor caster)
    {
        var cast = caster.CastInfo;
        if (cast == null || cast.Action.ID == 0)
            return;
        // ignore other players' (and their pets') own abilities — in open-field content they flood the
        // action list; we only want enemy/helper mechanics
        if (caster.Type is ActorType.Player or ActorType.Pet or ActorType.Chocobo)
            return;
        var aid = cast.Action.ID;
        var now = this.Now;

        if (!this.actions.TryGetValue(aid, out var info))
        {
            this.actions[aid] = info = new ActInfo
            {
                CasterOID = caster.OID,
                CasterName = caster.Name.Length > 0 ? caster.Name : $"OID{caster.OID:X}",
                CastTime = cast.TotalTime,
                Target = this.ClassifyTarget(caster, cast),
                FirstSeen = now,
            };
            // associate the most recent icon/tether if it fell within the correlation window
            if (now - this.lastIconTime <= CorrelationWindow)
                info.PrecedingIcon = this.lastIcon;
            if (now - this.lastTetherTime <= CorrelationWindow)
                info.PrecedingTether = this.lastTether;
        }
        info.Count++;

        if (info.Target == TargetKind.Player && cast.TargetID != 0)
            info.PlayerTargets.Add(cast.TargetID);

        var bucket = (long)Math.Round(now);
        info.PerBucketCasts[bucket] = info.PerBucketCasts.GetValueOrDefault(bucket) + 1;

        if (cast.LocXZ != default)
        {
            info.LocMinX = MathF.Min(info.LocMinX, cast.LocXZ.X);
            info.LocMaxX = MathF.Max(info.LocMaxX, cast.LocXZ.X);
            info.LocMinZ = MathF.Min(info.LocMinZ, cast.LocXZ.Z);
            info.LocMaxZ = MathF.Max(info.LocMaxZ, cast.LocXZ.Z);
            info.LocSamples++;
            this.castTimeline.Add((cast.LocXZ, aid, now)); // ground casts feed concentric-bullseye grouping
        }

        if (this.objects.TryGetValue(caster.OID, out var obj))
            obj.Casts++;

        this.Sample(cast.LocXZ != default ? cast.LocXZ : caster.Position);
    }

    // at resolution, correlate the things you can only see when players have committed: whether the party
    // piled on the target (stack), turned away from the caster (gaze), or was about to be shoved (knockback)
    private void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        var aid = cast.Action.ID;
        if (aid == 0 || !this.actions.TryGetValue(aid, out var info))
            return;
        var origin = cast.LocXZ != default ? cast.LocXZ : caster.Position;

        if (info.Target == TargetKind.Player)
        {
            // stack: count other players clustered on the marked player
            var target = this.ws.Actors.Find(cast.TargetID);
            if (target != null)
            {
                var cluster = 0;
                foreach (var a in this.ws.Actors)
                    if (a.Type == ActorType.Player && a.InstanceID != target.InstanceID && a.Position.InCircle(target.Position, StackRadius))
                        cluster++;
                info.MaxCluster = Math.Max(info.MaxCluster, cluster);
            }
            return;
        }

        // self/ground cast: gaze (players facing away) + snapshot for a deferred knockback measurement
        int away = 0, players = 0;
        var before = new Dictionary<ulong, float>();
        foreach (var a in this.ws.Actors)
        {
            if (a.Type != ActorType.Player)
                continue;
            players++;
            before[a.InstanceID] = (a.Position - origin).Length();
            var toCaster = (caster.Position - a.Position).Normalized();
            if (toCaster != default && WDir.Dot(a.Rotation.ToDirection(), toCaster) < 0f)
                away++;
        }
        info.ResolveSamples++;
        if (players > 0 && away >= 2 && away * 2 >= players)
            info.GazeVotes++;
        if (before.Count > 0)
            this.knockbackChecks.Add((aid, origin, this.Now + KnockbackCheckDelay, before));
    }

    // Merge every phase boundary into one time-ordered list: the targetable/HP boss timeline plus any
    // conservative one-shot map effect (a mid-fight arena change that doesn't coincide with a stronger boundary).
    private List<(double time, PhaseTrigger trigger, uint oid, float hp, byte mapIndex, uint mapState)> PhaseBoundaries()
    {
        var list = new List<(double time, PhaseTrigger trigger, uint oid, float hp, byte mapIndex, uint mapState)>();
        foreach (var e in this.bossTimeline)
            list.Add((e.time, e.trigger, e.oid, e.hp, (byte)0, 0u));

        foreach (var (index, info) in this.mapEffectTimeline)
        {
            // only a one-shot, non-zero, mid-fight arena change counts (telegraph tiles recur; setup fires early)
            if (info.count != 1 || info.firstState == 0 || info.firstTime < MapEffectPhaseMinTime)
                continue;
            var coincides = false;
            foreach (var b in list)
                if (Math.Abs(b.time - info.firstTime) < PhaseCoincidenceWindow) { coincides = true; break; }
            if (!coincides)
                list.Add((info.firstTime, PhaseTrigger.MapEffect, 0u, 0f, index, info.firstState));
        }

        list.Sort((a, b) => a.time.CompareTo(b.time));
        return list;
    }

    private TargetKind ClassifyTarget(Actor caster, ActorCastInfo cast)
    {
        if (cast.TargetID == caster.InstanceID || cast.TargetID == 0)
            return cast.LocXZ != default ? TargetKind.Location : TargetKind.Self;
        var target = this.ws.Actors.Find(cast.TargetID);
        if (target != null && target.Type == ActorType.Player)
            return TargetKind.Player;
        return TargetKind.Target;
    }

    private void Sample(WPos p)
    {
        this.minX = MathF.Min(this.minX, p.X);
        this.maxX = MathF.Max(this.maxX, p.X);
        this.minZ = MathF.Min(this.minZ, p.Z);
        this.maxZ = MathF.Max(this.maxZ, p.Z);
        this.sumX += p.X;
        this.sumZ += p.Z;
        this.posSamples++;
    }

    public int ActionCount => this.actions.Count;
    public int ObjectCount => this.objects.Count;

    // sweep the ground-cast timeline for concentric bullseyes: >=3 rapid casts at one spot spanning >=2 actions
    private void MarkConcentric()
    {
        if (this.castTimeline.Count < 3)
            return;
        var casts = new List<(WPos origin, uint aid, double time)>(this.castTimeline);
        casts.Sort((a, b) => a.time.CompareTo(b.time));
        var i = 0;
        while (i < casts.Count)
        {
            var j = i + 1;
            var distinct = new HashSet<uint> { casts[i].aid };
            while (j < casts.Count
                   && casts[j].origin.AlmostEqual(casts[j - 1].origin, ConcentricOriginEps)
                   && casts[j].time - casts[j - 1].time <= ConcentricMaxGap)
            {
                distinct.Add(casts[j].aid);
                j++;
            }
            if (j - i >= 3 && distinct.Count >= 2)
                for (var k = i; k < j; k++)
                    if (this.actions.TryGetValue(casts[k].aid, out var ci))
                        ci.Concentric = true;
            i = j;
        }
    }

    public GenerationInput BuildGenerationInput()
    {
        this.MarkConcentric();

        // boss = first phase form if we saw one; else the enemy that cast the most mechanics (tie-broken by
        // hitbox). Open-field bosses never fire a targetable phase marker, so the fallback carries them.
        uint bossOID = this.bossTimeline.Count > 0 ? this.bossTimeline[0].oid : 0;
        var bossName = "Boss";
        if (bossOID != 0 && this.objects.TryGetValue(bossOID, out var b0))
        {
            bossName = b0.Name;
        }
        else
        {
            // the boss is usually the biggest enemy that actually casts — NOT the busiest add. (Add-driven
            // fights, e.g. Occult Crescent CEs, otherwise pick the add that spams the most AOEs.)
            ObjInfo? best = null;
            foreach (var (oid, info) in this.objects)
            {
                if (info.Type != ActorType.Enemy)
                    continue;
                if (best == null || IsBetterBoss(info, best))
                {
                    best = info;
                    bossOID = oid;
                    bossName = info.Name;
                }
            }
        }

        // merged, time-ordered phase boundaries (targetable / HP gate / map effect); a cast's phase is the
        // last boundary at or before its first appearance
        var boundaries = this.PhaseBoundaries();
        int PhaseOf(double time)
        {
            var p = 0;
            for (var i = 0; i < boundaries.Count; ++i)
                if (boundaries[i].time <= time)
                    p = i;
            return p;
        }

        var objs = new List<ObjectFact>();
        foreach (var (oid, info) in this.objects)
        {
            var lifetime = info.LastSeen - info.FirstSeen;
            var voidzone = oid != bossOID && info.Casts == 0 && info.HitboxRadius is >= 1f and <= 15f
                && lifetime >= VoidzoneMinLifetime && info.Type is ActorType.Enemy or ActorType.EventObj;
            // an environment object appearing mid-fight often marks an arena change (wall/deathwall/floor)
            var arenaMarker = oid != bossOID && info.Type == ActorType.EventObj && info.FirstSeen >= ArenaMarkerMinTime;
            objs.Add(new ObjectFact(oid, info.Name, info.HitboxRadius, info.Count, info.Casts, lifetime, voidzone, arenaMarker));
        }

        var acts = new List<ActionFact>();
        foreach (var (aid, info) in this.actions)
        {
            var maxSim = 0;
            foreach (var v in info.PerBucketCasts.Values)
                maxSim = Math.Max(maxSim, v);
            var mech = this.ClassifyPlayerMechanic(info, maxSim);
            var exaflare = info.Target == TargetKind.Location && info.Count >= 4 && info.LocationSpread >= 4f;
            var gaze = info.Target != TargetKind.Player && info.GazeVotes > 0;
            acts.Add(new ActionFact(aid, info.CasterOID, info.CasterName, info.Target, info.CastTime, info.Count,
                info.PlayerTargets.Count, maxSim, mech, info.PrecedingIcon, info.PrecedingTether, PhaseOf(info.FirstSeen),
                exaflare, info.Concentric, gaze, info.KnockbackDistance));
        }

        var center = this.posSamples > 0 ? new WPos((float)(this.sumX / this.posSamples), (float)(this.sumZ / this.posSamples)) : default;
        var arena = new ArenaEstimate(center,
            this.posSamples > 0 ? this.minX : 0f, this.posSamples > 0 ? this.maxX : 0f,
            this.posSamples > 0 ? this.minZ : 0f, this.posSamples > 0 ? this.maxZ : 0f);

        var phases = new List<PhaseFact>();
        for (var i = 0; i < boundaries.Count; ++i)
        {
            var b = boundaries[i];
            phases.Add(new PhaseFact(i, $"P{i + 1}", b.time, b.oid, b.trigger, b.hp, b.mapIndex, b.mapState));
        }

        return new GenerationInput
        {
            Zone = this.ws.CurrentZone,
            CFCID = this.ws.CurrentCFCID,
            BossOID = bossOID,
            BossName = bossName,
            Objects = objs,
            Actions = acts,
            Arena = arena,
            Phases = phases,
            Statuses = [.. this.statuses],
            Tethers = [.. this.tethers],
            Icons = [.. this.icons],
        };
    }

    // prefer a caster over a non-caster, then the bigger hitbox (the boss is usually largest), then more casts
    private static bool IsBetterBoss(ObjInfo a, ObjInfo b)
    {
        var aCasts = a.Casts > 0;
        var bCasts = b.Casts > 0;
        if (aCasts != bCasts)
            return aCasts;
        if (a.HitboxRadius != b.HitboxRadius)
            return a.HitboxRadius > b.HitboxRadius;
        return a.Casts > b.Casts;
    }

    private PlayerMechanic ClassifyPlayerMechanic(ActInfo info, int maxSimultaneous)
    {
        if (info.Target != TargetKind.Player)
            return PlayerMechanic.None;
        if (info.PrecedingTether != 0)
            return PlayerMechanic.Bait;
        if (info.MaxCluster >= StackClusterMin)
            return PlayerMechanic.Stack;                  // party piled onto the marked player
        if (maxSimultaneous >= 2)
            return PlayerMechanic.Spread;                 // several players marked at once
        if (info.PlayerTargets.Count == 1 && info.Count >= 2)
            return PlayerMechanic.Tankbuster;             // always the same (tank) target
        return PlayerMechanic.Spread;                     // rotating / icon-driven single targets (review for stack)
    }

    public string Render()
    {
        var inv = CultureInfo.InvariantCulture;
        var b = new StringBuilder();
        var input = this.BuildGenerationInput();
        var duration = (this.lastTime - this.firstTime).TotalSeconds;
        b.AppendLine("== Minerva fact sheet ==");
        b.AppendLine($"zone {input.Zone}  cfc {input.CFCID}  boss '{input.BossName}' 0x{input.BossOID:X}  duration {duration.ToString("f1", inv)}s  phases {Math.Max(1, input.Phases.Count)}");
        b.AppendLine();

        b.AppendLine("Objects:");
        foreach (var o in input.Objects)
            b.AppendLine($"  0x{o.OID:X} '{o.Name}' R{o.HitboxRadius.ToString("f2", inv)}  x{o.Count} casts{o.Casts}{(o.VoidzoneCandidate ? "  [voidzone?]" : "")}");
        b.AppendLine();

        b.AppendLine("Actions:");
        foreach (var a in input.Actions)
        {
            var tag = a.PlayerMechanic != PlayerMechanic.None ? $" {a.PlayerMechanic}" : "";
            var icon = a.PrecedingIcon != 0 ? $" icon:{a.PrecedingIcon}" : "";
            var teth = a.PrecedingTether != 0 ? $" tether:{a.PrecedingTether}" : "";
            b.AppendLine($"  {a.AID} {a.CasterName}->{a.Target}{tag} {a.CastTime.ToString("f1", inv)}s x{a.Count} P{a.Phase + 1}{icon}{teth}");
        }
        b.AppendLine();

        if (this.posSamples > 0)
            b.AppendLine($"Arena: center ~[{input.Arena.Center.X.ToString("f1", inv)}, {input.Arena.Center.Z.ToString("f1", inv)}]  halfExtent {input.Arena.HalfExtent.ToString("f1", inv)}  {(input.Arena.LooksSquare ? "square" : "circle")}");
        if (this.statuses.Count > 0)
            b.AppendLine("Statuses (SID): " + string.Join(", ", this.statuses));
        if (this.tethers.Count > 0)
            b.AppendLine("Tethers (TetherID): " + string.Join(", ", this.tethers));
        if (this.icons.Count > 0)
            b.AppendLine("Icons (IconID): " + string.Join(", ", this.icons));
        return b.ToString();
    }
}
