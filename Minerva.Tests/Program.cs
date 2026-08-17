using System.Collections.Generic;
using System.Numerics;
using Minerva;

// Minerva.Core self-test. Drives a hand-built op stream through a WorldState and asserts the
// result, with no game and no Dalamud running — the Phase 1 milestone. Returns non-zero on
// failure so it can gate CI/builds later.

var t = new Harness();

// ---------------------------------------------------------------------------
// 1. Geometry sanity (coordinate convention: 0 = south, CCW; +90 = east)
// ---------------------------------------------------------------------------
t.Section("Geometry");
{
    var east = 90f.Degrees().ToDirection();
    t.Near("90deg points east (+X)", east.X, 1f);
    t.Near("90deg has ~0 Z", east.Z, 0f);

    var south = 0f.Degrees().ToDirection();
    t.Near("0deg points south (-Z)", south.Z, 1f); // ToDirection = (sin,cos): 0 -> (0,1)

    var a = new WPos(0f, 0f);
    var b = new WPos(3f, 4f);
    t.Near("distance 3-4-5", (b - a).Length(), 5f);
    t.True("InCircle inside", b.InCircle(a, 5.01f));
    t.True("InCircle outside", !b.InCircle(a, 4.99f));

    var wrapped = 270f.Degrees().Normalized();
    t.Near("270deg normalizes to -90", wrapped.Deg, -90f, 0.01f);
}

// ---------------------------------------------------------------------------
// 1b. Arena bounds shapes: donut ring + arbitrary polygon (varied arena outlines)
// ---------------------------------------------------------------------------
t.Section("Arena bounds shapes");
{
    var c = new WPos(100, 100);

    // Donut ring: contained between inner and outer, excluded in the hole and outside
    var donut = new ArenaBoundsDonut(5f, 20f);
    t.True("donut contains a point on the rim", donut.Contains(c, new WPos(110, 100)));  // 10y from center
    t.True("donut excludes the central hole", !donut.Contains(c, new WPos(103, 100)));   // 3y < inner 5
    t.True("donut excludes outside the rim", !donut.Contains(c, new WPos(125, 100)));    // 25y > outer 20
    t.Near("donut radius is the outer radius", donut.Radius, 20f);
    t.NotNull("donut draws an inner boundary loop", donut.InnerContour(c));

    // Polygon: an L-shape (full square with the top-right quadrant cut out) — exercises concavity
    var poly = new ArenaBoundsPolygon(
        new WDir(-10, -10), new WDir(0, -10), new WDir(0, 0), new WDir(10, 0), new WDir(10, 10), new WDir(-10, 10));
    t.True("polygon contains a solid quadrant", poly.Contains(c, new WPos(95, 95)));      // top-left, inside
    t.True("polygon excludes the cut-out quadrant", !poly.Contains(c, new WPos(105, 95))); // top-right, removed
    t.True("polygon contains the bottom-right", poly.Contains(c, new WPos(105, 105)));    // inside
    t.True("polygon excludes a point outside the hull", !poly.Contains(c, new WPos(130, 100)));
    t.Near("polygon radius is the farthest vertex", poly.Radius, MathF.Sqrt(200f));
    t.Eq("polygon contour has one point per vertex", poly.Contour(c).Count, 6);
    t.True("solid arena has no inner loop", new ArenaBoundsCircle(20f).InnerContour(c) == null);
}

// ---------------------------------------------------------------------------
// 2. WorldState op stream: create -> move -> cast -> status -> die -> destroy
// ---------------------------------------------------------------------------
t.Section("WorldState op stream");
{
    var ws = new WorldState(qpf: 10_000_000, gameVersion: "test");

    // event counters prove ops announce their changes
    int added = 0, moved = 0, castStarted = 0, castFinished = 0, statusGain = 0, removed = 0;
    ActorCastEvent? seenCastEvent = null;
    ws.Actors.Added.Subscribe(_ => added++);
    ws.Actors.Moved.Subscribe(_ => moved++);
    ws.Actors.CastStarted.Subscribe(_ => castStarted++);
    ws.Actors.CastFinished.Subscribe((_, _) => castFinished++);
    ws.Actors.StatusGain.Subscribe((_, _) => statusGain++);
    ws.Actors.Removed.Subscribe(_ => removed++);
    ws.Actors.CastEvent.Subscribe((_, ev) => seenCastEvent = ev);

    var opLog = new List<string>();
    ws.Modified.Subscribe(op => opLog.Add(op.ToString()));

    const ulong boss = 0x400000001;
    const ulong player = 0x100000001;

    // frame + zone
    ws.Execute(new WorldState.OpFrameStart(Frame(ws, 0), TimeSpan.Zero));
    ws.Execute(new WorldState.OpZoneChange(zone: 1000, cfcID: 832));
    t.Eq("zone set", ws.CurrentZone, (ushort)1000);
    t.Eq("cfc set", ws.CurrentCFCID, (ushort)832);

    // spawn two actors
    ws.Execute(new ActorState.OpCreate(player, 0x2000, 0, "Hero", 0, ActorType.Player, new Vector4(100, 0, 100, 0), 0.5f, new ActorHPMP(50000, 50000, 0, 10000, 10000), true, true, 0));
    ws.Execute(new ActorState.OpCreate(boss, 0x4115, 1, "Valigarmanda", 12854, ActorType.Enemy, new Vector4(100, 0, 90, 0), 20f, new ActorHPMP(2_000_000, 2_000_000, 0, 0, 0), true, false, 0));
    t.Eq("two actors exist", ws.Actors.Actors.Count, 2);
    t.Eq("added fired twice", added, 2);
    t.NotNull("find boss", ws.Actors.Find(boss));
    t.Eq("boss oid", ws.Actors.Find(boss)!.OID, 0x4115u);

    // party slot 0 -> player, resolves to the live actor
    ws.Execute(new PartyState.OpModify(0, new PartyState.Member(0xC0FFEE, player)));
    t.NotNull("party slot 0 resolves", ws.Party.Actor(0));
    t.Eq("party member is hero", ws.Party.Actor(0)!.Name, "Hero");

    // move boss
    ws.Execute(new ActorState.OpMove(boss, new Vector4(100, 0, 100, MathF.PI)));
    var bossActor = ws.Actors.Find(boss)!;
    t.Near("boss moved to z=100", bossActor.Position.Z, 100f);
    t.Near("boss faces north (pi)", bossActor.Rotation.Rad, MathF.PI, 0.001f);
    t.Eq("moved fired once", moved, 1);

    // start a 5s cast
    var cast = new ActorCastInfo { Action = ActionID.MakeSpell(36158u), TargetID = player, TotalTime = 5f, ElapsedTime = 0f, Rotation = bossActor.Rotation };
    ws.Execute(new ActorState.OpCastInfo(boss, cast));
    t.NotNull("boss is casting", bossActor.CastInfo);
    t.Eq("castStarted fired", castStarted, 1);

    // advance three 1s frames -> cast should be at 3s elapsed
    for (var i = 1; i <= 3; i++)
        ws.Execute(new WorldState.OpFrameStart(Frame(ws, (uint)i, dtSeconds: 1f), TimeSpan.FromSeconds(1)));
    t.Near("cast elapsed ~3s after 3 frames", bossActor.CastInfo!.ElapsedTime, 3f, 0.001f);
    t.Near("cast remaining ~2s", bossActor.CastInfo!.RemainingTime, 2f, 0.001f);

    // cast resolves: fire snapshot event, then clear cast
    ws.Execute(new ActorState.OpCastEvent(boss, new ActorCastEvent(ActionID.MakeSpell(36158u), player, bossActor.Rotation, new Vector3(100, 0, 100), 42)));
    ws.Execute(new ActorState.OpCastInfo(boss, null));
    t.True("cast cleared", bossActor.CastInfo == null);
    t.Eq("castFinished fired", castFinished, 1);
    t.NotNull("cast event observed", seenCastEvent);
    t.True("cast event action matches", seenCastEvent!.Action.IsSpell(36158u));

    // apply a status to the player
    ws.Execute(new ActorState.OpStatus(player, 0, new ActorStatus(1871, 0, ws.FutureTime(15), boss)));
    t.NotNull("player has status", ws.Actors.Find(player)!.FindStatus(1871u));
    t.Eq("statusGain fired", statusGain, 1);

    // snapshot round-trip: rebuild a fresh WorldState from CompareToInitial ops
    var snapshot = ws.CompareToInitial();
    var ws2 = new WorldState(ws.QPF, ws.GameVersion);
    foreach (var op in snapshot)
        ws2.Execute(op);
    t.Eq("snapshot rebuilds actor count", ws2.Actors.Actors.Count, ws.Actors.Actors.Count);
    t.Eq("snapshot rebuilds zone", ws2.CurrentZone, ws.CurrentZone);
    t.NotNull("snapshot rebuilds party slot 0", ws2.Party.Actor(0));
    t.NotNull("snapshot rebuilds player status", ws2.Actors.Find(player)!.FindStatus(1871u));

    // kill + destroy the boss
    ws.Execute(new ActorState.OpDead(boss, true));
    t.True("boss dead", ws.Actors.Find(boss)!.IsDead);
    ws.Execute(new ActorState.OpDestroy(boss));
    t.True("boss removed from world", ws.Actors.Find(boss) == null);
    t.Eq("removed fired", removed, 1);
    t.Eq("one actor left", ws.Actors.Actors.Count, 1);

    // op log captured every mutation as serializable text
    t.True("op log non-empty", opLog.Count > 10);
    Console.WriteLine($"\n  (captured {opLog.Count} serialized ops; sample:)");
    foreach (var line in opLog.Take(6))
        Console.WriteLine($"    {line}");
}

// ---------------------------------------------------------------------------
// 3. Module framework: SimpleAOEs driven by cast start/finish events
// ---------------------------------------------------------------------------
t.Section("Module framework");
{
    var ws = new WorldState(10_000_000, "test");
    ws.Execute(new WorldState.OpFrameStart(Frame(ws, 0), TimeSpan.Zero));

    const ulong boss = 0x400000009;
    ws.Execute(new ActorState.OpCreate(boss, 0x1234, 0, "Dummy", 0, ActorType.Enemy, new Vector4(100, 0, 100, 0), 5f, new ActorHPMP(1000, 1000, 0, 0, 0), true, false, 0));

    var module = new TestModule(ws, ws.Actors.Find(boss)!) { Arena = new NullArena() };
    module.BuildStates(); // activates TestCircleAOE via TestModuleStates
    var comp = module.FindComponent<TestCircleAOE>();
    t.NotNull("component activated by states", comp);

    // no AOE before the cast
    t.Eq("no AOE initially", comp!.ActiveAOEs(0, ws.Actors.Find(boss)!).Length, 0);

    // boss begins the tracked cast -> one AOE, hit-testing a point inside its 5y circle
    var cast = new ActorCastInfo { Action = ActionID.MakeSpell(100u), TotalTime = 5f, Rotation = default, Location = new Vector3(100, 0, 100) };
    ws.Execute(new ActorState.OpCastInfo(boss, cast));
    var aoes = comp.ActiveAOEs(0, ws.Actors.Find(boss)!);
    t.Eq("one AOE after cast start", aoes.Length, 1);
    t.True("point inside AOE detected", aoes[0].Check(new WPos(102, 100)));
    t.True("point outside AOE clear", !aoes[0].Check(new WPos(110, 100)));

    // a player standing in it should get a risk hint
    var hints = new ModuleComponent.TextHints();
    var inside = new Actor(0x1, 1, 0, "P", 0, ActorType.Player, new Vector4(101, 0, 100, 0));
    module.AddHints(0, inside, hints);
    t.True("player inside gets a hint", hints.Count > 0 && hints[0].risk);

    // cast resolves -> AOE clears
    ws.Execute(new ActorState.OpCastInfo(boss, null));
    t.Eq("AOE cleared after cast finish", comp.ActiveAOEs(0, ws.Actors.Find(boss)!).Length, 0);

    module.Dispose();
}

// ---------------------------------------------------------------------------
// 4. Reflection registry: discovers [ModuleInfo] modules and instantiates them
// ---------------------------------------------------------------------------
t.Section("Module registry");
{
    var reg = ModuleRegistry.Build(typeof(RegisteredTestModule).Assembly);
    t.True("registry found at least one module", reg.Count >= 1);
    var forCFC = reg.ForCFC(999u);
    t.Eq("module indexed under its CFC id", forCFC.Count, 1);
    t.Eq("registry resolved primary OID", forCFC.Count == 1 ? forCFC[0].PrimaryActorOID : 0u, 0xABCDu);
    t.True("module explicitly marks primary death as completion", forCFC.Count == 1 && forCFC[0].Attr.PrimaryActorDeathEndsEncounter);

    var ws = new WorldState(10_000_000, "test");
    ws.Execute(new WorldState.OpFrameStart(Frame(ws, 0), TimeSpan.Zero));
    const ulong bossId = 0x400000011;
    ws.Execute(new ActorState.OpCreate(bossId, 0xABCD, 0, "RegBoss", 0, ActorType.Enemy, new Vector4(0, 0, 0, 0), 5f, default, true, false, 0));
    var built = forCFC[0].Create(ws, ws.Actors.Find(bossId)!);
    t.NotNull("registry can instantiate the module", built);
    t.NotNull("instantiated module built its component", built.FindComponent<TestCircleAOE>());
    built.Dispose();
}

// ---------------------------------------------------------------------------
// 5. Replay round-trip: record a fight to text, parse it back, compare worlds
// ---------------------------------------------------------------------------
t.Section("Replay round-trip");
{
    // build a small fight and record it
    var live = new WorldState(10_000_000, "test-ver");
    var sw = new System.IO.StringWriter();
    using (var rec = new ReplayRecorder(live, sw))
    {
        live.Execute(new WorldState.OpFrameStart(Frame(live, 0), TimeSpan.Zero));
        live.Execute(new WorldState.OpZoneChange(1234, 826));
        const ulong boss = 0x40000AAAA;
        const ulong player = 0x10000BBBB;
        live.Execute(new ActorState.OpCreate(player, 0x2000, 0, "Adventurer Bob", 0, ActorType.Player, new Vector4(35, 0, -90, 0), 0.5f, new ActorHPMP(9000, 9000, 0, 0, 0), true, true, 0));
        live.Execute(new ActorState.OpCreate(boss, 0x4190, 1, "Prime Punutiy", 12723, ActorType.Enemy, new Vector4(35, 0, -95, 0), 8f, new ActorHPMP(500000, 500000, 0, 0, 0), true, false, 0));
        // a self-targeted 5s cast that resolves
        for (var i = 1; i <= 3; i++)
            live.Execute(new WorldState.OpFrameStart(Frame(live, (uint)i, 1f), TimeSpan.FromSeconds(1)));
        var cast = new ActorCastInfo { Action = ActionID.MakeSpell(36492u), TargetID = boss, TotalTime = 5f, Location = new Vector3(35, 0, -95) };
        live.Execute(new ActorState.OpCastInfo(boss, cast));
        live.Execute(new ActorState.OpStatus(player, 0, new ActorStatus(1871, 0, live.FutureTime(15), boss)));
        live.Execute(new ActorState.OpMove(boss, new Vector4(40, 0, -95, 1.5f)));
        live.Execute(new ActorState.OpCastInfo(boss, null));
    }

    var log = sw.ToString();
    t.True("recording produced lines", log.Split('\n').Length > 8);

    // recorder player-filter: excluding other players keeps the boss but drops non-party players
    {
        var w = new WorldState(10_000_000, "test");
        var fsw = new System.IO.StringWriter();
        const ulong bossId = 0x40000CCCC, meId = 0x10000AAAA, randoId = 0x10000FFFF;
        using (var rec = new ReplayRecorder(w, fsw, excludeOtherPlayers: true, localPlayerId: meId))
        {
            w.Execute(new WorldState.OpFrameStart(Frame(w, 0), TimeSpan.Zero));
            w.Execute(new ActorState.OpCreate(bossId, 0x4190, 0, "Boss", 0, ActorType.Enemy, new Vector4(0, 0, 0, 0), 8f, default, true, false, 0));
            w.Execute(new ActorState.OpCreate(meId, 0x2000, 1, "Me", 0, ActorType.Player, new Vector4(1, 0, 1, 0), 0.5f, default, true, true, 0));
            w.Execute(new ActorState.OpCreate(randoId, 0x2000, 2, "Rando Player", 0, ActorType.Player, new Vector4(9, 0, 9, 0), 0.5f, default, true, true, 0));
            w.Execute(new ActorState.OpMove(randoId, new Vector4(8, 0, 8, 1f))); // noise from another player
            w.Execute(new ActorState.OpMove(bossId, new Vector4(0, 0, 2, 0)));
        }
        var flog = fsw.ToString();
        t.True("filter keeps the boss", flog.Contains(bossId.ToString("X")));
        t.True("filter keeps the local player", flog.Contains(meId.ToString("X")));
        t.True("filter drops other players (create + moves)", !flog.Contains(randoId.ToString("X")));
        t.True("filter dropped rando's name too", !flog.Contains("Rando Player"));
    }

    // parse it back and attach an analyzer during playback
    ReplayAnalysis? analysis = null;
    var replay = ReplayParser.Replay(new System.IO.StringReader(log), ws => analysis = ReplayAnalysis.Attach(ws));
    var rebuilt = replay.World;

    t.Eq("world version round-trips", rebuilt.GameVersion, "test-ver");
    t.Eq("zone round-trips", rebuilt.CurrentZone, (ushort)1234);
    t.Eq("cfc round-trips", rebuilt.CurrentCFCID, (ushort)826);
    t.Eq("actor count round-trips", rebuilt.Actors.Actors.Count, live.Actors.Actors.Count);

    var bossR = rebuilt.Actors.Find(0x40000AAAA);
    t.NotNull("boss reconstructed", bossR);
    t.Eq("boss name (with spaces) round-trips", bossR!.Name, "Prime Punutiy");
    t.Eq("boss OID round-trips", bossR.OID, 0x4190u);
    t.Near("boss moved position round-trips (x)", bossR.Position.X, 40f);
    t.Near("boss rotation round-trips", bossR.Rotation.Rad, 1.5f, 0.01f);
    t.NotNull("player status round-trips", rebuilt.Actors.Find(0x10000BBBB)!.FindStatus(1871u));

    // analyzer mined the cast + objects
    t.NotNull("analysis attached", analysis);
    t.True("analysis found the action", analysis!.ActionCount >= 1);
    t.True("analysis found objects", analysis.ObjectCount >= 1);

    var sheet = analysis.Render();
    t.True("fact sheet mentions the AID", sheet.Contains("36492"));
    t.True("fact sheet mentions CFC", sheet.Contains("cfc 826"));
    Console.WriteLine("\n--- fact sheet ---\n" + sheet);

    // interactive-playback timeline: parse the same log into stepped, frame-timestamped ops
    var timeline = ReplayParser.ParseTimeline(new System.IO.StringReader(log));
    t.True("timeline parsed ops", timeline.Ops.Count > 5);
    t.Eq("timeline version round-trips", timeline.GameVersion, "test-ver");
    t.True("timeline ticks are non-decreasing", IsSorted(timeline.Ops));
    t.True("timeline has a positive duration", timeline.DurationTicks > 0);
    t.True("snapshot ops share the fight start time (not 0)", timeline.StartTicks > 0);

    static bool IsSorted(System.Collections.Generic.IReadOnlyList<(long Ticks, WorldState.Operation Op)> ops)
    {
        for (var i = 1; i < ops.Count; ++i)
            if (ops[i].Ticks < ops[i - 1].Ticks)
                return false;
        return true;
    }
}

// ---------------------------------------------------------------------------
// 6. Module generation: facts + shape hints -> compilable module draft
// ---------------------------------------------------------------------------
t.Section("Extractor: correlation, names & phases");
{
    // Drive a synthetic replay through the analyzer so the correlation layer is exercised end-to-end.
    var ws = new WorldState(10_000_000, "test");
    var fi = 0u;
    void Adv(float dt) => ws.Execute(new WorldState.OpFrameStart(Frame(ws, fi++, dt), TimeSpan.FromSeconds(dt)));

    const ulong boss = 0x400000001, boss2 = 0x400000002, h1 = 0x400000010, h2 = 0x400000011;
    const ulong p1 = 0x100000001, p2 = 0x100000002, puddle = 0x400000050;
    var analysis = ReplayAnalysis.Attach(ws);

    Adv(0f); // establishes t0
    ws.Execute(new WorldState.OpZoneChange(1234, 826));
    ws.Execute(new ActorState.OpCreate(boss, 0x4190, 0, "Prime Punutiy", 0, ActorType.Enemy, new Vector4(35, 0, -95, 0), 8f, default, false, false, 0));
    ws.Execute(new ActorState.OpCreate(h1, 0x233C, 1, "", 0, ActorType.Helper, new Vector4(35, 0, -95, 0), 0.5f, default, true, false, 0));
    ws.Execute(new ActorState.OpCreate(h2, 0x233C, 2, "", 0, ActorType.Helper, new Vector4(35, 0, -95, 0), 0.5f, default, true, false, 0));
    ws.Execute(new ActorState.OpCreate(p1, 0x2000, 3, "P1", 0, ActorType.Player, new Vector4(40, 0, -90, 0), 0.5f, default, true, true, 0));
    ws.Execute(new ActorState.OpCreate(p2, 0x2000, 4, "P2", 0, ActorType.Player, new Vector4(30, 0, -90, 0), 0.5f, default, true, true, 0));
    ws.Execute(new ActorState.OpTargetable(boss, true)); // boss becomes targetable -> phase 1

    // helper for firing a self/location cast of a given aid
    void Cast(ulong caster, uint aid, ulong target, Vector3 loc = default)
    {
        ws.Execute(new ActorState.OpCastInfo(caster, new ActorCastInfo { Action = ActionID.MakeSpell(aid), TargetID = target, TotalTime = 5f, Location = loc }));
        ws.Execute(new ActorState.OpCastInfo(caster, null));
    }

    Cast(boss, 100u, boss);                          Adv(1f);   // big self circle (no location) -> raidwide
    Cast(boss, 101u, boss);                          Adv(1f);   // cone -> SimpleAOEs
    Cast(h1, 102u, h1, new Vector3(40, 0, -95));      Adv(1f);   // location circle -> SimpleAOEs
    Cast(boss, 103u, p1);                            Adv(1f);   // tankbuster (same target twice)
    Cast(boss, 103u, p1);                            Adv(1f);
    // simultaneous spread: two casters, same AID, same tick, different players
    ws.Execute(new ActorState.OpCastInfo(h1, new ActorCastInfo { Action = ActionID.MakeSpell(104u), TargetID = p1, TotalTime = 5f }));
    ws.Execute(new ActorState.OpCastInfo(h2, new ActorCastInfo { Action = ActionID.MakeSpell(104u), TargetID = p2, TotalTime = 5f }));
    ws.Execute(new ActorState.OpCastInfo(h1, null));
    ws.Execute(new ActorState.OpCastInfo(h2, null));
    Adv(1f);
    // tether-bait: tether appears, then a player-targeted cast within the window
    ws.Execute(new ActorState.OpTether(p1, new ActorTetherInfo(17, boss)));
    Cast(boss, 105u, p2);                            Adv(10f);  // (advance past the tether window)
    // icon spread: icon on a player, then a player-targeted cast
    ws.Execute(new ActorState.OpIcon(p1, 23u, p1));
    Cast(boss, 106u, p1);                            Adv(1f);
    // exaflare: the same location-targeted cast marches across the arena
    Cast(boss, 108u, boss, new Vector3(35, 0, -95));  Adv(1f);
    Cast(boss, 108u, boss, new Vector3(39, 0, -95));  Adv(1f);
    Cast(boss, 108u, boss, new Vector3(43, 0, -95));  Adv(1f);
    Cast(boss, 108u, boss, new Vector3(47, 0, -95));  Adv(1f);
    // line stack: a rectangle aimed at a player -> everyone lines up to share it
    Cast(boss, 109u, p1);                            Adv(1f);
    // voidzone: a lingering non-casting hazard object
    ws.Execute(new ActorState.OpCreate(puddle, 0x9999, 5, "Gas Cloud", 0, ActorType.Enemy, new Vector4(35, 0, -95, 0), 6f, default, false, false, 0));
    Adv(5f);
    ws.Execute(new ActorState.OpDestroy(puddle));
    // phase 2: a second boss form becomes targetable, then casts
    ws.Execute(new ActorState.OpCreate(boss2, 0x4179, 6, "Prime Punutiy", 0, ActorType.Enemy, new Vector4(35, 0, -95, 0), 8f, default, false, false, 0));
    ws.Execute(new ActorState.OpTargetable(boss2, true)); // -> phase 2
    Cast(boss2, 107u, boss2);                        Adv(1f);

    // stack: pile the party onto P1, then a player-targeted cast resolves with everyone clustered
    const ulong p3 = 0x100000003;
    ws.Execute(new ActorState.OpCreate(p3, 0x2000, 7, "P3", 0, ActorType.Player, new Vector4(80, 0, 80, 0), 0.5f, default, true, true, 0));
    ws.Execute(new ActorState.OpMove(p2, new Vector4(40, 0, -90, 0))); // onto P1 (P1 at 40,-90)
    ws.Execute(new ActorState.OpMove(p3, new Vector4(41, 0, -90, 0)));
    Cast(boss, 300u, p1);                            Adv(1f);
    ws.Execute(new ActorState.OpMove(p2, new Vector4(30, 0, -90, 0))); // disperse again
    ws.Execute(new ActorState.OpMove(p3, new Vector4(80, 0, 80, 0)));

    // gaze: self-cast with the party turned away from the boss (players south of boss, facing south)
    ws.Execute(new ActorState.OpMove(p1, new Vector4(35, 0, -80, 0)));
    ws.Execute(new ActorState.OpMove(p2, new Vector4(37, 0, -80, 0)));
    Cast(boss, 301u, boss);                          Adv(1f);

    // knockback: self-cast near the party, then everyone is shoved radially outward from the boss
    ws.Execute(new ActorState.OpMove(p1, new Vector4(35, 0, -90, MathF.PI))); // near boss, facing it (no gaze)
    ws.Execute(new ActorState.OpMove(p2, new Vector4(37, 0, -90, MathF.PI)));
    Cast(boss, 302u, boss);                          Adv(0.2f);
    ws.Execute(new ActorState.OpMove(p1, new Vector4(35, 0, -78, MathF.PI))); // shoved ~12y out
    ws.Execute(new ActorState.OpMove(p2, new Vector4(37, 0, -78, MathF.PI)));
    Adv(1.2f); // past the knockback settle delay

    // concentric: three different ground casts at the same spot in quick succession -> a bullseye
    Cast(boss, 310u, boss, new Vector3(35, 0, -95));  Adv(0.5f);
    Cast(boss, 311u, boss, new Vector3(35, 0, -95));  Adv(0.5f);
    Cast(boss, 312u, boss, new Vector3(35, 0, -95));  Adv(0.5f);

    // tether-telegraphed AOE: a tether, then a non-player self/location cast with a known shape -> TetherAOEs
    ws.Execute(new ActorState.OpTether(h1, new ActorTetherInfo(55, boss)));
    Cast(h1, 120u, h1, new Vector3(35, 0, -95));      Adv(1f);

    // a nearby player casting their own ability -> must be ignored (open-field noise)
    Cast(p1, 88888u, boss);                          Adv(1f);

    // an environment object spawning mid-fight -> arena-change scaffold in the draft
    ws.Execute(new ActorState.OpCreate(0x400000060, 0x1EBD5C, 8, "Deathwall", 0, ActorType.EventObj, new Vector4(35, 0, -95, 0), 0.5f, default, false, false, 0));
    Adv(1f);

    var input = analysis.BuildGenerationInput();
    t.Eq("boss identified from phase timeline", input.BossOID, 0x4190u);
    t.Eq("two phases detected", input.Phases.Count, 2);

    var gen = new Minerva.Generation.ModuleGenerator(new StubShapeResolver(), new StubNameResolver());
    var result = gen.Generate(input);
    var code = result.Code;

    // names from the (stub) resolver
    t.True("AID enum uses real action names", code.Contains("PunutiyPress = 100"));
    t.True("component class uses the action name", code.Contains("sealed class Hydrowave"));
    // correlation-based classification
    t.True("big self circle -> RaidwideCast", code.Contains("Components.RaidwideCast(module, (uint)AID.PunutiyPress)"));
    t.True("cone -> SimpleAOEs", code.Contains("Components.SimpleAOEs(module, (uint)AID.Hydrowave, new AOEShapeCone("));
    t.True("same-target repeat -> tankbuster", code.Contains("IceTalon: tankbuster"));
    t.True("simultaneous player casts -> SpreadFromCastTargets", code.Contains("Components.SpreadFromCastTargets(module, (uint)AID.CalamitousCry"));
    t.True("preceding tether -> BaitAwayTethers", code.Contains("Components.BaitAwayTethers(module, 17u)"));
    t.True("preceding icon -> SpreadFromIcon", code.Contains("Components.SpreadFromIcon(module, 23u"));
    // voidzone from a lingering hazard object
    t.True("lingering object -> Voidzone", code.Contains("Components.Voidzone(module,") && code.Contains("OID.GasCloud"));
    // marching location casts flagged as an exaflare (still drawn as SimpleAOEs, annotated for upgrade)
    t.True("marching location casts -> exaflare annotation", code.Contains("exaflare?") && code.Contains("(uint)AID.CeruleumVent"));
    // player-targeted rectangle -> line stack
    t.True("player-targeted rect -> LineStack", code.Contains("Components.LineStack(module, (uint)AID.WingBlade"));
    // party piled on the marked player -> stack (not spread)
    t.True("party clustered on target -> StackWithCastTargets", code.Contains("Components.StackWithCastTargets(module, (uint)AID.BallLightning"));
    // self-cast with the party facing away -> gaze
    t.True("players facing away -> Gaze", code.Contains("Components.Gaze(module, (uint)AID.Petrifaction)"));
    // party shoved radially outward -> knockback
    t.True("radial shove -> SimpleKnockbacks", code.Contains("Components.SimpleKnockbacks(module, (uint)AID.Shockwave"));
    // same-origin rapid ground casts -> concentric bullseye annotation
    t.True("same-origin bullseye -> concentric annotation", code.Contains("concentric?"));
    // other players' abilities are filtered out of the action list
    t.True("other players' casts are ignored", !code.Contains("88888"));
    // tether-preceded non-player cast -> a tether-driven AOE component
    t.True("tether-preceded cast -> TetherAOEs", code.Contains("Components.TetherAOEs(module, 55u, (uint)AID.ChainLightning"));
    // mid-fight environment object -> arena-change scaffold (commented, author fills the bounds)
    t.True("environment object -> ArenaChange scaffold", code.Contains("Components.ArenaChange") && code.Contains("arena may change"));
    // phases emitted as a real state machine with a transition condition
    t.True("state machine emits named phases", code.Contains("this.Phase(\"P1\")") && code.Contains("this.Phase(\"P2\")"));
    t.True("phase transition keys off the next boss becoming targetable", code.Contains(".TransitionOnTargetable((uint)OID.PrimePunutiy)"));
    t.True("has ModuleInfo with the CFC", code.Contains("[ModuleInfo(CFCID = 826u"));

    Console.WriteLine("\n--- fact sheet ---\n" + analysis.Render());
    Console.WriteLine("--- extraction report ---\n" + result.Report);

    // dump the generated module for the compile smoke-test (env-gated so normal runs don't write files)
    var dump = Environment.GetEnvironmentVariable("MINERVA_DUMP");
    if (!string.IsNullOrEmpty(dump))
        System.IO.File.WriteAllText(dump, code);
}

// ---------------------------------------------------------------------------
// 7. Auto-dodge: forbidden zones -> nearest safe spot
// ---------------------------------------------------------------------------
t.Section("Auto-dodge pathfinding");
{
    var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    var center = new WPos(100f, 100f);
    var hints = new AIHints { Center = center, Bounds = new ArenaBoundsSquare(20f) };

    // player standing in the open, no danger -> stay put
    hints.PlayerPosition = center;
    var stay = ArenaPathfinder.Solve(hints, now);
    t.True("no danger -> no need to move", !stay.NeedToMove);

    // a big circle AOE lands right on the player -> must dodge out of it
    hints.AddForbiddenZone(new AOEShapeCircle(8f), center, default, now.AddSeconds(2));
    hints.PlayerPosition = center;
    var dodge = ArenaPathfinder.Solve(hints, now);
    t.True("in AOE -> need to move", dodge.NeedToMove);
    t.True("found a safe spot", dodge.Found);
    t.True("safe spot is outside the AOE", !new AOEShapeCircle(8f).Check(dodge.Target, center, default));
    t.True("safe spot is inside arena bounds", hints.Bounds.Contains(center, dodge.Target));
    t.True("dodge direction points away from center", WDir.Dot(dodge.Direction, (dodge.Target - center).Normalized()) > 0.5f);

    // a far-future AOE shouldn't trigger movement yet (outside the 3s horizon)
    var later = new AIHints { Center = center, Bounds = new ArenaBoundsSquare(20f), PlayerPosition = center };
    later.AddForbiddenZone(new AOEShapeCircle(8f), center, default, now.AddSeconds(10));
    t.True("far-future AOE ignored within horizon", !ArenaPathfinder.Solve(later, now).NeedToMove);

    // whole arena covered -> honestly report no safe spot
    var trapped = new AIHints { Center = center, Bounds = new ArenaBoundsSquare(20f), PlayerPosition = center };
    trapped.AddForbiddenZone(new AOEShapeCircle(60f), center, default, now.AddSeconds(1));
    var noSpot = ArenaPathfinder.Solve(trapped, now);
    t.True("fully covered -> need to move", noSpot.NeedToMove);
    t.True("fully covered -> no safe spot found", !noSpot.Found);

    // component -> AIHints wiring: a SimpleAOEs feeds the dodge engine through the module
    var ws = new WorldState(10_000_000, "test");
    ws.Execute(new WorldState.OpFrameStart(Frame(ws, 0), TimeSpan.Zero));
    const ulong dummy = 0x400000099;
    ws.Execute(new ActorState.OpCreate(dummy, 0x1234, 0, "Dummy", 0, ActorType.Enemy, new Vector4(100, 0, 100, 0), 5f, default, true, false, 0));
    var module = new TestModule(ws, ws.Actors.Find(dummy)!) { Arena = new NullArena() };
    module.BuildStates();
    var cast = new ActorCastInfo { Action = ActionID.MakeSpell(100u), TotalTime = 5f, Location = new Vector3(100, 0, 100) };
    ws.Execute(new ActorState.OpCastInfo(dummy, cast));
    var player = new Actor(0x1, 1, 0, "P", 0, ActorType.Player, new Vector4(100, 0, 100, 0));
    var built = new AIHints();
    module.BuildAIHints(0, player, built);
    t.True("component fed a forbidden zone into AIHints", built.ForbiddenZones.Count == 1);
    // 5s cast: no dodge yet at the 3s horizon (correct), but dodge once it's imminent (6s horizon)
    t.True("module-driven: 5s cast not yet imminent at 3s horizon", !ArenaPathfinder.Solve(built, ws.CurrentTime).NeedToMove);
    t.True("module-driven: dodge triggers once imminent", ArenaPathfinder.Solve(built, ws.CurrentTime, horizonSeconds: 6f).NeedToMove);
    module.Dispose();
}

// ---------------------------------------------------------------------------
// 8. Mechanic component library (spread / stack / voidzone / gaze / knockback)
// ---------------------------------------------------------------------------
t.Section("Component library");
{
    var ws = new WorldState(10_000_000, "test");
    ws.Execute(new WorldState.OpFrameStart(Frame(ws, 0), TimeSpan.Zero));
    const ulong boss = 0x400000001, p1 = 0x100000001, p2 = 0x100000002, puddle = 0x400000050;
    ws.Execute(new ActorState.OpCreate(boss, 0x1234, 0, "Boss", 0, ActorType.Enemy, new Vector4(100, 0, 100, 0), 5f, default, true, false, 0));
    ws.Execute(new ActorState.OpCreate(p1, 0x2000, 0, "P1", 0, ActorType.Player, new Vector4(100, 0, 105, 0), 0.5f, default, true, true, 0));
    ws.Execute(new ActorState.OpCreate(p2, 0x2000, 0, "P2", 0, ActorType.Player, new Vector4(101, 0, 105, 0), 0.5f, default, true, true, 0));
    var module = new TestModule(ws, ws.Actors.Find(boss)!) { Arena = new NullArena() };
    var actorP1 = ws.Actors.Find(p1)!;
    var actorP2 = ws.Actors.Find(p2)!;

    // Spread: cast targets P1; P2 stands 1y away (< 6y radius) -> both warned
    var spread = new Minerva.Components.SpreadFromCastTargets(module, 500u, 6f);
    spread.OnCastStarted(ws.Actors.Find(boss)!, new ActorCastInfo { Action = ActionID.MakeSpell(500u), TargetID = p1, TotalTime = 5f });
    t.True("spread registered a target", spread.IsTarget(actorP1));
    var sh = new ModuleComponent.TextHints();
    spread.AddHints(0, actorP2, sh);
    t.True("spread warns a nearby non-target", sh.Count > 0);
    spread.OnCastFinished(ws.Actors.Find(boss)!, new ActorCastInfo { Action = ActionID.MakeSpell(500u), TargetID = p1 });
    t.True("spread cleared on finish", !spread.IsTarget(actorP1));

    // Stack: cast targets P1; P2 is far -> told to stack
    ws.Execute(new ActorState.OpMove(p2, new Vector4(115, 0, 105, 0))); // far from P1
    var stack = new Minerva.Components.StackWithCastTargets(module, 501u, 6f);
    stack.OnCastStarted(ws.Actors.Find(boss)!, new ActorCastInfo { Action = ActionID.MakeSpell(501u), TargetID = p1, TotalTime = 5f });
    var st = new ModuleComponent.TextHints();
    stack.AddHints(0, ws.Actors.Find(p2)!, st);
    t.True("stack tells a far player to stack", st.Count > 0 && st[0].text == "Stack!");

    // Voidzone: a puddle actor exists -> forbidden zone + hint when standing in it
    ws.Execute(new ActorState.OpCreate(puddle, 0x9999, 0, "Puddle", 0, ActorType.Enemy, new Vector4(100, 0, 100, 0), 1f, default, true, false, 0));
    var voidzone = new Minerva.Components.Voidzone(module, 6f, 0x9999u);
    var vh = new AIHints { Center = module.Center, Bounds = module.Bounds };
    voidzone.AddAIHints(0, actorP1, vh);
    t.Eq("voidzone contributes a forbidden zone", vh.ForbiddenZones.Count, 1);
    var vt = new ModuleComponent.TextHints();
    var inPuddle = new Actor(0x9, 9, 0, "X", 0, ActorType.Player, new Vector4(100, 0, 102, 0)); // 2y from puddle center
    voidzone.AddHints(0, inPuddle, vt);
    t.True("voidzone warns a player standing in it", vt.Count > 0);

    // Gaze: facing check
    var gaze = new Minerva.Components.Gaze(module, 502u);
    var facing = new Actor(0xA, 10, 0, "F", 0, ActorType.Player, new Vector4(100, 0, 110, 0)); // south of boss
    // rotation 0 = facing south (+Z); boss is north (-Z) -> facing AWAY from boss
    t.True("player facing away from source is safe", !Minerva.Components.Gaze.FacingToward(facing, new WPos(100, 100)));
    var facingBoss = new Actor(0xB, 11, 0, "F2", 0, ActorType.Player, new Vector4(100, 0, 110, MathF.PI)); // rotation pi = facing north
    t.True("player facing the source is flagged", Minerva.Components.Gaze.FacingToward(facingBoss, new WPos(100, 100)));

    // Knockback: predicted landing off the arena
    var kb = new Minerva.Components.SimpleKnockbacks(module, 503u, 15f);
    var nearEdge = new Actor(0xC, 12, 0, "E", 0, ActorType.Player, new Vector4(115, 0, 100, 0)); // 15y east of center, arena half=20
    var landing = kb.PredictLanding(nearEdge, new WPos(100, 100)); // pushed further east to x=130 -> off (half 20 => max 120)
    t.True("knockback predicts off-arena landing", !module.Bounds.Contains(module.Center, landing));

    // Concentric: inner circle then outer donut -> one dangerous ring at a time, centre-safe after advance
    var concentric = new Minerva.Components.ConcentricAOEs(module, [new AOEShapeCircle(5f), new AOEShapeDonut(5f, 10f)]);
    concentric.AddSequence(new WPos(100, 100));
    var ring1 = concentric.ActiveAOEs(0, actorP1);
    t.Eq("concentric shows one ring at a time", ring1.Length, 1);
    t.True("concentric inner ring hits the centre", ring1[0].Check(new WPos(100, 100)));
    concentric.AdvanceSequence(new WPos(100, 100));
    var ring2 = concentric.ActiveAOEs(0, actorP1);
    t.Eq("concentric advanced to the next ring", ring2.Length, 1);
    t.True("concentric outer donut leaves the centre safe", !ring2[0].Check(new WPos(100, 100)));
    concentric.AdvanceSequence(new WPos(100, 100));
    t.Eq("concentric clears after its last ring", concentric.ActiveAOEs(0, actorP1).Length, 0);

    // Exaflare: first cast spawns a marching line; a matching cast steps it forward
    ws.Execute(new ActorState.OpMove(boss, new Vector4(100, 0, 100, 0))); // face south (+Z)
    var exaBoss = ws.Actors.Find(boss)!;
    var exa = new Minerva.Components.SimpleExaflare(module, radius: 4f, aidFirst: 600u, aidRest: 601u, distance: 5f, timeToMove: 1d, explosionsLeft: 3, maxShown: 2);
    exa.OnCastStarted(exaBoss, new ActorCastInfo { Action = ActionID.MakeSpell(600u), TotalTime = 5f, Location = new Vector3(100, 0, 100) });
    t.True("exaflare spawned a marching line", exa.Active);
    var exaAoes = exa.ActiveAOEs(0, actorP1);
    t.True("exaflare draws the imminent explosion (plus preview)", exaAoes.Length >= 1);
    t.True("exaflare imminent explosion is risky", exaAoes[0].Risky);
    exa.OnCastFinished(exaBoss, new ActorCastInfo { Action = ActionID.MakeSpell(600u), Location = new Vector3(100, 0, 100) });
    t.True("exaflare still marching after one step", exa.Active);

    // LineStack: rectangle from boss through P1; off-line players must stack in, in-line players are clear
    var lsBoss = ws.Actors.Find(boss)!;
    var lineStack = new Minerva.Components.LineStack(module, 700u, halfWidth: 2f, length: 50f);
    lineStack.OnCastStarted(lsBoss, new ActorCastInfo { Action = ActionID.MakeSpell(700u), TargetID = p1, TotalTime = 5f });
    t.True("line stack registered a target", lineStack.Active);
    var lsFar = new ModuleComponent.TextHints();
    var offLine = new Actor(0xD, 13, 0, "O", 0, ActorType.Player, new Vector4(115, 0, 100, 0)); // 15y to the side of the +Z line
    lineStack.AddHints(0, offLine, lsFar);
    t.True("line stack warns a player off the line", lsFar.Count > 0 && lsFar[0].text == "Stack in line!");
    var lsOn = new ModuleComponent.TextHints();
    var onLine = new Actor(0xE, 14, 0, "N", 0, ActorType.Player, new Vector4(100, 0, 108, 0)); // on the line beyond P1
    lineStack.AddHints(0, onLine, lsOn);
    t.True("line stack clears a player standing in the line", lsOn.Count == 0);

    // ArenaChange: the bounds swap when a trigger actor spawns
    var shrinking = new TestModule(ws, ws.Actors.Find(boss)!) { Arena = new NullArena() };
    var arenaChange = new Minerva.Components.ArenaChange(shrinking, new ArenaBoundsCircle(10f), triggerOID: 0xDEAD);
    t.True("arena unchanged before the trigger", shrinking.Bounds.Radius > 15f);
    arenaChange.OnActorCreated(new Actor(0xF1, 0xDEAD, 0, "Wall", 0, ActorType.EventObj, new Vector4(0, 0, 0, 0)));
    t.True("arena change applied on trigger spawn", arenaChange.Applied);
    t.Near("arena bounds swapped to the new shape", shrinking.Bounds.Radius, 10f);
    shrinking.Dispose();

    // imminent-danger coloring: an AOE about to resolve is drawn brighter than a far-future one
    var now0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    var soon = new AOEInstance(new AOEShapeCircle(5f), new WPos(0, 0), default, now0.AddSeconds(1));
    var later = new AOEInstance(new AOEShapeCircle(5f), new WPos(0, 0), default, now0.AddSeconds(10));
    t.Eq("imminent AOE uses the bright colour", Minerva.Components.GenericAOEs.ColorFor(soon, now0), Colors.AOEImminent);
    t.Eq("far-future AOE uses the normal colour", Minerva.Components.GenericAOEs.ColorFor(later, now0), Colors.AOE);

    // ShapeHint -> real AOEShape (drives the no-compile draft preview)
    var coneShape = new Minerva.Generation.ShapeHint(Minerva.Generation.ShapeKind.Cone, Radius: 60f, HalfAngleDeg: 15f).ToShape();
    t.True("cone hint builds an AOEShapeCone", coneShape is AOEShapeCone { Radius: 60f });
    t.True("donut hint builds an AOEShapeDonut", new Minerva.Generation.ShapeHint(Minerva.Generation.ShapeKind.Donut, Radius: 40f, InnerRadius: 6f).ToShape() is AOEShapeDonut);
    t.True("unknown hint builds no shape", Minerva.Generation.ShapeHint.Unknown.ToShape() == null);

    module.Dispose();
}

// ---------------------------------------------------------------------------
// 9. Extractor: HP-gated phase boundary (boss disengages at an HP %, then returns)
// ---------------------------------------------------------------------------
t.Section("Extractor: HP-gated phase");
{
    var ws = new WorldState(10_000_000, "test");
    var fi = 0u;
    void Adv(float dt) => ws.Execute(new WorldState.OpFrameStart(Frame(ws, fi++, dt), TimeSpan.FromSeconds(dt)));
    const ulong boss = 0x400000301;
    var analysis = ReplayAnalysis.Attach(ws);

    Adv(0f);
    ws.Execute(new WorldState.OpZoneChange(1234, 826));
    ws.Execute(new ActorState.OpCreate(boss, 0x4190, 0, "Prime Punutiy", 0, ActorType.Enemy, new Vector4(35, 0, -95, 0), 8f, new ActorHPMP(1000, 1000, 0, 0, 0), false, false, 0));
    ws.Execute(new ActorState.OpTargetable(boss, true)); // phase 1

    void Cast(uint aid)
    {
        ws.Execute(new ActorState.OpCastInfo(boss, new ActorCastInfo { Action = ActionID.MakeSpell(aid), TargetID = boss, TotalTime = 5f }));
        ws.Execute(new ActorState.OpCastInfo(boss, null));
    }

    Cast(100u); Adv(1f);
    // boss drops to 50%, disengages (untargetable) for an intermission, then returns -> HP-gated phase
    ws.Execute(new ActorState.OpHPMP(boss, new ActorHPMP(500, 1000, 0, 0, 0)));
    ws.Execute(new ActorState.OpTargetable(boss, false)); Adv(2f);
    ws.Execute(new ActorState.OpTargetable(boss, true));  Adv(1f);
    Cast(101u); Adv(1f);

    var input = analysis.BuildGenerationInput();
    t.Eq("two phases detected via HP gate", input.Phases.Count, 2);
    t.Eq("phase 2 is HP-gated", input.Phases[1].Trigger, Minerva.Generation.PhaseTrigger.PrimaryHP);
    t.Near("HP gate captured at 50%", input.Phases[1].TriggerHP, 0.5f);

    var code = new Minerva.Generation.ModuleGenerator(new StubShapeResolver(), new StubNameResolver()).Generate(input).Code;
    t.True("HP gate -> TransitionOnPrimaryHP", code.Contains(".TransitionOnPrimaryHP(0.5f)"));
    t.True("still emits two named phases", code.Contains("this.Phase(\"P1\")") && code.Contains("this.Phase(\"P2\")"));
}

// ---------------------------------------------------------------------------
// 9b. Extractor: map-effect phase boundary (a one-shot arena change starts a phase)
// ---------------------------------------------------------------------------
t.Section("Extractor: map-effect phase");
{
    var ws = new WorldState(10_000_000, "test");
    var fi = 0u;
    void Adv(float dt) => ws.Execute(new WorldState.OpFrameStart(Frame(ws, fi++, dt), TimeSpan.FromSeconds(dt)));
    const ulong boss = 0x400000401;
    var analysis = ReplayAnalysis.Attach(ws);

    Adv(0f);
    ws.Execute(new WorldState.OpZoneChange(1234, 826));
    ws.Execute(new ActorState.OpCreate(boss, 0x4190, 0, "Prime Punutiy", 0, ActorType.Enemy, new Vector4(35, 0, -95, 0), 8f, new ActorHPMP(1000, 1000, 0, 0, 0), false, false, 0));
    ws.Execute(new ActorState.OpTargetable(boss, true)); // phase 1

    void Cast(uint aid)
    {
        ws.Execute(new ActorState.OpCastInfo(boss, new ActorCastInfo { Action = ActionID.MakeSpell(aid), TargetID = boss, TotalTime = 5f }));
        ws.Execute(new ActorState.OpCastInfo(boss, null));
    }

    Cast(100u); Adv(3f);
    // a recurring telegraph tile (index 9) toggles several times -> NOT a phase boundary
    ws.Execute(new WorldState.OpMapEffect(9, 1)); Adv(1f);
    ws.Execute(new WorldState.OpMapEffect(9, 0)); Adv(1f);
    ws.Execute(new WorldState.OpMapEffect(9, 1)); Adv(2f);
    // a one-shot arena change mid-fight (index 5, state 3) -> phase boundary
    ws.Execute(new WorldState.OpMapEffect(5, 3)); Adv(1f);
    Cast(101u); Adv(1f);

    var input = analysis.BuildGenerationInput();
    t.Eq("map effect adds a second phase", input.Phases.Count, 2);
    t.Eq("recurring tile did not add phases", input.Phases[1].Trigger, Minerva.Generation.PhaseTrigger.MapEffect);
    t.Eq("captured the map-effect index", input.Phases[1].TriggerMapIndex, (byte)5);
    t.Eq("captured the map-effect state", input.Phases[1].TriggerMapState, 3u);

    var code = new Minerva.Generation.ModuleGenerator(new StubShapeResolver(), new StubNameResolver()).Generate(input).Code;
    t.True("map-effect gate -> TransitionOnMapEffect", code.Contains(".TransitionOnMapEffect((byte)5, 3u)"));
}

// ---------------------------------------------------------------------------
// 9c. Extractor: boss = biggest casting enemy (not the spammy add) + tether-driven AOEs
// ---------------------------------------------------------------------------
t.Section("Extractor: boss detection + tether AOEs");
{
    var ws = new WorldState(10_000_000, "test");
    var fi = 0u;
    void Adv(float dt) => ws.Execute(new WorldState.OpFrameStart(Frame(ws, fi++, dt), TimeSpan.FromSeconds(dt)));
    const ulong bossBig = 0x400000701, addSmall = 0x400000702;
    var analysis = ReplayAnalysis.Attach(ws);

    Adv(0f);
    ws.Execute(new WorldState.OpZoneChange(1234, 1018));
    ws.Execute(new ActorState.OpCreate(bossBig, 0x46E1, 0, "Boss", 0, ActorType.Enemy, new Vector4(0, 0, 0, 0), 5f, default, true, false, 0)); // big hitbox
    ws.Execute(new ActorState.OpCreate(addSmall, 0x46E2, 1, "Add", 0, ActorType.Enemy, new Vector4(0, 0, 0, 0), 2f, default, true, false, 0)); // small hitbox

    void Cast(ulong c, uint aid)
    {
        ws.Execute(new ActorState.OpCastInfo(c, new ActorCastInfo { Action = ActionID.MakeSpell(aid), TargetID = c, TotalTime = 3f }));
        ws.Execute(new ActorState.OpCastInfo(c, null));
    }

    Cast(bossBig, 200u); Adv(1f);                     // the boss casts once
    for (var i = 0; i < 6; i++) { Cast(addSmall, 201u); Adv(0.5f); } // the add spams six casts

    t.Eq("boss = biggest casting enemy, not the spammy add", analysis.BuildGenerationInput().BossOID, 0x46E1u);

    // TetherAOEs component: a tether drops an AOE on the target; the matching cast clears it
    var mod = new TestModule(ws, ws.Actors.Find(bossBig)!) { Arena = new NullArena() };
    var tether = new Minerva.Components.TetherAOEs(mod, 303u, 500u, new AOEShapeCircle(16f));
    ws.Execute(new ActorState.OpTether(addSmall, new ActorTetherInfo(303, bossBig))); // add tethered to the boss (target)
    var addSmallActor = ws.Actors.Find(addSmall)!;
    tether.OnTethered(addSmallActor, in addSmallActor.Tether);
    t.Eq("tether drops an AOE on the target", tether.ActiveAOEs(0, ws.Actors.Find(addSmall)!).Length, 1);
    tether.OnCastFinished(ws.Actors.Find(bossBig)!, new ActorCastInfo { Action = ActionID.MakeSpell(500u) });
    t.Eq("the target's cast clears the tether AOE", tether.ActiveAOEs(0, ws.Actors.Find(addSmall)!).Length, 0);
    mod.Dispose();
}

// ---------------------------------------------------------------------------
// 9d. Replay validation: does the module cover the fight? (drawn / hinted / uncovered)
// ---------------------------------------------------------------------------
t.Section("Replay validation");
{
    var reg = ModuleRegistry.Build(typeof(RegisteredTestModule).Assembly);
    const ulong boss = 0x400000801;
    var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    var ops = new List<(long, WorldState.Operation)>();
    void Add(WorldState.Operation op) => ops.Add((t0.Ticks, op));

    const ulong helper = 0x400000802;
    Add(new WorldState.OpFrameStart(new FrameState(t0, 0UL, 0u, 0f, 0f, 1f), TimeSpan.Zero));
    Add(new WorldState.OpZoneChange(1234, 999)); // registered test module is CFC 999
    Add(new ActorState.OpCreate(boss, 0xABCD, 0, "Boss", 0, ActorType.Enemy, new Vector4(0, 0, 0, 0), 5f, default, true, false, 0));
    Add(new ActorState.OpCreate(helper, 0x233C, 1, "", 0, ActorType.Helper, new Vector4(0, 0, 0, 0), 0.5f, default, true, false, 0));
    Add(new ActorState.OpCastInfo(boss, new ActorCastInfo { Action = ActionID.MakeSpell(100u), TargetID = boss, TotalTime = 5f })); // TestCircleAOE watches 100 -> drawn
    Add(new ActorState.OpCastInfo(boss, null));
    Add(new ActorState.OpCastInfo(boss, new ActorCastInfo { Action = ActionID.MakeSpell(101u), TargetID = boss, TotalTime = 5f })); // boss self-cast, unhandled -> visual
    Add(new ActorState.OpCastInfo(boss, null));
    Add(new ActorState.OpCastInfo(helper, new ActorCastInfo { Action = ActionID.MakeSpell(102u), TargetID = helper, TotalTime = 3f })); // helper-cast, unhandled -> likely missed mechanic
    Add(new ActorState.OpCastInfo(helper, null));
    Add(new ActorState.OpCreate(0x1EBD5C, 0x1EBD5C, 2, "Wall", 0, ActorType.EventObj, new Vector4(0, 0, 0, 0), 0.5f, default, true, false, 0)); // arena marker

    var timeline = new ReplayTimeline { QPF = 10_000_000, GameVersion = "test", Ops = ops };
    var result = ReplayValidator.Validate(timeline, reg);

    t.Eq("validation activated the module", result.ModuleName, "RegisteredTestModule");
    t.Eq("validation counted the enemy actions", result.EnemyActions, 3);
    t.True("a cast a component draws -> drawn", result.Drawn.Contains(100u));
    t.True("boss self-cast, unhandled -> uncovered visual", result.UncoveredVisuals.Contains(101u));
    t.True("helper cast, unhandled -> uncovered mechanic (likely missed)", result.UncoveredMechanics.Contains(102u));
    t.True("environment object spawned -> arena-change warning", result.ArenaNote != null && result.ArenaNote.Contains("⚠"));
    Console.WriteLine("\n--- validation ---\n" + result.Render());
}

// ---------------------------------------------------------------------------
// 10. Recording lifecycle: only positive completion can stop a recording
// ---------------------------------------------------------------------------
t.Section("Recording auto-stop");
{
    var detector = new RecordingCompletionDetector(TimeSpan.FromSeconds(2));
    detector.Reset();

    t.True("waiting before the pull never stops", !detector.Update(TimeSpan.FromMinutes(10)));
    t.True("wipe or combat loss cannot stop without success", !detector.Update(TimeSpan.FromMinutes(10)));
    t.True("no completion is pending before a success signal", !detector.CompletionPending);

    detector.SignalCompletion();
    t.True("success signal starts the completion delay", detector.CompletionPending);
    t.True("first post-success second keeps recording", !detector.Update(TimeSpan.FromSeconds(1)));
    t.True("completion stops after the grace period", detector.Update(TimeSpan.FromSeconds(1)));
    t.True("completion fires only once", !detector.Update(TimeSpan.FromMinutes(10)));

    detector.SignalCompletion();
    detector.Reset();
    t.True("manual stop/reset clears a pending completion", !detector.CompletionPending && !detector.Update(TimeSpan.FromMinutes(10)));
}

// ---------------------------------------------------------------------------
// 11. Phase machine: transitions advance phases, swapping components on enter/exit
// ---------------------------------------------------------------------------
t.Section("Phase machine");
{
    var ws = new WorldState(10_000_000, "test");
    ws.Execute(new WorldState.OpFrameStart(Frame(ws, 0), TimeSpan.Zero));
    const ulong boss = 0x400000201;
    ws.Execute(new ActorState.OpCreate(boss, 0x1234, 0, "Boss", 0, ActorType.Enemy, new Vector4(0, 0, 0, 0), 5f, new ActorHPMP(1000, 1000, 0, 0, 0), true, false, 0));
    var module = new PhaseTestModule(ws, ws.Actors.Find(boss)!) { Arena = new NullArena() };
    module.BuildStates();

    // enters phase 0 on build: P1's component is on, P2's is not
    t.Eq("starts in phase 0", module.CurrentPhase, 0);
    t.NotNull("phase 0 component active on enter", module.FindComponent<PhaseAComp>());
    t.True("phase 1 component not yet active", module.FindComponent<PhaseBComp>() == null);

    // trigger absent -> Update holds the phase
    module.Update();
    t.Eq("no transition without the trigger", module.CurrentPhase, 0);

    // the phase-2 boss form appears (targetable) -> next Update advances and swaps components
    const ulong boss2 = 0x400000202;
    ws.Execute(new ActorState.OpCreate(boss2, 0xBEEF, 1, "Boss2", 0, ActorType.Enemy, new Vector4(0, 0, 0, 0), 8f, default, true, false, 0));
    module.Update();
    t.Eq("advanced to phase 1", module.CurrentPhase, 1);
    t.NotNull("phase 1 component active after transition", module.FindComponent<PhaseBComp>());
    t.True("phase 0 component deactivated on exit", module.FindComponent<PhaseAComp>() == null);

    // map-effect observation (what TransitionOnMapEffect polls): records seen (index,state) pairs
    ws.Execute(new WorldState.OpMapEffect(7, 2));
    t.True("module records a seen map effect", module.SawMapEffect(7, 2));
    t.True("an unseen map effect is not reported", !module.SawMapEffect(7, 9));

    module.Dispose();
}

return t.Report();

// Build a FrameState whose timestamp advances by dtSeconds from the world's current time.
static FrameState Frame(WorldState ws, uint index, float dtSeconds = 0f)
{
    var baseTime = ws.CurrentTime == default ? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) : ws.CurrentTime;
    var ts = baseTime.AddSeconds(dtSeconds);
    return new FrameState(ts, qpc: index * 10_000_000UL, index, dtSeconds, dtSeconds, 1f);
}

// --- test doubles for the module-framework section ---

// headless Arena: draw calls are no-ops (we only exercise geometry + hints here)
sealed class NullArena : Arena
{
    public override void ZoneShape(AOEShape shape, WPos origin, Angle rotation, uint color) { }
    public override void OutlineShape(AOEShape shape, WPos origin, Angle rotation, uint color, float thickness = 1f) { }
    public override void AddCircle(WPos center, float radius, uint color, float thickness = 1f) { }
    public override void AddCircleFilled(WPos center, float radius, uint color) { }
    public override void AddLine(WPos a, WPos b, uint color, float thickness = 1f) { }
    public override void ActorMarker(WPos pos, Angle rotation, float radius, uint color) { }
    public override void DrawBoundary() { }
}

sealed class TestCircleAOE(ModuleBase module) : Minerva.Components.SimpleAOEs(module, 100u, new AOEShapeCircle(5f));

sealed class TestModule(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(100f, 100f), new ArenaBoundsSquare(20f));

// paired states builder discovered by ModuleBase.BuildStates() via the "<Module>States" convention
sealed class TestModuleStates : StateMachineBuilder
{
    public TestModuleStates(ModuleBase module) : base(module)
        => this.TrivialPhase().ActivateOnEnter<TestCircleAOE>();
}

// a [ModuleInfo]-tagged module so the registry-discovery section has something to find
[ModuleInfo(CFCID = 999u, PrimaryActorOID = 0xABCDu, NameID = 1u, PrimaryActorDeathEndsEncounter = true)]
sealed class RegisteredTestModule(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(0f, 0f), new ArenaBoundsCircle(20f));

// --- test doubles for the phase-machine section: two phases, one component each ---
sealed class PhaseAComp(ModuleBase module) : Minerva.Components.SimpleAOEs(module, 900u, new AOEShapeCircle(3f));
sealed class PhaseBComp(ModuleBase module) : Minerva.Components.SimpleAOEs(module, 901u, new AOEShapeCircle(3f));

sealed class PhaseTestModule(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(0f, 0f), new ArenaBoundsSquare(20f));

sealed class PhaseTestModuleStates : StateMachineBuilder
{
    public PhaseTestModuleStates(ModuleBase module) : base(module)
    {
        this.Phase("P1")
            .ActivateOnEnter<PhaseAComp>()
            .TransitionOnTargetable(0xBEEFu);
        this.Phase("P2")
            .ActivateOnEnter<PhaseBComp>();
    }
}

sealed class RegisteredTestModuleStates : StateMachineBuilder
{
    public RegisteredTestModuleStates(ModuleBase module) : base(module)
        => this.TrivialPhase().ActivateOnEnter<TestCircleAOE>();
}

// stub resolvers for the extractor test
sealed class StubShapeResolver : Minerva.Generation.IShapeResolver
{
    public Minerva.Generation.ShapeHint Resolve(uint aid) => aid switch
    {
        100u => new(Minerva.Generation.ShapeKind.Circle, Radius: 40f),               // big self circle -> raidwide
        101u => new(Minerva.Generation.ShapeKind.Cone, Radius: 60f, HalfAngleDeg: 15f),
        102u => new(Minerva.Generation.ShapeKind.Circle, Radius: 6f),
        104u => new(Minerva.Generation.ShapeKind.Circle, Radius: 5f),                 // spread radius
        106u => new(Minerva.Generation.ShapeKind.Circle, Radius: 4f),                 // icon-spread radius
        107u => new(Minerva.Generation.ShapeKind.Circle, Radius: 10f),
        108u => new(Minerva.Generation.ShapeKind.Circle, Radius: 5f),                 // exaflare explosion
        109u => new(Minerva.Generation.ShapeKind.Rect, Radius: 50f, HalfWidth: 3f),   // line stack rectangle
        310u => new(Minerva.Generation.ShapeKind.Circle, Radius: 5f),                 // concentric bullseye rings
        311u => new(Minerva.Generation.ShapeKind.Circle, Radius: 8f),
        312u => new(Minerva.Generation.ShapeKind.Circle, Radius: 11f),
        120u => new(Minerva.Generation.ShapeKind.Circle, Radius: 6f),                 // tether-telegraphed AOE
        _ => Minerva.Generation.ShapeHint.Unknown,                                    // 103/105/300/301/302 -> by correlation
    };
}

sealed class StubNameResolver : Minerva.Generation.INameResolver
{
    public string? ActionName(uint aid) => aid switch
    {
        100u => "Punutiy Press",
        101u => "Hydrowave",
        102u => "Bury",
        103u => "Ice Talon",
        104u => "Calamitous Cry",
        105u => "Strangling Coil",
        106u => "Blighted Bolt",
        107u => "Tulidisaster",
        108u => "Ceruleum Vent",
        109u => "Wing Blade",
        300u => "Ball Lightning",
        301u => "Petrifaction",
        302u => "Shockwave",
        310u => "Stone Ring",
        311u => "Rock Blast",
        312u => "Boulder Rain",
        120u => "Chain Lightning",
        _ => null,
    };

    public string? ObjectName(uint oid) => null; // replay names used
}

// Minimal assertion harness: prints PASS/FAIL per check, returns exit code from Report().
sealed class Harness
{
    private int passed;
    private int failed;

    public void Section(string name) => Console.WriteLine($"\n== {name} ==");

    public void True(string label, bool cond) => Record(label, cond);
    public void Eq<T>(string label, T actual, T expected) => Record($"{label} (= {expected})", EqualityComparer<T>.Default.Equals(actual, expected), $"got {actual}");
    public void NotNull(string label, object? v) => Record(label, v != null, "was null");
    public void Near(string label, float actual, float expected, float eps = 0.01f) => Record($"{label} (~ {expected})", MathF.Abs(actual - expected) <= eps, $"got {actual}");

    private void Record(string label, bool ok, string? detail = null)
    {
        if (ok) { passed++; Console.WriteLine($"  [PASS] {label}"); }
        else { failed++; Console.WriteLine($"  [FAIL] {label}{(detail != null ? $" -- {detail}" : "")}"); }
    }

    public int Report()
    {
        Console.WriteLine($"\n{(failed == 0 ? "ALL PASSED" : "FAILURES")}: {passed} passed, {failed} failed.");
        return failed == 0 ? 0 : 1;
    }
}
