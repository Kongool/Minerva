using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Minerva;
using Minerva.Components;
using A = Minerva.PartyRolesConfig.Assignment;

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
    t.Eq("solid arena has no obstacles", new ArenaBoundsCircle(20f).Obstacles(c).Count, 0);

    // Custom bounds with cut-outs: a Treno-style field of boulders. Every obstacle must be drawable,
    // not just the first — the LoS mechanic is "hide behind one", so a partial draw is a wrong answer.
    var boulders = new ArenaBoundsCustom(
        [new Square(c, 19.5f)],
        [new Polygon(new(90f, 100f), 2f, 16), new Polygon(new(110f, 100f), 2.5f, 16), new Polygon(new(100f, 112f), 1.5f, 16)]);
    t.Eq("custom bounds expose every cut-out", boulders.Obstacles(c).Count, 3);
    // Radius drives the radar's zoom, so it must be the true circumradius, not the bounding-box diagonal:
    // a round field must not claim to be sqrt(2) bigger than it is, while a square's corner is genuinely farthest.
    t.Near("round custom bounds report their own radius", new ArenaBoundsCustom([new Polygon(c, 19.5f, 64)]).Radius, 19.5f, 0.1f);
    t.Near("square custom bounds reach to the corner", new ArenaBoundsCustom([new Square(c, 19.5f)]).Radius, 19.5f * MathF.Sqrt(2f), 0.1f);
    t.True("custom bounds exclude the third boulder", !boulders.Contains(c, new WPos(100f, 112f)));
    t.True("custom bounds contain open floor", boulders.Contains(c, new WPos(100f, 90f)));
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

    // quest solo duties: the attribute carries the quest id, the game says which duty that is
    var quests = ModuleRegistry.Build(q => q == 70001u ? 4242u : 0u, typeof(RegisteredTestModule).Assembly);
    t.Eq("quest module indexed under the resolved duty", quests.ForCFC(4242u).Count, 1);
    t.Eq("the quest id itself is no longer a key", quests.ForCFC(70001u).Count, 0);
    t.Eq("without a resolver the quest id stays the key (dormant, as before)", reg.ForCFC(70001u).Count, 1);
    var unresolved = ModuleRegistry.Build(q => 0u, typeof(RegisteredTestModule).Assembly);
    t.Eq("a quest the game does not know stays on its quest id", unresolved.ForCFC(70001u).Count, 1);
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
        live.Execute(new OpDodgeDecision(new DodgeDecision(true, true, new WPos(37f, -92f), DodgeReason.Danger, DodgeBlocker.None, true) { GazeUp = true, Turning = true, Mover = Mover.NavPath, MoverBusy = true }));
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
    // the dodge's decision rides in the same stream, so a hit can be explained from the log afterwards
    t.True("dodge decision round-trips", rebuilt.LastDodge is { Known: true, NeedToMove: true, Found: true, Reason: DodgeReason.Danger, Steering: true }
        && rebuilt.LastDodge.Target.AlmostEqual(new WPos(37f, -92f), 0.01f));
    t.True("the facing half of the decision round-trips", rebuilt.LastDodge is { GazeUp: true, Turning: true });
    t.True("the mover half of the decision round-trips", rebuilt.LastDodge is { Mover: Mover.NavPath, MoverBusy: true, MoverKnown: true });
    {
        // a decision line written before the mover fields existed still parses, with the mover unknown
        var old = new WorldState(10_000_000, "old");
        ReplayParser.Replay(new System.IO.StringReader("MINERVA-REPLAY 2 10000000 old 0\n0 DODG 7 -149.000 -864.000 1 0\n"), ws => old = ws);
        t.True("an older decision line parses without mover fields", old.LastDodge is { Known: true, NeedToMove: true, Steering: true, Mover: Mover.None, MoverBusy: false });
    }
    t.True("a steer with an idle mover names the mover", new DodgeDecision(true, true, default, DodgeReason.Danger, DodgeBlocker.None, true) { Known = true, Mover = Mover.NavPath, MoverKnown = true }.Explain(true, 3f).Contains("nothing driving"));
    t.True("a hit while walking for uptime is a zone the module never drew",
        new DodgeDecision(true, true, default, DodgeReason.Uptime, DodgeBlocker.None, true) { Known = true }.Explain(true, 4f).Contains("did not draw"));
    t.True("a gaze hold explains itself", new DodgeDecision(true, true, default, DodgeReason.Danger, DodgeBlocker.Gaze, false) { Known = true }.Explain(true, 3f).Contains("gaze"));
    t.True("a fresh world knows no decision", !new WorldState(1, "x").LastDodge.Known);
    t.True("a hit while stunned is not a dodge failure",
        new DodgeDecision(true, true, default, DodgeReason.Danger, DodgeBlocker.Incapacitated, false) { Known = true }.Explain(true, 5f).Contains("stunned"));
    t.True("a hit with nothing drawn is called a module gap",
        new DodgeDecision(false, false, default, DodgeReason.None, DodgeBlocker.None, false).Explain(false, 0f).Contains("module gap"));
    t.True("a hit while auto-move is off says so",
        new DodgeDecision(true, true, default, DodgeReason.Danger, DodgeBlocker.AutoDodgeOff, false) { Known = true }.Explain(true, 3f).Contains("auto-move is off"));
    t.True("a hit while steering blames the lead, not the module",
        new DodgeDecision(true, true, default, DodgeReason.Danger, DodgeBlocker.None, true) { Known = true }.Explain(true, 3f).Contains("clearance lead"));
    t.True("a hit the dodge judged safe names the margin",
        new DodgeDecision(false, false, default, DodgeReason.None, DodgeBlocker.None, false) { Known = true }.Explain(true, 0f).Contains("margin"));
    t.True("a drawn hit in a recording without decisions says so instead of guessing",
        default(DodgeDecision).Explain(true, 0f).Contains("predates"));
    // the hits report's own wordings for mechanics meant to land (Shantotto, 2026-09-06: a self-spread and a
    // proximity AOE were both reported as dodge failures)
    var safeDecision = new DodgeDecision(false, false, default, DodgeReason.None, DodgeBlocker.None, false) { Known = true };
    ReplayValidator.Hit hit(bool drawn, bool inside, int hitCount, bool spread = false, bool stack = false, bool mine = false, string owner = "", bool proximity = false)
        => new(1.0, 1u, "boss", drawn, safeDecision, 0f, false, 0u, 180f, false) { Inside = inside, TargetsHit = hitCount, Spread = spread, Stack = stack, Mine = mine, Owner = owner, Proximity = proximity };
    t.True("your own spread landing alone is the mechanic done right", hit(true, true, 1, spread: true, mine: true).Why.Contains("alone"));
    t.True("your own spread catching others says so", hit(true, true, 2, spread: true, mine: true).Why.Contains("1 other player"));
    t.True("someone else's spread names them", hit(true, true, 2, spread: true, owner: "Rosa Discord").Why.StartsWith("Rosa Discord's spread"));
    t.True("a shared stack is expected damage", hit(true, true, 3, stack: true).Why.Contains("shared by 3"));
    t.True("a stack taken alone says nobody gathered", hit(true, true, 1, stack: true).Why.Contains("alone"));
    t.True("outside the drawn core with many hit is a proximity AOE", hit(true, false, 16, proximity: true).Why.Contains("proximity"));
    t.True("outside everything drawn with few hit is a shape too small", hit(true, false, 2).Why.Contains("smaller than the real one"));
    t.True("inside the drawn zone still asks the decision", hit(true, true, 2).Why.Contains("margin"));

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
    // Which boss of this duty the module is for. Several bosses share one CFC -- every boss in Occult
    // Crescent North Horn reports 1093 -- so without this the registry falls back to reading OID.Boss by
    // name, and a draft can activate for the wrong fight or for none.
    t.True("the draft names its boss explicitly", code.Contains("PrimaryActorOID = 0x4190u"));

    // ...and two bosses from one duty have to be able to coexist: same file name, same namespace or same
    // class name and the second draft either overwrites the first or refuses to compile beside it
    t.True("the class is named after the boss, not the duty", code.Contains("class D826PrimePunutiy("));
    t.True("and so is the namespace", code.Contains("namespace Minerva.Generated.D826PrimePunutiy;"));
    t.True("as is the file it is written to", input.DraftFileName() == "D826PrimePunutiy.generated.cs");

    t.True("AID enum uses real action names", code.Contains("PunutiyPress = 100"));
    t.True("component class uses the action name", code.Contains("sealed class Hydrowave"));
    // correlation-based classification
    t.True("big self circle -> RaidwideCast", code.Contains("Components.RaidwideCast(module, (uint)AID.PunutiyPress)"));
    // A circle too big to escape is a raidwide wherever it is aimed. Requiring self-targeting let Arch
    // Kelpie's 60y location-targeted Water IV through as an ordinary AOE, which forbids the whole floor.
    t.True("a circle larger than the arena is never an avoidable AOE",
        !code.Contains("SimpleAOEs(module, (uint)AID.PunutiyPress"));
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

    // two waves two seconds apart that together cover the whole floor inside the horizon (Alexander's Divine
    // Arrow lines, 2026-09-06): dodge the first wave now rather than freeze on "no safe spot"
    var waves = new AIHints { Center = center, Bounds = new ArenaBoundsSquare(20f), PlayerPosition = center };
    waves.AddForbiddenZone(new AOEShapeCircle(30f), center - new WDir(15f, 0f), default, now.AddSeconds(2));
    waves.AddForbiddenZone(new AOEShapeCircle(30f), center + new WDir(15f, 0f), default, now.AddSeconds(4));
    var staged = ArenaPathfinder.Solve(waves, now, horizonSeconds: 5f);
    t.True("two waves covering everything: still moves", staged.NeedToMove && staged.Found);
    t.True("two waves covering everything: clears the first wave", staged.Found && (staged.Target - (center - new WDir(15f, 0f))).Length() > 30f);

    // the trash-mode arena estimate follows the party: a box learned in the last room is forgotten the
    // moment the player is no longer inside it (Eureka Orthos, 2026-09-06)
    var footprint = new ArenaFootprint();
    footprint.EnterZone(1);
    for (var i = 0; i < 30; ++i)
        footprint.Observe(new WPos(90f + (i % 3) * 10f, 90f + (i % 5) * 5f));
    t.True("a wide enough footprint is an arena when the player is inside it", footprint.TryEstimate(new WPos(100f, 100f), out var fpCenter, out _) && (fpCenter - new WPos(100f, 100f)).Length() < 1f);
    t.True("the same footprint is forgotten when the player has left it", !footprint.TryEstimate(new WPos(300f, 300f), out _, out _) && footprint.Samples == 0);

    // standing inside the margin band is not "settled": the margin the search used is the bar for holding too
    // (Orthos 2026-09-06: held at 8.1-8.7y from an 8y circle with a 1.5y margin, hit at 8.5)
    var band = new AIHints { Center = center, Bounds = new ArenaBoundsSquare(20f), PlayerPosition = center + new WDir(8.3f, 0f) };
    band.AddForbiddenZone(new AOEShapeCircle(8f), center, default, now.AddSeconds(2));
    var bandSpot = ArenaPathfinder.Solve(band, now, horizonSeconds: 5f, safetyMargin: 1.5f);
    t.True("inside the margin band the dodge still moves", bandSpot.NeedToMove && bandSpot.Found);
    t.True("and moves to a cell that clears the margin", (bandSpot.Target - center).Length() >= 9.5f - 0.01f);

    // ground somebody stood on is remembered by cell and forgotten with the zone
    var ground = new KnownGround();
    ground.Observe(new WPos(100.4f, 100.9f));
    t.True("a stood-on cell is known ground", ground.Contains(new WPos(101.5f, 101.5f)) && ground.Count == 1);
    t.True("the next cell over is not", !ground.Contains(new WPos(103f, 100f)));
    ground.Reset();
    t.True("a zone change forgets it", !ground.Contains(new WPos(101f, 101f)) && ground.Count == 0);

    // a guessed donut's hole errs small: too large paints lethal ground safe and killed a samurai on the
    // Orthochimera's Dragon's Voice (30y donut, real hole <= 9.9, old estimate 12), 2026-09-06
    t.True("a 30y donut's guessed hole stays under the real one", Minerva.Generation.CastTypeShapes.DonutInner(30f) <= 8f);
    t.True("a huge donut does not get a huge hole", Minerva.Generation.CastTypeShapes.DonutInner(40f) <= Minerva.Generation.CastTypeShapes.DonutInnerMax);
    t.True("a small donut's hole scales down", Minerva.Generation.CastTypeShapes.DonutInner(12f) <= 3f);

    // a gaze is answered by facing, and in unscripted content only the action's name says it is one
    t.True("a hex eye reads as a gaze", Minerva.Automation.AutoHints.LooksLikeGaze("Double Hex Eye"));
    t.True("so does a gaze and a glare", Minerva.Automation.AutoHints.LooksLikeGaze("Chilling Glare") && Minerva.Automation.AutoHints.LooksLikeGaze("Baleful Gaze"));
    t.True("an ordinary AOE does not", !Minerva.Automation.AutoHints.LooksLikeGaze("Knowing Gleam") && !Minerva.Automation.AutoHints.LooksLikeGaze("Body Slam"));
    t.True("nothing named is not a gaze", !Minerva.Automation.AutoHints.LooksLikeGaze(null));
    // the state, not the status name, so a report reads as a sentence
    {
        var stunned = new Actor(0xE, 14, 0, "S", 0, ActorType.Player, new Vector4(100, 0, 100, 0));
        t.True("a free actor is not incapacitated", Incapacitation.Blocking(stunned) == null);
        stunned.Statuses[0].ID = 149u;
        t.Eq("a stunned actor reports the state", Incapacitation.Blocking(stunned), "stunned");
    }

    // what the cast-bar guesser draws, one rule shared with the replay validator
    t.True("guesser draws a ground circle", Minerva.Automation.AutoHints.Draws(new Minerva.Generation.ShapeHint(Minerva.Generation.ShapeKind.Circle, Radius: 8f)));
    t.True("guesser draws a cone", Minerva.Automation.AutoHints.Draws(new Minerva.Generation.ShapeHint(Minerva.Generation.ShapeKind.Cone, Radius: 12f, HalfAngleDeg: 45f)));
    t.True("guesser skips a raidwide-sized circle", !Minerva.Automation.AutoHints.Draws(new Minerva.Generation.ShapeHint(Minerva.Generation.ShapeKind.Circle, Radius: 30f)));
    t.True("guesser skips single-target and unknown", !Minerva.Automation.AutoHints.Draws(new Minerva.Generation.ShapeHint(Minerva.Generation.ShapeKind.SingleTarget)) && !Minerva.Automation.AutoHints.Draws(Minerva.Generation.ShapeHint.Unknown));

    // a big circle AOE lands right on the player -> must dodge out of it
    hints.AddForbiddenZone(new AOEShapeCircle(8f), center, default, now.AddSeconds(2));
    hints.PlayerPosition = center;
    var dodge = ArenaPathfinder.Solve(hints, now);
    t.True("in AOE -> need to move", dodge.NeedToMove);
    t.True("found a safe spot", dodge.Found);
    t.True("safe spot is outside the AOE", !new AOEShapeCircle(8f).Check(dodge.Target, center, default));
    t.True("safe spot is inside arena bounds", hints.Bounds.Contains(center, dodge.Target));
    t.True("dodge direction points away from center", WDir.Dot(dodge.Direction, (dodge.Target - center).Normalized()) > 0.5f);

    // REGRESSION: the solve must keep moving until it is actually clear of the AOE, not stop the instant
    // the player's centre crosses the boundary. Reported in-game as "starts to avoid but doesn't fully
    // leave the AOE" — the early-out tested danger with no margin while target selection required one.
    var edge = new AIHints { Center = center, Bounds = new ArenaBoundsSquare(20f) };
    edge.AddForbiddenZone(new AOEShapeCircle(8f), center, default, now.AddSeconds(2));
    edge.PlayerPosition = center + new WDir(8.05f, 0f); // 0.05y outside the AOE edge
    var atEdge = ArenaPathfinder.Solve(edge, now, safetyMargin: 2.5f);
    t.True("just outside the AOE edge still counts as needing to move", atEdge.NeedToMove);
    t.True("edge case still finds a spot", atEdge.Found);
    t.True("that spot clears the AOE by the safety margin",
        (atEdge.Target - center).Length() >= 8f + 2.5f - 0.01f);

    // ...but once genuinely clear by the margin, hold position (no jitter)
    var clear = new AIHints { Center = center, Bounds = new ArenaBoundsSquare(20f) };
    clear.AddForbiddenZone(new AOEShapeCircle(8f), center, default, now.AddSeconds(2));
    clear.PlayerPosition = center + new WDir(12f, 0f); // 4y clear, beyond the 2.5y margin
    t.True("clear by more than the margin -> hold position",
        !ArenaPathfinder.Solve(clear, now, safetyMargin: 2.5f).NeedToMove);

    // REGRESSION GUARD for the MaxCastTime clamp in AIManager.Update.
    //
    // These two assertions are the whole reason that clamp exists, so keep them together: with a goal in
    // play and NOTHING dangerous anywhere, Regain still walks the character back to uptime, while the
    // geometric MaxCastTime truthfully reports "stand here as long as you like". Both are correct on their
    // own and together they mislead a rotation plugin, which reads MaxCastTime as the budget a hard cast
    // has to fit inside and then cannot start one because the character is being steered. AIManager
    // reconciles them by reporting 0 while it steers; if this pair ever stops holding, that clamp is
    // either unnecessary or has moved, and it should not survive unexplained.
    var regain = new AIHints { Center = center, Bounds = new ArenaBoundsSquare(20f) };
    regain.PlayerPosition = center + new WDir(18f, 0f);        // well outside melee reach of a boss at centre
    var uptime = new UptimeGoal(center, default, 2.6f);        // melee band, no positional required
    var walkBack = ArenaPathfinder.Solve(regain, now, goal: uptime);
    t.True("no danger at all, but out of uptime -> Regain still moves us", walkBack.NeedToMove);
    t.True("and it has somewhere to go", walkBack.Found);
    t.True("meanwhile the geometry says we may cast forever, because the ground IS safe",
        regain.MaxCastTime(now) == float.MaxValue);

    // ...which is also the escape hatch. AIManager.RequestHold lets Daedalus say "she is at that corpse on
    // purpose", and the clamp above stands down -- but only where the ground allows, and THIS is the
    // predicate that decides. Danger outranks a hold, because a raiser who dies mid-cast has raised nobody.
    // The branch itself lives in AIManager, which is Dalamud-bound and out of reach from here; what is
    // testable is that the question it asks separates the two cases, so pin that.
    t.True("nothing underfoot -> a hold may be honoured and the cast may start",
        !regain.InImminentDanger(regain.PlayerPosition, now.AddSeconds(5f), 1f));

    var holdDenied = new AIHints { Center = center, Bounds = new ArenaBoundsSquare(20f) };
    holdDenied.PlayerPosition = center + new WDir(18f, 0f);
    holdDenied.AddForbiddenZone(new AOEShapeCircle(6f), holdDenied.PlayerPosition, default, now.AddSeconds(3));
    t.True("an AOE landing on the corpse -> the same hold is overridden and we move",
        holdDenied.InImminentDanger(holdDenied.PlayerPosition, now.AddSeconds(5f), 1f));

    // The other half: once uptime is satisfied there is no walk, and the two answers agree again.
    var satisfied = new AIHints { Center = center, Bounds = new ArenaBoundsSquare(20f) };
    satisfied.PlayerPosition = center + new WDir(2f, 0f);      // inside the melee band
    t.True("inside the uptime band -> nothing to regain",
        !ArenaPathfinder.Solve(satisfied, now, goal: new UptimeGoal(center, default, 2.6f)).NeedToMove);

    // The safety margin is a configurable knob (Configuration.AutoDodgeSafetyMargin, default 1y).
    // At 0 the dodge stops the moment it leaves the shape — geometrically safe, but with no allowance
    // for hitbox radius or latency, which is what the UI warning is about.
    var zeroMargin = new AIHints { Center = center, Bounds = new ArenaBoundsSquare(20f) };
    zeroMargin.AddForbiddenZone(new AOEShapeCircle(8f), center, default, now.AddSeconds(2));
    zeroMargin.PlayerPosition = center + new WDir(8.05f, 0f); // barely outside the edge
    t.True("margin 0 -> stops as soon as the shape is cleared",
        !ArenaPathfinder.Solve(zeroMargin, now, safetyMargin: 0f).NeedToMove);
    t.True("margin 1 -> still moving at the same spot",
        ArenaPathfinder.Solve(zeroMargin, now, safetyMargin: 1f).NeedToMove);

    // a larger margin pushes the destination correspondingly further out
    zeroMargin.PlayerPosition = center;
    var m1 = ArenaPathfinder.Solve(zeroMargin, now, safetyMargin: 1f);
    var m4 = ArenaPathfinder.Solve(zeroMargin, now, safetyMargin: 4f);
    t.True("bigger margin -> destination further from the AOE",
        (m4.Target - center).Length() > (m1.Target - center).Length());

    // Constrained arena: a donut whose safe centre is smaller than the safety margin. The margin can
    // never be satisfied there, so the solve must settle on the safe spot rather than twitch forever.
    var donut = new AIHints { Center = center, Bounds = new ArenaBoundsSquare(20f) };
    donut.AddForbiddenZone(new AOEShapeDonut(3f, 30f), center, default, now.AddSeconds(2));
    donut.PlayerPosition = center; // dead centre of a 3y safe hole, margin 2.5y
    var inHole = ArenaPathfinder.Solve(donut, now, safetyMargin: 2.5f);
    t.True("safe hole smaller than the margin -> settle, don't twitch", !inHole.NeedToMove);

    // ...but from inside the donut ring, it must still run to that hole
    donut.PlayerPosition = center + new WDir(10f, 0f);
    var toHole = ArenaPathfinder.Solve(donut, now, safetyMargin: 2.5f);
    t.True("inside the donut ring -> move", toHole.NeedToMove);
    t.True("moves toward the safe hole", (toHole.Target - center).Length() < 3f);

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

    // TemporaryObstacles are avoided just like forbidden zones (they have no activation time)
    var withObstacle = new AIHints { Center = center, Bounds = new ArenaBoundsSquare(20f), PlayerPosition = center };
    withObstacle.AddForbiddenZone(new AOEShapeCircle(8f), center, default, now.AddSeconds(2));
    withObstacle.TemporaryObstacles.Add(new SDCircle(center + new WDir(12f, 0f), 6f)); // a wall to the east
    var obDodge = ArenaPathfinder.Solve(withObstacle, now);
    t.True("dodge avoids the standing obstacle", !withObstacle.InObstacle(obDodge.Target));
    t.True("dodge still escapes the AOE", !new AOEShapeCircle(8f).Check(obDodge.Target, center, default));

    // GoalZones gently bias the chosen safe spot: with two equidistant escape sides, prefer the goal side
    var withGoal = new AIHints { Center = center, Bounds = new ArenaBoundsSquare(20f), PlayerPosition = center };
    withGoal.AddForbiddenZone(new AOEShapeCircle(8f), center, default, now.AddSeconds(2));
    withGoal.GoalZones.Add(AIHints.GoalSingleTarget(center + new WDir(0f, 12f), 4f, 1f)); // goal to the south (+Z)
    var goalDodge = ArenaPathfinder.Solve(withGoal, now);
    t.True("goal bias pulls the dodge toward the goal side", goalDodge.Target.Z > center.Z);

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

    // Line of sight: a boss self-casting reports no target location, so the safe zone must still radiate
    // from the boss. Computed from (0,0) instead, the shadows arrive as bands across the arena and point the
    // player at cover that is not there.
    {
        const ulong rock = 0x400000060;
        ws.Execute(new ActorState.OpCreate(rock, 0x3000, 0, "Rock", 0, ActorType.Enemy, new Vector4(100, 0, 110, 0), 2f, default, true, false, 0));
        var los = new TestLineOfSight(module, 600u, [ws.Actors.Find(rock)!]);
        // ActiveCaster reads the actor's live CastInfo, so the cast has to exist on the world, not just be
        // handed to the component. A self-cast carries no target location — that is the whole point here.
        var losCast = new ActorCastInfo { Action = ActionID.MakeSpell(600u), TargetID = boss, TotalTime = 5f };
        ws.Execute(new ActorState.OpCastInfo(boss, losCast));
        los.OnCastStarted(ws.Actors.Find(boss)!, losCast);
        t.True("line of sight radiates from the caster", los.Origin == new WPos(100f, 100f));

        // directly behind the rock, on the far side from the boss, is the one safe spot
        var behind = new WPos(100f, 118f);
        var exposed = new WPos(112f, 100f);
        var zone = los.Safezones.Count != 0 ? los.Safezones[0] : default;
        t.True("a safe zone was produced", los.Safezones.Count == 1);
        t.True("behind the rock is safe", !zone.Shape.Check(behind, zone.Origin, zone.Rotation));
        t.True("open ground is not", zone.Shape.Check(exposed, zone.Origin, zone.Rotation));
    }

    // Spread: cast targets P1; P2 stands 1y away (< 6y radius) -> both warned
    var spread = new Minerva.Components.SpreadFromCastTargets(module, 500u, 6f);
    spread.OnCastStarted(ws.Actors.Find(boss)!, new ActorCastInfo { Action = ActionID.MakeSpell(500u), TargetID = p1, TotalTime = 5f });
    t.True("spread registered a target", spread.IsSpreadTarget(actorP1));
    var sh = new ModuleComponent.TextHints();
    spread.AddHints(0, actorP2, sh);
    t.True("spread warns a nearby non-target", sh.Count > 0);
    spread.OnCastFinished(ws.Actors.Find(boss)!, new ActorCastInfo { Action = ActionID.MakeSpell(500u), TargetID = p1 });
    t.True("spread cleared on finish", !spread.IsSpreadTarget(actorP1));

    // Stack: cast targets P1; P2 is far -> told to stack
    ws.Execute(new ActorState.OpMove(p2, new Vector4(115, 0, 105, 0))); // far from P1
    var stack = new Minerva.Components.StackWithCastTargets(module, 501u, 6f);
    stack.OnCastStarted(ws.Actors.Find(boss)!, new ActorCastInfo { Action = ActionID.MakeSpell(501u), TargetID = p1, TotalTime = 5f });
    var st = new ModuleComponent.TextHints();
    stack.AddHints(0, ws.Actors.Find(p2)!, st);
    t.True("stack tells a far player to stack", st.Count > 0 && st[0].text == "Stack!");

    // The AI side of it: a stack on P1 pulls P2 in (everything outside P1's circle is forbidden) and sends P1
    // toward a clean teammate. Alexander 2026-09-06: eight toons ignored every Mega Holy because this hint
    // did not exist -- the port only kept players out of stacks they were barred from.
    ws.Execute(new PartyState.OpModify(0, new PartyState.Member(0xA1, p1)));
    ws.Execute(new PartyState.OpModify(1, new PartyState.Member(0xA2, p2)));
    var stackHints = new AIHints { Center = module.Center, Bounds = module.Bounds, PlayerPosition = actorP2.Position };
    stack.AddAIHints(1, actorP2, PartyRolesConfig.Assignment.Unassigned, stackHints);
    t.True("a stack on someone else forbids the ground outside it", stackHints.ForbiddenZones.Count == 1 && stackHints.InImminentDanger(new WPos(115f, 105f), DateTime.MaxValue));
    t.True("inside the stack circle is allowed", !stackHints.InImminentDanger(new WPos(101f, 105f), DateTime.MaxValue));
    var carrierHints = new AIHints { Center = module.Center, Bounds = module.Bounds, PlayerPosition = actorP1.Position };
    stack.AddAIHints(0, actorP1, PartyRolesConfig.Assignment.Unassigned, carrierHints);
    t.True("the carrier is sent toward a clean teammate", carrierHints.ForbiddenZones.Count == 1 && !carrierHints.InImminentDanger(new WPos(114f, 105f), DateTime.MaxValue) && carrierHints.InImminentDanger(new WPos(100f, 105f), DateTime.MaxValue));

    // Voidzone: a puddle actor exists -> forbidden zone + hint when standing in it
    ws.Execute(new ActorState.OpCreate(puddle, 0x9999, 0, "Puddle", 0, ActorType.Enemy, new Vector4(100, 0, 100, 0), 1f, default, true, false, 0));
    var voidzone = new Minerva.Components.Voidzone(module, 6f, 0x9999u);
    var vh = new AIHints { Center = module.Center, Bounds = module.Bounds };
    voidzone.AddAIHints(0, actorP1, PartyRolesConfig.Assignment.Unassigned, vh);
    t.Eq("voidzone contributes a forbidden zone", vh.ForbiddenZones.Count, 1);
    var vt = new ModuleComponent.TextHints();
    var inPuddle = new Actor(0x9, 9, 0, "X", 0, ActorType.Player, new Vector4(100, 0, 102, 0)); // 2y from puddle center
    voidzone.AddHints(0, inPuddle, vt);
    t.True("voidzone warns a player standing in it", vt.Count > 0);

    // a walking hazard (a Voidzone with a movement hint) forbids the lane ahead of it, not just the ground under it
    var walker = new Actor(0xC, 12, 0, "Head", 0, ActorType.Enemy, new Vector4(100, 0, 100, 0)); // rotation 0 = facing +Z
    var walking = new Minerva.Components.Voidzone(module, 2f, _ => new[] { walker }, 6f);
    var wh = new AIHints { Center = module.Center, Bounds = module.Bounds };
    walking.AddAIHints(0, actorP1, PartyRolesConfig.Assignment.Unassigned, wh);
    t.True("a walking hazard forbids four reaches ahead", wh.ForbiddenZones.Count == 4);
    t.True("the lane ahead of it is forbidden", wh.InImminentDanger(new WPos(100f, 104f), DateTime.MaxValue));
    t.True("behind it is not", !wh.InImminentDanger(new WPos(100f, 95f), DateTime.MaxValue));

    // Gaze: facing check
    var gaze = new Minerva.Components.Gaze(module, 502u);
    var facing = new Actor(0xA, 10, 0, "F", 0, ActorType.Player, new Vector4(100, 0, 110, 0)); // south of boss
    // rotation 0 = facing south (+Z); boss is north (-Z) -> facing AWAY from boss
    t.True("player facing away from source is safe", !Minerva.Components.Gaze.FacingToward(facing, new WPos(100, 100)));
    var facingBoss = new Actor(0xB, 11, 0, "F2", 0, ActorType.Player, new Vector4(100, 0, 110, MathF.PI)); // rotation pi = facing north
    t.True("player facing the source is flagged", Minerva.Components.Gaze.FacingToward(facingBoss, new WPos(100, 100)));

    // The tolerance is the game's own +/-45 degrees, not a front hemisphere. Testing dot > 0 called a
    // source 80 degrees off "looking at it" -- so the text said look away while this component's own
    // forbidden arc, which has always been 45, considered that facing perfectly fine.
    static Actor Turned(float deg) => new(0xD, 13, 0, "T", 0, ActorType.Player, new Vector4(100, 0, 110, deg * MathF.PI / 180f));
    var source = new WPos(100, 100); // due north of the player, so facing north (180 deg) looks straight at it
    t.True("30 degrees off the source still counts as looking at it", Minerva.Components.Gaze.FacingToward(Turned(150f), source));
    t.True("and 30 the other way", Minerva.Components.Gaze.FacingToward(Turned(210f), source));
    t.True("60 degrees off does not", !Minerva.Components.Gaze.FacingToward(Turned(120f), source));
    t.True("nor 60 the other way", !Minerva.Components.Gaze.FacingToward(Turned(240f), source));
    t.True("and side-on is clear", !Minerva.Components.Gaze.FacingToward(Turned(90f), source));

    // Knockback: predicted landing off the arena (AwayFromSource pushes east past the wall)
    var nearEdge = new Actor(0xC, 12, 0, "E", 0, ActorType.Player, new Vector4(115, 0, 100, 0)); // 15y east of center, arena half=20
    var landing = Minerva.Components.GenericKnockback.AwayFromSource(nearEdge.Position, new WPos(100, 100), 15f); // pushed to x=130 -> off (max 120)
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

    // LineStack: rectangle from boss through P1; off-line players must stack in, in-line players are clear.
    // No marker AID/icon => the resolve cast is its own telegraph (the legacy cast-driven path).
    var lsBoss = ws.Actors.Find(boss)!;
    var lineStack = new Minerva.Components.LineStack(module, aidMarker: default, 700u, range: 50f, halfWidth: 2f);
    lineStack.OnCastStarted(lsBoss, new ActorCastInfo { Action = ActionID.MakeSpell(700u), TargetID = p1, TotalTime = 5f });
    t.Eq("line stack registered a target", lineStack.ActiveBaits.Count, 1);
    var lsFar = new ModuleComponent.TextHints();
    var offLine = new Actor(0xD, 13, 0, "O", 0, ActorType.Player, new Vector4(115, 0, 100, 0)); // 15y to the side of the +Z line
    lineStack.AddHints(0, offLine, lsFar);
    t.True("line stack warns a player off the line", lsFar.Count > 0 && lsFar[0].risk);
    var lsOn = new ModuleComponent.TextHints();
    var onLine = new Actor(0xE, 14, 0, "N", 0, ActorType.Player, new Vector4(100, 0, 108, 0)); // on the line beyond P1
    lineStack.AddHints(0, onLine, lsOn);
    t.True("line stack clears a player standing in the line", lsOn.Count > 0 && !lsOn[0].risk);
    lineStack.OnCastFinished(lsBoss, new ActorCastInfo { Action = ActionID.MakeSpell(700u), TargetID = p1 });
    t.Eq("line stack clears when the resolve cast lands", lineStack.ActiveBaits.Count, 0);
    t.Eq("line stack counted the resolve", lineStack.NumCasts, 1);

    // Marker-cast path: a telegraph AID marks the target, the resolve AID clears it
    var marked = new Minerva.Components.LineStack(module, aidMarker: 701u, 702u, activationDelay: 5d, range: 50f, halfWidth: 2f);
    marked.OnCastStarted(lsBoss, new ActorCastInfo { Action = ActionID.MakeSpell(702u), TargetID = p1, TotalTime = 5f });
    t.Eq("marker-driven line stack ignores the resolve cast start", marked.ActiveBaits.Count, 0);
    marked.OnEventCast(lsBoss, new ActorCastEvent(ActionID.MakeSpell(701u), p1, default, default, 0));
    t.Eq("marker cast registers the stack", marked.ActiveBaits.Count, 1);
    marked.OnEventCast(lsBoss, new ActorCastEvent(ActionID.MakeSpell(702u), p1, default, default, 0));
    t.Eq("resolve cast clears the marked stack", marked.ActiveBaits.Count, 0);

    // Icon path + markerIsFinalTarget: false — resolves are counted off the queue instead of matched by target
    var iconStack = new Minerva.Components.LineStack(module, iconID: 42u, 703u, 5d, 50f, 2f, markerIsFinalTarget: false);
    iconStack.OnEventIcon(lsBoss, 42u, p1);
    t.Eq("icon registers the stack", iconStack.ActiveBaits.Count, 1);
    iconStack.OnEventCast(lsBoss, new ActorCastEvent(ActionID.MakeSpell(703u), 0xDEAD, default, default, 0)); // unrelated target
    t.Eq("counted resolve clears regardless of target", iconStack.ActiveBaits.Count, 0);

    // StayMove -> SpecialModes: a stand-still punisher must be machine-readable, not just a text hint,
    // so the auto-dodge holds position and rotation plugins can stop casting (Minerva.MustNotAct IPC).
    var stayMod = new TestModule(ws, ws.Actors.Find(boss)!) { Arena = new NullArena() };
    var stay = new StayMoveProbe(stayMod);
    var stayHints = new AIHints();
    var nowStay = ws.CurrentTime;
    stay.AddAIHints(0, actorP1, PartyRolesConfig.Assignment.Unassigned, stayHints);
    t.True("no special mode without a requirement", !stayHints.MustNotAct(nowStay) && !stayHints.MustNotMove(nowStay));

    stayHints.SpecialModes.Clear();
    stay.Set(0, Minerva.Components.StayMove.Requirement.Stay, nowStay);
    stay.AddAIHints(0, actorP1, PartyRolesConfig.Assignment.Unassigned, stayHints);
    t.Eq("Stay maps to the Pyretic special mode", stayHints.ActiveSpecialMode(nowStay), AIHints.SpecialMode.Pyretic);
    t.True("Stay forbids acting", stayHints.MustNotAct(nowStay));
    t.True("Stay forbids moving", stayHints.MustNotMove(nowStay));

    stayHints.SpecialModes.Clear();
    stay.Set(0, Minerva.Components.StayMove.Requirement.Stay2, nowStay);
    stay.AddAIHints(0, actorP1, PartyRolesConfig.Assignment.Unassigned, stayHints);
    t.True("Stay2 forbids moving but allows acting", stayHints.MustNotMove(nowStay) && !stayHints.MustNotAct(nowStay));

    stayHints.SpecialModes.Clear();
    stay.Set(0, Minerva.Components.StayMove.Requirement.Stay, nowStay.AddSeconds(5d));
    stay.AddAIHints(0, actorP1, PartyRolesConfig.Assignment.Unassigned, stayHints);
    t.True("a mode that has not activated yet is not in force", !stayHints.MustNotAct(nowStay));
    stayMod.Dispose();

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
// 9c-bis. Components: CastCounter + SimpleAOEGroups (BMR-ported AOE family)
// ---------------------------------------------------------------------------
t.Section("Components: CastCounter + SimpleAOEGroups");
{
    var ws = new WorldState(10_000_000, "test");
    ws.Execute(new WorldState.OpFrameStart(Frame(ws, 0, 0f), TimeSpan.Zero));
    const ulong boss = 0x400000801, helper = 0x400000802;
    ws.Execute(new ActorState.OpCreate(boss, 0x1000, 0, "Boss", 0, ActorType.Enemy, new Vector4(0, 0, 0, 0), 5f, default, true, false, 0));
    ws.Execute(new ActorState.OpCreate(helper, 0x1001, 1, "Helper", 0, ActorType.Helper, new Vector4(10, 0, 10, 0), 0.5f, default, true, false, 0));
    var bossActor = ws.Actors.Find(boss)!;
    var helperActor = ws.Actors.Find(helper)!;
    var mod = new TestModule(ws, bossActor) { Arena = new NullArena() };

    ActorCastInfo MkCast(uint aid) => new() { Action = ActionID.MakeSpell(aid), TargetID = boss, TotalTime = 3f, Location = new Vector3(10, 0, 10) };

    // SimpleAOEGroups: two different action ids share one 6y circle; each caster is a distinct actor
    var grp = new Minerva.Components.SimpleAOEGroups(mod, [700u, 701u], 6f);
    grp.OnCastStarted(bossActor, MkCast(700u));
    grp.OnCastStarted(helperActor, MkCast(701u));
    t.Eq("group tracks both watched actions", grp.ActiveAOEs(0, bossActor).Length, 2);
    grp.OnCastStarted(bossActor, MkCast(999u)); // unwatched
    t.Eq("group ignores an unwatched action", grp.ActiveAOEs(0, bossActor).Length, 2);
    grp.OnCastFinished(bossActor, MkCast(700u));
    t.Eq("finishing one caster's cast clears one AOE", grp.ActiveAOEs(0, bossActor).Length, 1);

    // CastCounter only tallies resolved casts of its watched action
    var cc = new Minerva.Components.CastCounter(mod, 700u);
    cc.OnEventCast(bossActor, new ActorCastEvent(ActionID.MakeSpell(700u), boss, default, default, 0));
    cc.OnEventCast(bossActor, new ActorCastEvent(ActionID.MakeSpell(701u), boss, default, default, 0));
    t.Eq("cast counter counts only its watched action", cc.NumCasts, 1);

    // CastTowers: a cast spawns a soak tower; a forbidden soaker gets a forbidden zone + GTFO hint
    var towers = new Minerva.Components.CastTowers(mod, 800u, 4f);
    towers.OnCastStarted(helperActor, new ActorCastInfo { Action = ActionID.MakeSpell(800u), TargetID = helper, TotalTime = 5f, Location = new Vector3(10, 0, 10) });
    t.Eq("cast spawns a tower", towers.Towers.Count, 1);
    towers.Towers[0] = towers.Towers[0] with { ForbiddenSoakers = BitMask.Build(0) }; // player in slot 0 is forbidden
    var inTower = new Actor(0xD, 13, 0, "T", 0, ActorType.Player, new Vector4(10, 0, 10, 0)); // standing in the tower
    var th = new ModuleComponent.TextHints();
    towers.AddHints(0, inTower, th);
    t.True("forbidden soaker told to GTFO from tower", th.Exists(h => h.text.Contains("GTFO from tower")));
    var thints = new AIHints { Center = mod.Center, Bounds = mod.Bounds };
    towers.AddAIHints(0, inTower, PartyRolesConfig.Assignment.Unassigned, thints);
    t.Eq("forbidden tower becomes a forbidden zone", thints.ForbiddenZones.Count, 1);
    towers.OnCastFinished(helperActor, new ActorCastInfo { Action = ActionID.MakeSpell(800u) });
    t.Eq("finishing the cast clears the tower", towers.Towers.Count, 0);

    // BaitAwayCast: a cast marks its target as the baiter; a non-target standing in the AOE is warned
    var bait = new Minerva.Components.BaitAwayCast(mod, 810u, new AOEShapeCircle(5f), centerAtTarget: true);
    bait.OnCastStarted(bossActor, new ActorCastInfo { Action = ActionID.MakeSpell(810u), TargetID = helper, TotalTime = 5f });
    t.True("cast marks its target as a baiter", bait.IsBaitTarget(helperActor));
    var nearBaiter = new Actor(0xE, 14, 0, "N", 0, ActorType.Player, new Vector4(11, 0, 10, 0)); // 1y from the baiter (< 5y)
    var bh = new ModuleComponent.TextHints();
    bait.AddHints(0, nearBaiter, bh);
    t.True("a non-target clipped by the bait is warned", bh.Exists(h => h.text.Contains("GTFO from baited aoe")));
    bait.OnCastFinished(bossActor, new ActorCastInfo { Action = ActionID.MakeSpell(810u), TargetID = helper });
    t.Eq("finishing the cast clears the bait", bait.CurrentBaits.Count, 0);

    // RaidwideCastDelay: a visual cast arms a delayed raidwide cue; the AOE event disarms it
    var rw = new Minerva.Components.RaidwideCastDelay(mod, 820u, 821u, 3d, "Raidwide incoming");
    rw.OnCastStarted(bossActor, new ActorCastInfo { Action = ActionID.MakeSpell(820u), TargetID = boss, TotalTime = 2f });
    var gh = new ModuleComponent.GlobalHints();
    rw.AddGlobalHints(gh);
    t.True("armed raidwide shows a global hint", gh.Contains("Raidwide incoming"));
    rw.OnEventCast(bossActor, new ActorCastEvent(ActionID.MakeSpell(821u), boss, default, default, 0));
    var gh2 = new ModuleComponent.GlobalHints();
    rw.AddGlobalHints(gh2);
    t.True("resolved raidwide clears the hint", gh2.Count == 0);

    // GenericRotatingAOE: a rotating cone sequence shows the imminent cast and steps rotation on advance
    var rot = new Minerva.Components.GenericRotatingAOE(mod);
    rot.Sequences.Add(new Minerva.Components.GenericRotatingAOE.Sequence(new AOEShapeCone(20f, 45f.Degrees()), new WPos(0, 0), 0f.Degrees(), 90f.Degrees(), ws.CurrentTime, 2d, 3, 2));
    rot.Update();
    t.True("rotating sequence shows at least the imminent AOE", rot.ActiveAOEs(0, bossActor).Length >= 1);
    rot.AdvanceSequence(0, ws.CurrentTime);
    t.Eq("advancing the sequence steps rotation by the increment", rot.Sequences[0].Rotation.Rad, 90f.Degrees().Rad);

    // CastGaze: a cast spawns an eye; a player facing it is told to look away
    var gaze = new Minerva.Components.CastGaze(mod, 830u);
    gaze.OnCastStarted(bossActor, new ActorCastInfo { Action = ActionID.MakeSpell(830u), TargetID = boss, TotalTime = 4f, Location = new Vector3(0, 0, 10) });
    var facingEye = new Actor(0xF, 15, 0, "F", 0, ActorType.Player, new Vector4(0, 0, 0, 0)); // at origin, facing +Z (south=0) toward the eye at z=10
    var gzh = new ModuleComponent.TextHints();
    gaze.AddHints(0, facingEye, gzh);
    t.True("a player facing the gaze is warned", gzh.Exists(h => h.text.Contains("Turn away")));

    // CastSharedTankbuster: a cast marks a shared buster on its target
    var stb = new Minerva.Components.CastSharedTankbuster(mod, 840u, 6f);
    stb.OnCastStarted(bossActor, new ActorCastInfo { Action = ActionID.MakeSpell(840u), TargetID = helper, TotalTime = 5f });
    t.True("shared tankbuster is active after the cast", stb.Active);
    var stbh = new ModuleComponent.TextHints();
    stb.AddHints(0, helperActor, stbh);
    t.True("shared tankbuster warns its target to stack", stbh.Exists(h => h.text.Contains("Shared tankbuster")));
    stb.OnCastFinished(bossActor, new ActorCastInfo { Action = ActionID.MakeSpell(840u), TargetID = helper });
    t.True("shared tankbuster clears when the cast finishes", !stb.Active);

    // VoidzoneAtCastTarget: predicts an AOE at the cast target until the voidzone actor is present
    var vz = new Minerva.Components.VoidzoneAtCastTarget(mod, 6f, 850u, _ => System.Array.Empty<Actor>());
    vz.OnCastStarted(helperActor, new ActorCastInfo { Action = ActionID.MakeSpell(850u), TargetID = boss, TotalTime = 4f, Location = new Vector3(5, 0, 5) });
    t.Eq("voidzone predicts one AOE from the active cast", vz.ActiveAOEs(0, bossActor).Length, 1);
    vz.OnCastFinished(helperActor, new ActorCastInfo { Action = ActionID.MakeSpell(850u) });
    t.Eq("voidzone prediction clears when the cast finishes (no source actor yet)", vz.ActiveAOEs(0, bossActor).Length, 0);
    mod.Dispose();
}

// ---------------------------------------------------------------------------
// 9c-ter. Phase 3: custom mesh shapes + arenas (BMR-ported ArenaBoundsCustom / AOEShapeCustom)
// ---------------------------------------------------------------------------
t.Section("Extractor: cone angles from omen paths");
{
    // Real Omen.Path samples from the Action sheet. The filename carries the TOTAL angle; AOEShapeCone
    // wants the half-angle, so each of these must come back halved.
    t.Near("gl_fan120 is a 60-degree half-cone", Minerva.Generation.OmenPath.ConeHalfAngleDeg("gl_fan120_1bf") ?? -1f, 60f);
    t.Near("gl_fan060 is a 30-degree half-cone", Minerva.Generation.OmenPath.ConeHalfAngleDeg("gl_fan060_1bf") ?? -1f, 30f);
    t.Near("er_gl_fan090 resolves through the prefix", Minerva.Generation.OmenPath.ConeHalfAngleDeg("er_gl_fan090_1bf") ?? -1f, 45f);
    t.Near("m0070_fan180 is a half-room cleave", Minerva.Generation.OmenPath.ConeHalfAngleDeg("m0070_fan180_0h") ?? -1f, 90f);
    t.Near("gl_fan270 handles reflex angles", Minerva.Generation.OmenPath.ConeHalfAngleDeg("gl_fan270_1005af") ?? -1f, 135f);
    t.Near("a bare two-digit angle still resolves", Minerva.Generation.OmenPath.ConeHalfAngleDeg("x6r2_b3_fan54_01k1") ?? -1f, 27f);

    // fan01/fan02 are bespoke omen sequence numbers, not angles — a 0.5-degree cone would be nonsense
    t.True("fan01 is an index, not an angle", Minerva.Generation.OmenPath.ConeHalfAngleDeg("m0532om_fan01x") == null);
    t.True("a circle omen names no angle", Minerva.Generation.OmenPath.ConeHalfAngleDeg("general_1bf") == null);
    t.True("an empty path resolves to nothing", Minerva.Generation.OmenPath.ConeHalfAngleDeg("") == null);
    t.True("a null path resolves to nothing", Minerva.Generation.OmenPath.ConeHalfAngleDeg(null) == null);
}

t.Section("Module enemy tracking");
{
    // Modules capture these lists once, in a readonly field, following BMR's idiom. A snapshot therefore
    // freezes at activation: anything spawning later never appears. On Treno that left four of thirteen
    // boulders casting no line-of-sight shadow, so they looked like cover the radar refused to mark safe.
    var ws = new WorldState(10_000_000, "test");
    ws.Execute(new WorldState.OpFrameStart(Frame(ws, 0), TimeSpan.Zero));
    const ulong boss = 0x400000001, rock1 = 0x400000010, rock2 = 0x400000011;
    ws.Execute(new ActorState.OpCreate(boss, 0x1234, 0, "Boss", 0, ActorType.Enemy, new Vector4(0, 0, 0, 0), 5f, default, true, false, 0));
    ws.Execute(new ActorState.OpCreate(rock1, 0x5001, 0, "Rock", 0, ActorType.Enemy, new Vector4(10, 0, 0, 0), 2f, default, true, false, 0));
    var mod = new TestModule(ws, ws.Actors.Find(boss)!) { Arena = new NullArena() };

    var rocks = mod.Enemies([0x5001u, 0x5002u]); // captured now, as a module would
    t.Eq("captures what already exists", rocks.Count, 1);

    ws.Execute(new ActorState.OpCreate(rock2, 0x5002, 0, "Rock", 0, ActorType.Enemy, new Vector4(-10, 0, 0, 0), 2f, default, true, false, 0));
    t.Eq("a later spawn joins the captured list", rocks.Count, 2);

    ws.Execute(new ActorState.OpDestroy(rock1));
    t.Eq("a destroyed actor leaves it", rocks.Count, 1);

    t.True("the same OID set hands back the same list", ReferenceEquals(mod.Enemies([0x5002u, 0x5001u]), rocks));
    t.Eq("an unrelated OID tracks separately", mod.Enemies([0x9999u]).Count, 0);
}

t.Section("Encounter teardown");
{
    // A killed boss lingers as a corpse, so "destroyed" never fires promptly. Without a death check the
    // module keeps running over a finished fight and its components keep painting stale AOEs.
    var ws = new WorldState(10_000_000, "test");
    ws.Execute(new WorldState.OpFrameStart(Frame(ws, 0), TimeSpan.Zero));
    ws.Execute(new WorldState.OpZoneChange(1, 999));
    const ulong boss = 0x400000001;
    ws.Execute(new ActorState.OpCreate(boss, 0xABCDu, 0, "Boss", 0, ActorType.Enemy, new Vector4(0, 0, 0, 0), 5f, default, true, false, 0));

    var reg = ModuleRegistry.Build(typeof(ValidateProbeModule).Assembly);
    var info = reg.ForCFC(999u)[0];
    t.True("the probe module ends on primary death", info.Attr.PrimaryActorDeathEndsEncounter);

    var actor = ws.Actors.Find(boss)!;
    t.True("a live boss is not a finished encounter", !actor.IsDead && !actor.IsDestroyed);
    ws.Execute(new ActorState.OpDead(boss, true));
    t.True("a dead boss is still present, not destroyed", actor.IsDead && !actor.IsDestroyed);
}

t.Section("Gaze facing hints");
{
    // A gaze is the one mechanic where position does not matter and facing is everything, so the arc has to
    // be published or nothing but the player's own eyes can act on it.
    var ws = new WorldState(10_000_000, "test");
    ws.Execute(new WorldState.OpFrameStart(Frame(ws, 0), TimeSpan.Zero));
    const ulong boss = 0x400000001, p1 = 0x100000001;
    ws.Execute(new ActorState.OpCreate(boss, 0x1234, 0, "Boss", 0, ActorType.Enemy, new Vector4(0, 0, 10, 0), 5f, default, true, false, 0));
    ws.Execute(new ActorState.OpCreate(p1, 0x2000, 0, "P1", 0, ActorType.Player, new Vector4(0, 0, 0, 0), 0.5f, default, true, true, 0));
    var module = new TestModule(ws, ws.Actors.Find(boss)!) { Arena = new NullArena() };
    var pc = ws.Actors.Find(p1)!;

    var gaze = new TestGaze(module, new WPos(0f, 10f), inverted: false);
    var hints = new AIHints();
    gaze.AddAIHints(0, pc, PartyRolesConfig.Assignment.Unassigned, hints);
    t.Eq("a gaze publishes one forbidden arc", hints.ForbiddenDirections.Count, 1);
    // the eye is due south of the player (+Z), which is angle 0 in the game convention
    t.Near("the arc points at the eye", hints.ForbiddenDirections[0].center.Deg, 0f, 0.5f);
    t.Near("a normal gaze forbids 45 degrees either side", hints.ForbiddenDirections[0].halfWidth.Deg, 45f, 0.5f);

    // an inverted eye is the same statement turned inside out: only facing it is allowed
    var look = new TestGaze(module, new WPos(0f, 10f), inverted: true);
    var hints2 = new AIHints();
    look.AddAIHints(0, pc, PartyRolesConfig.Assignment.Unassigned, hints2);
    t.Near("an inverted gaze points away from the eye", MathF.Abs(hints2.ForbiddenDirections[0].center.Deg), 180f, 0.5f);
    t.Near("an inverted gaze forbids 135 degrees either side", hints2.ForbiddenDirections[0].halfWidth.Deg, 135f, 0.5f);

    // --- Eye to Eye (North Horn): the boss gazes from the centre while orbs gaze from around the arena,
    // with each cast overlapping the next by about a second. One eye is trivial; the overlap is the mechanic.
    var seer = new WPos(0f, 0f);                        // EvilSeer, centre
    var orbEast = new WPos(18f, 0f);
    var orbWest = new WPos(-18f, 0f);
    var orbNorth = new WPos(0f, -18f);
    var stand = new WPos(6f, 6f);                       // player, south-east of the boss
    var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // staggered 5s casts, each starting 4s after the last -> a 1s overlap between consecutive pairs
    var overlapping = new AIHints();
    void Eye(WPos at, double resolvesIn)
    {
        var toEye = Angle.FromDirection(at - stand);
        overlapping.ForbiddenDirections.Add((toEye, 45f.Degrees(), t0.AddSeconds(resolvesIn)));
    }

    Eye(seer, 5d);       // SeeNoEvil from the boss
    Eye(orbEast, 6d);    // SinisterSight, overlapping the tail of the first
    Eye(orbWest, 7d);
    Eye(orbNorth, 8d);

    t.Eq("four overlapping gazes publish four arcs", overlapping.ForbiddenDirections.Count, 4);

    // only the boss gaze has resolved by t+5: turning away from it is enough, and it is a real turn
    var facingBoss = Angle.FromDirection(seer - stand);
    t.True("with one gaze live there is a safe facing",
        overlapping.TryFindSafeFacing(t0.AddSeconds(5d), facingBoss, out var early));
    t.True("and it is not still pointing at the eye", MathF.Abs((early - facingBoss).Normalized().Deg) > 44f);

    // by t+8 all four are live, from the centre and three sides at once
    var allLive = overlapping.TryFindSafeFacing(t0.AddSeconds(8d), facingBoss, out var late);
    t.True("with all four live a facing still exists", allLive);
    foreach (var (center, halfWidth, _) in overlapping.ForbiddenDirections)
        t.True($"the chosen facing clears the arc at {center.Deg:f0}deg",
            MathF.Abs((late - center).Normalized().Rad) > halfWidth.Rad);

    // and the honest case: eyes boxing the player in from four quarters leave nothing legal
    var boxed = new AIHints();
    for (var deg = 0; deg < 360; deg += 90)
        boxed.ForbiddenDirections.Add((deg.Degrees(), 45f.Degrees(), t0));
    t.True("four 90-degree arcs cover the circle, so no facing is safe",
        !boxed.TryFindSafeFacing(t0, default, out _));

    // compass bearings are not the game's angles: 0 is north clockwise, the engine's 0 is south. Typing
    // "face 90" must point east whatever the engine calls that, so the conversion gets pinned down here.
    static Angle Compass(float deg) => new((180f - deg) * (MathF.PI / 180f));
    t.True("north points at -Z", Compass(0f).ToDirection().Z < -0.99f);
    t.True("east points at +X", Compass(90f).ToDirection().X > 0.99f);
    t.True("south points at +Z", Compass(180f).ToDirection().Z > 0.99f);
    t.True("west points at -X", Compass(270f).ToDirection().X < -0.99f);
}

t.Section("Analytic shape distance");
{
    // The annular-sector distance is hand-derived, so check it against the polygon walk it replaces. The
    // contour is a 60-segment approximation, so they agree to about a chord width, not exactly.
    var seg = new DonutSegment(new WPos(0f, 0f), 6f, 30f, (-20f).Degrees(), 20f.Degrees());
    var contour = seg.ContourWorld();
    var worst = 0f;
    for (var x = -35f; x <= 35f; x += 2.5f)
    {
        for (var z = -35f; z <= 35f; z += 2.5f)
        {
            var p = new WPos(x, z);
            var byPolygon = float.MaxValue;
            for (int i = 0, j = contour.Count - 1; i < contour.Count; j = i++)
            {
                var a = contour[j];
                var ab = contour[i] - a;
                var lenSq = ab.LengthSq();
                var tt = lenSq > 1e-6f ? Math.Clamp(WDir.Dot(p - a, ab) / lenSq, 0f, 1f) : 0f;
                byPolygon = MathF.Min(byPolygon, (p - (a + (ab * tt))).Length());
            }

            if (seg.Contains(p))
                byPolygon = -byPolygon;
            worst = MathF.Max(worst, MathF.Abs(seg.SignedDistance(p) - byPolygon));
        }
    }

    t.True($"sector distance matches its polygon (worst {worst:f2}y)", worst < 0.35f);
    t.True("inside the sector reads negative", seg.SignedDistance(new WPos(0f, 18f)) < 0f);
    t.True("outside the wedge reads positive", seg.SignedDistance(new WPos(18f, 0f)) > 0f);
    t.True("inside the hole reads positive", seg.SignedDistance(new WPos(0f, 2f)) > 0f);
    t.Near("a circle's distance is exact", new Circle(new WPos(0f, 0f), 5f).SignedDistance(new WPos(0f, 8f)), 3f);
}

t.Section("Auto-dodge uptime preference");
{
    // A cone from the boss covering the west. Both east cells are safe, but one hugs the boss and one is at
    // the far wall — the dodge should keep melee range rather than take the first safe cell it finds.
    var boss = new WPos(0f, 0f);
    var hints = new AIHints
    {
        Center = boss,
        Bounds = new ArenaBoundsCircle(20f),
        PlayerPosition = new WPos(-3f, 0f), // stood in the cone, west of the boss
    };
    var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    hints.AddForbiddenZone(new AOEShapeCone(25f, 80f.Degrees()), boss, (-90f).Degrees(), now.AddSeconds(2));

    var withUptime = ArenaPathfinder.Solve(hints, now, horizonSeconds: 5f, goal: new UptimeGoal(boss, default, 2.6f));
    var without = ArenaPathfinder.Solve(hints, now, horizonSeconds: 5f);
    t.True("uptime solve moves", withUptime.NeedToMove && withUptime.Found);
    t.True("without an uptime target it still solves", without.NeedToMove && without.Found);
    t.True("uptime keeps the dodge nearer the boss",
        (withUptime.Target - boss).Length() <= (without.Target - boss).Length() + 0.01f);
    t.True("the uptime spot is genuinely safe", !hints.InImminentDanger(withUptime.Target, now.AddSeconds(5)));

    // --- and once the danger passes, close the distance again. Solving only for safety leaves the character
    // parked wherever the last AOE pushed them; BMR gets the walk back for free because its goal zones are
    // never removed, only overridden.
    var clear = new AIHints
    {
        Center = boss,
        Bounds = new ArenaBoundsCircle(20f),
        PlayerPosition = new WPos(0f, 15f),   // 15y out, nothing incoming
    };
    var back = ArenaPathfinder.Solve(clear, now, horizonSeconds: 5f, goal: new UptimeGoal(boss, default, 5f));
    t.True("with nothing incoming it walks back toward the boss", back.NeedToMove && back.Found);
    t.True("and stops within reach", (back.Target - boss).Length() <= 5f);
    t.True("moving inward, not outward", (back.Target - boss).Length() < (clear.PlayerPosition - boss).Length());

    // already in range: stay put rather than jitter toward the exact centre
    var near = new AIHints { Center = boss, Bounds = new ArenaBoundsCircle(20f), PlayerPosition = new WPos(0f, 3f) };
    t.True("in range already means stay", !ArenaPathfinder.Solve(near, now, 5f, 1f, 0f, new UptimeGoal(boss, default, 5f)).NeedToMove);

    // reachability: a cell we cannot cross to before the cast lands is not a candidate
    var tight = new AIHints { Center = boss, Bounds = new ArenaBoundsCircle(30f), PlayerPosition = boss };
    tight.AddForbiddenZone(new AOEShapeCircle(6f), boss, default, now.AddSeconds(0.5d));   // half a second to clear 6y
    var rushed = ArenaPathfinder.Solve(tight, now, horizonSeconds: 5f, moveSpeed: 6f);
    t.True("under time pressure it still moves", rushed.NeedToMove);
    // 0.5s at 6 y/s is 3y of travel, so a 6y zone cannot be cleared -- it must fall back rather than freeze
    t.True("and falls back to the best it can do rather than standing still", rushed.Found);

    var roomy = new AIHints { Center = boss, Bounds = new ArenaBoundsCircle(30f), PlayerPosition = boss };
    roomy.AddForbiddenZone(new AOEShapeCircle(6f), boss, default, now.AddSeconds(4d));     // plenty of time
    var relaxed = ArenaPathfinder.Solve(roomy, now, horizonSeconds: 5f, moveSpeed: 6f);
    t.True("with time to spare it clears the zone properly", (relaxed.Target - boss).Length() > 6f);

    // positional: with a rear preference the walk back should end up behind the target, not in front
    var facingNorth = 180f.Degrees();      // boss faces -Z
    var flanked = new AIHints { Center = boss, Bounds = new ArenaBoundsCircle(20f), PlayerPosition = new WPos(0f, -14f) };
    var rear = ArenaPathfinder.Solve(flanked, now, 5f, 1f, 0f, new UptimeGoal(boss, facingNorth, 5f, Positional.Rear));
    t.True("a rear positional is respected on the way back",
        new UptimeGoal(boss, facingNorth, 5f, Positional.Rear).Satisfied(rear.Target));

    // the geometry itself, independent of the solver
    var goalRear = new UptimeGoal(boss, facingNorth, 5f, Positional.Rear);
    t.True("behind a north-facing target is south of it", goalRear.Satisfied(new WPos(0f, 3f)));
    t.True("in front of it is not", !goalRear.Satisfied(new WPos(0f, -3f)));
    var goalFlank = new UptimeGoal(boss, facingNorth, 5f, Positional.Flank);
    t.True("beside it counts as flank", goalFlank.Satisfied(new WPos(3f, 0f)));
    t.True("behind it does not count as flank", !goalFlank.Satisfied(new WPos(0f, 3f)));

    // the set: most melee accept rear OR flank and only care about not standing in front. Boxing has no
    // free hand for BMR's hold-to-allow-a-second-positional key, so both have to be expressible at once.
    var either = new UptimeGoal(boss, facingNorth, 5f, Positional.Rear | Positional.Flank);
    t.True("rear satisfies a rear-or-flank set", either.Satisfied(new WPos(0f, 3f)));
    t.True("flank satisfies it too", either.Satisfied(new WPos(3f, 0f)));
    t.True("front still does not", !either.Satisfied(new WPos(0f, -3f)));

    // The point of the margin: stand just inside the arc, not at its centre. BossmodReborn parks you dead
    // rear, which is 45 degrees of travel from the flank; Monk has one oGCD between its flank and rear hits
    // and cannot afford that trip, so the preferred stand is 15 degrees into the rear instead of 45.
    var switchable = new UptimeGoal(boss, facingNorth, 5f, Positional.Rear | Positional.Flank);
    // borders are measured off the TARGET's facing: a north-facing boss has its rear at +Z, so the
    // flank/rear line sits at absolute 45 degrees and dead-rear is straight south of it
    var onTheLine = boss + new WDir(2.12f, 2.12f);                     // rel 135deg -- exactly on the border
    var justInsideRear = boss + new WDir(1.5f, 2.6f);                  // rel 150deg -- the 15deg margin
    var deepRear = new WPos(0f, 3f);                                   // rel 180deg -- as deep as rear goes
    t.Near("the preferred stand is the margin inside the arc, not its centre", switchable.DegreesFromPreferredStand(justInsideRear) ?? -1f, 0f, 1.5f);
    t.Near("the border itself is one margin away", switchable.DegreesFromPreferredStand(onTheLine) ?? -1f, 15f, 1.5f);
    t.Near("dead rear is twice that -- what BMR would have picked", switchable.DegreesFromPreferredStand(deepRear) ?? -1f, 30f, 1.5f);

    // a single side still has a preferred stand: the margin inside its own border, so a later request for
    // the neighbouring arc is a 15 degree turn rather than a 45 degree one
    var rearOnly = new UptimeGoal(boss, facingNorth, 5f, Positional.Rear);
    t.Near("one side still prefers the near edge of its arc", rearOnly.DegreesFromPreferredStand(justInsideRear) ?? -1f, 0f, 1.5f);
    t.Near("and dead rear is the worse of the two", rearOnly.DegreesFromPreferredStand(deepRear) ?? -1f, 30f, 1.5f);

    // The switch itself, through the solver. Asked for the OTHER side from the rear anchor, the nearest cell
    // that merely satisfies the flank sits a step past the border -- and the boss's next turn puts you back
    // on the rear. The regain has to land past the floor (a third of the margin), inside the band the
    // margin bounds, on the same side of the boss it started: anywhere in that band is a positional the
    // game will credit, and the shallower the landing the shorter the switch back.
    // The floor is a walking distance, read back as degrees at this target's stand radius: 0.4s of
    // running. At five yalms that is more than the whole band, so the floor IS the margin -- deep is free
    // on a small mob. At eighteen yalms (a boss with a huge hitbox) the same walk is under eight degrees,
    // so the floor drops to keep the switch back inside a GCD -- and never below a third of the margin.
    t.Near("small target: the floor rises to the margin -- the whole band is a yalm and a half", rearOnly.MinimumDepthDeg, 15f, 0.01f);
    var bigTarget = new UptimeGoal(boss, facingNorth, 18f, Positional.Rear);
    t.Near("huge hitbox: the floor drops to what 0.4s of running buys", bigTarget.MinimumDepthDeg, 7.6f, 0.2f);
    t.Near("but never below a third of the margin", new UptimeGoal(boss, facingNorth, 60f, Positional.Rear).MinimumDepthDeg, 5f, 0.01f);
    var pastTheFloor = boss + new WDir(9.5f, 14.1f);                   // rel ~146deg, 11deg inside rear, 17y out
    t.True("past the floor counts as held", bigTarget.SatisfiedInside(pastTheFloor));
    t.True("the border itself does not", !bigTarget.SatisfiedInside(boss + new WDir(12f, 12f)));
    t.True("nor does a hair inside it", !bigTarget.SatisfiedInside(boss + new WDir(11.3f, 12.7f)));  // rel ~138deg
    t.True("on the small target, eleven degrees is not yet the floor", !rearOnly.SatisfiedInside(boss + new WDir(1.7f, 2.5f)));
    var flankOnly = new UptimeGoal(boss, facingNorth, 5f, Positional.Flank);
    var fromRear = new AIHints { Center = boss, Bounds = new ArenaBoundsCircle(20f), PlayerPosition = justInsideRear };
    var toFlank = ArenaPathfinder.Solve(fromRear, now, 5f, 1f, 0f, flankOnly);
    t.True("rear -> flank moves", toFlank.NeedToMove && toFlank.Found);
    t.True("and lands past the floor of the flank, not a step past the border", flankOnly.SatisfiedInside(toFlank.Target));
    t.True("but well short of the flank's centre -- the switch back stays short",
        MathF.Abs((Angle.FromDirection(toFlank.Target - boss) - facingNorth).Normalized().Deg) > 95f);
    t.True("on the same side of the boss it started, not round the far side", toFlank.Target.X > 0f);
    var justInsideFlank = boss + new WDir(2.6f, 1.5f);                   // rel 120deg -- the flank anchor
    var fromFlank = new AIHints { Center = boss, Bounds = new ArenaBoundsCircle(20f), PlayerPosition = justInsideFlank };
    var toRear = ArenaPathfinder.Solve(fromFlank, now, 5f, 1f, 0f, rearOnly);
    t.True("flank -> rear lands past the floor of the rear", toRear.NeedToMove && toRear.Found && rearOnly.SatisfiedInside(toRear.Target));
    t.True("and well short of dead rear",
        MathF.Abs((Angle.FromDirection(toRear.Target - boss) - facingNorth).Normalized().Deg) < 170f);
    t.True("on the same side too", toRear.Target.X > 0f);
    t.True("already inside the margin: a rear request leaves you alone",
        !ArenaPathfinder.Solve(new AIHints { Center = boss, Bounds = new ArenaBoundsCircle(20f), PlayerPosition = justInsideRear }, now, 5f, 1f, 0f, rearOnly).NeedToMove);

    // a zero margin is BMR's behaviour, kept reachable for jobs that would rather sit dead centre
    var centred = new UptimeGoal(boss, facingNorth, 5f, Positional.Rear, 0f);
    t.Near("a zero margin puts the ideal back on the border", centred.DegreesFromPreferredStand(onTheLine) ?? -1f, 0f, 1.5f);

    // no side asked for means nothing to prefer
    var noSide = new UptimeGoal(boss, facingNorth, 5f);
    t.True("no positional means no preferred stand", noSide.DegreesFromPreferredStand(deepRear) == null);

    var anySide = new UptimeGoal(boss, facingNorth, 5f, Positional.Any);
    t.True("an empty set accepts the front", anySide.Satisfied(new WPos(0f, -3f)));
    t.True("an empty set accepts the rear", anySide.Satisfied(new WPos(0f, 3f)));

    // and never walk into something to regain uptime
    var blocked = new AIHints { Center = boss, Bounds = new ArenaBoundsCircle(20f), PlayerPosition = new WPos(0f, 15f) };
    blocked.AddForbiddenZone(new AOEShapeCircle(9f), boss, default, now.AddSeconds(2));
    t.True("a dangerous way back means stay", !ArenaPathfinder.Solve(blocked, now, 5f, 1f, 0f, new UptimeGoal(boss, default, 5f)).NeedToMove);
}

t.Section("Packet coordinate decoding");
{
    // The wire format is unsigned 16-bit fixed point spanning +/-1000 yalms. Getting this wrong would place
    // every cast somewhere plausible-looking but wrong, which is far worse than placing it nowhere.
    static Vector3 Decode(ushort x, ushort y, ushort z)
        => new((x * (2000f / 65535f)) - 1000f, (y * (2000f / 65535f)) - 1000f, (z * (2000f / 65535f)) - 1000f);

    t.Near("zero decodes to the negative extreme", Decode(0, 0, 0).X, -1000f);
    t.Near("max decodes to the positive extreme", Decode(65535, 65535, 65535).X, 1000f);
    t.Near("midpoint decodes to the origin", Decode(32768, 32768, 32768).X, 0f, 0.05f);
    var treno = Decode(35520, 32768, 44892); // (84 + 1000) / (2000/65535) = 35520
    t.Near("a real arena coordinate round-trips", treno.X, 84f, 0.1f);
}

t.Section("An open-world FATE is bounded by the FATE");
{
    // A foray FATE has no sealed arena: you can walk out, and out means out of the encounter. So the
    // bound is not there to say what is possible, it is there to keep the dodge in the fight.
    // BossmodReborn does not bound its open-world modules at all, and its AI will occasionally dodge a
    // mechanic by leaving the FATE. The game states a centre and radius; there is no need to guess.
    var f = new FateState(1234u, new WPos(100f, 200f), 45f);
    t.True("a stated FATE is active", f.Active);
    t.True("its centre is inside it", f.Contains(f.Center));
    t.True("just inside the edge counts", f.Contains(new WPos(100f + 44f, 200f)));
    t.True("just outside does not", !f.Contains(new WPos(100f + 46f, 200f)));

    var none = default(FateState);
    t.True("no FATE reads as inactive", !none.Active);
    t.True("and contains nothing", !none.Contains(new WPos(0f, 0f)));

    // it has to survive a recording: a replay begun mid-FATE must open knowing the boundary, exactly as
    // it must know an arena change that already happened
    var ws = new WorldState(10_000_000, "test");
    ws.Execute(new WorldState.OpActiveFate(f));
    t.Eq("the world remembers the radius", ws.ActiveFate.Radius, 45f);

    var replayed = new WorldState(10_000_000, "test");
    foreach (var op in ws.CompareToInitial())
        replayed.Execute(op);
    t.Eq("and the opening snapshot carries it", replayed.ActiveFate.Radius, 45f);
    t.Eq("with the right id", replayed.ActiveFate.ID, 1234u);
}

t.Section("Dragged-in trash is not a voidzone");
{
    // A foray FATE is not sealed: players walk in and out and drag aggro with them, so trash ends up
    // standing inside the boss's cast radius for the whole fight. Every one of those passed the voidzone
    // test -- not the boss, never casts, plausible hitbox, present throughout, ActorType.Enemy -- and three
    // per draft were emitted as Components.Voidzone, telling the dodge to avoid a wandering mob.
    // A critical engagement seals its arena, so its actor list needs no such filter; the rule below is
    // what makes one classifier safe for both.
    var ws = new WorldState(10_000_000, "test");
    var analysis = ReplayAnalysis.Attach(ws);
    var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    void Tick(double s) => ws.Execute(new WorldState.OpFrameStart(new FrameState { Timestamp = t0.AddSeconds(s) }, TimeSpan.FromSeconds(1)));

    Tick(0);
    ws.Execute(new WorldState.OpZoneChange(1346, 1093));
    ws.Execute(new ActorState.OpCreate(0x1, 0x4B1Fu, 0, "Boss", 0u, ActorType.Enemy, new Vector4(0, 0, 0, 0), 5f, default, true, false, 0));
    ws.Execute(new ActorState.OpHPMP(0x1, new ActorHPMP(15_000_000, 15_000_000, 0, 0, 0)));

    // a real hazard: spawns, sits there, never casts, and has no health bar at all
    ws.Execute(new ActorState.OpCreate(0x2, 0x4D57u, 0, "Poison Cloud", 0u, ActorType.Enemy, new Vector4(5, 0, 5, 0), 1.7f, default, true, false, 0));
    // dragged-in trash: identical in every respect the old test looked at, except it has 800k HP
    ws.Execute(new ActorState.OpCreate(0x3, 0x4E13u, 0, "Crescent Opken", 0u, ActorType.Enemy, new Vector4(9, 0, 9, 0), 1.69f, default, true, false, 0));
    ws.Execute(new ActorState.OpHPMP(0x3, new ActorHPMP(779_562, 779_562, 0, 0, 0)));

    Tick(1);
    ws.Execute(new ActorState.OpCastInfo(0x1, new ActorCastInfo { Action = ActionID.MakeSpell(47383u), TargetID = 0x1, TotalTime = 4.7f, Location = new Vector3(0, 0, 0) }));
    Tick(6);
    ws.Execute(new ActorState.OpCastInfo(0x1, null));

    // both expire late in the fight: lifetime is part of the voidzone test, and an actor that is never
    // destroyed has none, so leaving them alive would make the fixture prove nothing
    Tick(25);
    ws.Execute(new ActorState.OpDestroy(0x2));
    ws.Execute(new ActorState.OpDestroy(0x3));
    Tick(30);

    var input = analysis.BuildGenerationInput();
    var cloud = input.Objects.FirstOrDefault(o => o.OID == 0x4D57u);
    var trash = input.Objects.FirstOrDefault(o => o.OID == 0x4E13u);
    t.True("both non-casters were seen", cloud != null && trash != null);
    t.True("a hazard with no health bar is a voidzone candidate", cloud!.VoidzoneCandidate);
    t.True("a mob with 800k HP is not", !trash!.VoidzoneCandidate);
}

t.Section("No two modules claim the same boss");
{
    // Silent when it happens and total when it matters: the registry takes whichever it reaches first, so
    // one module's mechanics are simply never drawn. Found for real when a batch port of BossmodReborn's
    // Occult Crescent set landed AllureOfTheOccult and ARottenAffair beside hand-written modules for the
    // same two bosses -- BMR names those after the FATE, Minerva had named them after the boss, and a
    // search by either name missed the other.
    var reg = ModuleRegistry.Build(typeof(ValidateProbeModule).Assembly);
    var clashes = reg.Collisions();
    if (clashes.Count != 0)
        foreach (var (cfc, oid, names) in clashes)
            Console.WriteLine($"    CFC {cfc} boss 0x{oid:X}: {string.Join(", ", names)}");
    t.Eq("the test assembly has no colliding modules", clashes.Count, 0);
}

t.Section("Which boss the module activates on");
{
    // Occult Crescent puts every boss in the zone on one CFC, and its FATEs run concurrently, so several
    // candidates can be standing around at once. Taking the first one found activated Metamorph -- idle
    // beside the party, 115M HP, zero casts all fight -- for a recording of a Machetaur pull.
    var reg = ModuleRegistry.Build(typeof(ValidateProbeModule).Assembly);
    var cands = reg.ForCFC(9001u);
    t.True("the probe duty has a candidate", cands.Count == 1);

    var oid = cands[0].PrimaryActorOID;
    static Actor Boss(ulong id, uint oid, float x, bool inCombat)
    {
        var a = new Actor(id, oid, -1, $"b{id}", 0u, ActorType.Enemy, new Vector4(x, 0f, 0f, 0f), 5f,
            new ActorHPMP { CurHP = 100, MaxHP = 100 }, true, false);
        a.InCombat = inCombat;
        return a;
    }

    var near = Boss(1, oid, 5f, inCombat: false);
    var far = Boss(2, oid, 40f, inCombat: false);
    var farEngaged = Boss(3, oid, 40f, inCombat: true);
    var me = new WPos(0f, 0f);

    // nothing to go on but distance
    t.True("the nearer boss wins when neither is fighting",
        ModuleRegistry.PickActivation(cands, [far, near], me)?.Boss.InstanceID == 1ul);

    // a boss that is fighting beats one that is merely there, even further away
    t.True("a fighting boss beats an idle nearer one",
        ModuleRegistry.PickActivation(cands, [near, farEngaged], me)?.Boss.InstanceID == 3ul);

    // ...but "in combat" cannot tell your fight from someone else's, so your own target outranks it
    t.True("the boss you have targeted wins over one merely in combat",
        ModuleRegistry.PickActivation(cands, [near, farEngaged], me, playerTarget: 1ul)?.Boss.InstanceID == 1ul);

    // activation before the pull still works -- requiring combat would delay the radar to first damage
    t.True("an idle boss still activates when it is the only one",
        ModuleRegistry.PickActivation(cands, [near], me) != null);

    // a corpse must not re-activate the module that just ended on it
    var dead = Boss(4, oid, 1f, inCombat: false);
    dead.IsDead = true;
    var pick = ModuleRegistry.PickActivation(cands, [dead, far], me);
    t.True("a dead boss is skipped when its death ends the encounter",
        !cands[0].Attr.PrimaryActorDeathEndsEncounter || pick?.Boss.InstanceID == 2ul);

    t.True("no candidate present means no activation", ModuleRegistry.PickActivation(cands, [], me) == null);
}

t.Section("Line-of-sight blockers as obstacles");
{
    // A probe standing in for a real module's component: the shadows are the safe ground, the blockers are
    // the objects casting them, and whether you may stand inside one is what decides whether the dodge is
    // allowed to walk through it.
    var origin = new WPos(0f, 0f);
    (WPos, float)[] rocks = [(new WPos(8f, 0f), 2f), (new WPos(-8f, 0f), 2f)];

    static AIHints Hint() => new() { Center = new WPos(0f, 0f), Bounds = new ArenaBoundsCircle(30f), PlayerPosition = new WPos(0f, 12f) };

    // default: standing inside a blocker is explicitly safe, so it must NOT become an obstacle -- fencing
    // the character out of ground the same component just called safe is worse than not knowing about it
    var standable = new LoSProbe(2001u, blockersImpassable: false, safeInsideHitbox: true);
    standable.Modify(origin, rocks);
    var h1 = Hint();
    standable.AddAIHints(0, null!, PartyRolesConfig.Assignment.Unassigned, h1);
    t.Eq("a standable blocker adds no obstacle", h1.TemporaryObstacles.Count, 0);

    // declared impassable: now the dodge has to go round
    var solid = new LoSProbe(2002u, blockersImpassable: true, safeInsideHitbox: true);
    solid.Modify(origin, rocks);
    var h2 = Hint();
    solid.AddAIHints(0, null!, PartyRolesConfig.Assignment.Unassigned, h2);
    t.Eq("an impassable blocker becomes one", h2.TemporaryObstacles.Count, 2);
    t.True("its centre is solid", h2.InObstacle(new WPos(8f, 0f)));
    t.True("and clear ground beside it is not", !h2.InObstacle(new WPos(8f, 6f)));

    // not-safe-inside is the same claim by another name
    var notSafe = new LoSProbe(2003u, blockersImpassable: false, safeInsideHitbox: false);
    notSafe.Modify(origin, rocks);
    var h3 = Hint();
    notSafe.AddAIHints(0, null!, PartyRolesConfig.Assignment.Unassigned, h3);
    t.Eq("a blocker you cannot stand in is also an obstacle", h3.TemporaryObstacles.Count, 2);

    // and the routing actually uses them
    var routed = ArenaPathfinder.Solve(h2, DateTime.Now, 5f, 1f, 0f);
    t.True("the solve respects them", !h2.InObstacle(routed.Steer));

    // The whole point, in Treno's shape: the safe ground is the shadow BEHIND a rock, so the rock is
    // between the player and where they must be. Going there means going around.
    var rock = new WPos(0f, 0f);
    var behind = new AIHints { Center = rock, Bounds = new ArenaBoundsCircle(30f), PlayerPosition = new WPos(0f, -10f) };
    behind.TemporaryObstacles.Add(new SDCircle(rock, 3f));
    // everything is lethal except a small pocket directly behind the rock
    behind.AddForbiddenZone(new SDInvertedCircle(new WPos(0f, 6f), 3f), DateTime.Now.AddSeconds(3));

    var hide = ArenaPathfinder.Solve(behind, DateTime.Now, 5f, 1f, 0f);
    t.True("it reaches the shadow", hide.Found && (hide.Target - new WPos(0f, 6f)).Length() <= 3f);
    t.True("without steering into the rock", !behind.InObstacle(hide.Steer));

    // a straight line from player to shadow passes through the rock, so the first move must be off-axis
    var straight = (hide.Target - behind.PlayerPosition).Normalized();
    var chosen = (hide.Steer - behind.PlayerPosition).Normalized();
    t.True("the straight line would have gone through it",
        !new RouteGrid(behind, DateTime.Now.AddSeconds(5), behind.PlayerPosition, 1f, 0f)
            .Walkable(behind.PlayerPosition, hide.Target));
    t.True("so the first move is aimed to one side of it", straight.Dot(chosen) < 0.95f);
}

t.Section("A recording opens with the world as it stands");
{
    // Thundergust Griffin shrinks its arena from 29.5 to 20 yalms when its first raidwide resolves, via a
    // map effect. Map effects are events: a recording started after that one fired never learns it
    // happened, so the replay runs the whole fight against a floor thirty percent larger than the real
    // one -- and nothing downstream can tell. Confirmed from recordings: seven Griffin logs carry both
    // 0x00020001 (shrink) and 0x00080004 (restore); the one begun mid-fight carries only the restore.
    var ws = new WorldState(10_000_000, "test");
    ws.Execute(new WorldState.OpZoneChange(1314, 1064));
    ws.Execute(new WorldState.OpMapEffect(0x17, 0x00020001u));
    ws.Execute(new WorldState.OpMapEffect(0x05, 0x00010001u));

    t.Eq("the state is retained, not just fired", ws.MapEffectStates.Count, 2);
    t.Eq("and it is the latest value", ws.MapEffectStates[0x17], 0x00020001u);

    // a later change to the same index replaces rather than accumulates
    ws.Execute(new WorldState.OpMapEffect(0x17, 0x00080004u));
    t.Eq("still two indices", ws.MapEffectStates.Count, 2);
    t.Eq("with the newer state", ws.MapEffectStates[0x17], 0x00080004u);

    // the snapshot a new recording opens with has to carry them
    var snapshot = ws.CompareToInitial();
    var effects = snapshot.OfType<WorldState.OpMapEffect>().ToList();
    t.Eq("the opening snapshot replays both effects", effects.Count, 2);
    t.True("including the arena's current shape",
        effects.Any(e => e.Index == 0x17 && e.State == 0x00080004u));

    // and replaying that snapshot into a fresh world reproduces the state
    var replayed = new WorldState(10_000_000, "test");
    foreach (var op in snapshot)
        replayed.Execute(op);
    t.Eq("a world rebuilt from the snapshot agrees", replayed.MapEffectStates[0x17], 0x00080004u);
}

t.Section("Routing around obstacles");
{
    var now = DateTime.Now;
    var c = new WPos(0f, 0f);

    // A wall between the player and the only safe ground. Straight-line scoring picks the safe cell and
    // steers at it -- through the wall. This is the Treno shape: hide behind a boulder, where the very
    // thing casting the safe shadow is the thing in the way.
    var walled = new AIHints { Center = c, Bounds = new ArenaBoundsSquare(20f), PlayerPosition = new WPos(0f, -12f) };
    walled.TemporaryObstacles.Add(new SDRect(c, new WDir(1f, 0f), 8f, 8f, 2.5f)); // a wall running east-west
    walled.AddForbiddenZone(new AOEShapeCircle(6f), new WPos(0f, -12f), default, now.AddSeconds(3));

    var routed = ArenaPathfinder.Solve(walled, now, 5f, 1f, 0f);
    t.True("it finds somewhere to go", routed.Found && routed.NeedToMove);
    t.True("the destination is not inside the wall", !walled.InObstacle(routed.Target));
    t.True("and neither is the point it steers at", !walled.InObstacle(routed.Steer));

    // the steering point is what makes it go round rather than into: it must be reachable in a straight
    // line from where the character actually is
    var grid = new RouteGrid(walled, now.AddSeconds(5), walled.PlayerPosition, 1f, 0f);
    t.True("the steering point is reachable without crossing anything solid",
        grid.Walkable(walled.PlayerPosition, routed.Steer));

    // a cell walled off from every approach is not a destination, however near it looks
    var boxed = new AIHints { Center = c, Bounds = new ArenaBoundsSquare(20f), PlayerPosition = new WPos(0f, -12f) };
    foreach (var (o, d) in new[]
    {
        (new WPos(0f, 4f), new WDir(1f, 0f)), (new WPos(0f, -4f), new WDir(1f, 0f)),
        (new WPos(4f, 0f), new WDir(0f, 1f)), (new WPos(-4f, 0f), new WDir(0f, 1f)),
    })
    {
        boxed.TemporaryObstacles.Add(new SDRect(o, d, 4.5f, 4.5f, 1f)); // four walls forming a sealed box
    }

    var sealedGrid = new RouteGrid(boxed, now.AddSeconds(5), boxed.PlayerPosition, 1f, 0f);
    t.True("the player's own cell is reachable", sealedGrid.TryCellOf(boxed.PlayerPosition, out var px, out var pz) && !float.IsInfinity(sealedGrid.CostAt(px, pz)));
    t.True("but the sealed interior is not", sealedGrid.TryCellOf(c, out var ix, out var iz) && float.IsInfinity(sealedGrid.CostAt(ix, iz)));

    // open ground must be unchanged: no route, no detour, steer straight at it
    var open = new AIHints { Center = c, Bounds = new ArenaBoundsSquare(20f), PlayerPosition = c };
    open.AddForbiddenZone(new AOEShapeCircle(6f), c, default, now.AddSeconds(3));
    var plain = ArenaPathfinder.Solve(open, now, 5f, 1f, 0f);
    t.True("on open ground the steer point is the destination", plain.Steer.AlmostEqual(plain.Target, 0.01f));

    // danger is crossable, just expensive -- otherwise a line-of-sight fight, where everything except a
    // few shadows is forbidden, would have no legal route to the next shadow at all
    var gauntlet = new AIHints { Center = c, Bounds = new ArenaBoundsSquare(20f), PlayerPosition = new WPos(0f, -14f) };
    gauntlet.AddForbiddenZone(new AOEShapeRect(4f, 20f), c, default, now.AddSeconds(4)); // a band across the middle
    var through = ArenaPathfinder.Solve(gauntlet, now, 6f, 1f, 0f);
    t.True("a band of danger does not make the far side unreachable", through.Found);
}

t.Section("A recording ends itself when the fight does");
{
    var s = TimeSpan.FromSeconds(1);
    // the shipped values: 2s grace after the fight is known over, 8s out of combat ends it by itself
    var d = new RecordingCompletionDetector(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(8));

    // out of combat before the pull must never end it -- a recording started while walking to the boss
    for (var i = 0; i < 60; ++i)
        d.NoteCombat(false, s);
    t.True("idling before the pull does not end anything", !d.Update(s));

    // pull
    for (var i = 0; i < 5; ++i)
        d.NoteCombat(true, s);
    t.True("nor does being in combat", !d.Update(s));

    // a wipe, a rez, a run back: combat drops but comes back before the timeout
    for (var i = 0; i < 5; ++i)
        d.NoteCombat(false, s);
    t.True("a five second lull is not the end of the fight", !d.Update(s));
    d.NoteCombat(true, s);
    t.True("and re-engaging clears it", !d.Update(s));

    // properly over: nothing to detect a death on, the player just walks away
    var ended = false;
    for (var i = 0; i < 25 && !ended; ++i)
    {
        d.NoteCombat(false, s);
        ended = d.Update(s);
    }

    t.True("sustained out of combat ends the recording", ended);
    t.True("and it ends exactly once", !d.Update(s));

    // signalling more than once is harmless: polling the boss's HP every frame means the signal repeats
    // for as long as it stays dead, and that must not keep pushing the grace period back
    var poll = new RecordingCompletionDetector(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(8));
    poll.NoteCombat(true, s);
    var stopped = false;
    for (var i = 0; i < 4 && !stopped; ++i)
    {
        poll.SignalCompletion();          // as a per-frame poll would
        stopped = poll.Update(TimeSpan.FromSeconds(1));
    }

    t.True("a repeated completion signal still stops within the grace period", stopped);

    // a real completion signal still wins immediately -- the idle rule never delays a known ending
    var d2 = new RecordingCompletionDetector(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(8));
    d2.NoteCombat(true, s);
    d2.SignalCompletion();
    t.True("the grace period still applies", !d2.Update(TimeSpan.FromSeconds(1)));
    t.True("then it stops", d2.Update(TimeSpan.FromSeconds(2)));

    // and a detector built without an idle timeout behaves exactly as before
    var legacy = new RecordingCompletionDetector(TimeSpan.FromSeconds(2));
    legacy.NoteCombat(true, s);
    for (var i = 0; i < 600; ++i)
        legacy.NoteCombat(false, s);
    t.True("no idle timeout means it records until told otherwise", !legacy.Update(s));
}

t.Section("Gaze compression: Eye to Eye at enrage");
{
    var now = DateTime.Now;

    // Real geometry, from minerva-20260819-200518-Evil-seer.log: four Accursed Orbs parked at the corners
    // of the arena, each casting SinisterSight from where it stands. The fight adds no mechanics as it
    // runs on -- it shortens the gaps between the same casts until they overlap -- so "how many at once"
    // is the entire difficulty curve, and four is where it ends up.
    var orbs = new[] { new WPos(495.9f, -15.9f), new WPos(509.9f, -50f), new WPos(509.9f, -10f), new WPos(495.9f, -44.1f) };
    var stood = new WPos(511.7f, -31f); // where the player actually was, from the same recording

    static AIHints Looking(WPos player, IEnumerable<WPos> eyes, DateTime at)
    {
        var h = new AIHints { Center = player, Bounds = new ArenaBoundsCircle(30f), PlayerPosition = player };
        foreach (var e in eyes)
            h.ForbiddenDirections.Add((Angle.FromDirection(e - player), 45f.Degrees(), at)); // don't look at it
        return h;
    }

    var opening = Looking(stood, orbs.Take(2), now);
    opening.TryFindBestFacing(now.AddSeconds(1), 0f.Degrees(), out _, out var openHits);
    t.Eq("two overlapping gazes leave a clean facing", openHits, 0);

    // The interesting result, and not the one worth assuming: with all four looking at once this is STILL
    // solvable. Every orb sat west of where the player stood, so their bearings span well under 270
    // degrees and the whole eastern arc stays open. Where you stand decides whether a gaze fight is a
    // facing problem at all.
    var allFour = Looking(stood, orbs, now);
    t.True("four at once is still escapable from the east edge",
        allFour.TryFindSafeFacing(now.AddSeconds(1), 0f.Degrees(), out _));

    // Surrounded is the case that has no answer: four eyes at even bearings each forbid 90 degrees, and
    // 4x90 is the circle. Constructed rather than taken from the log, because the log does not contain it.
    var centre = new WPos(0f, 0f);
    var ring = new[] { new WPos(0f, 20f), new WPos(20f, 0f), new WPos(0f, -20f), new WPos(-20f, 0f) };
    var boxed = Looking(centre, ring, now);
    t.True("evenly spaced eyes leave no safe facing", !boxed.TryFindSafeFacing(now.AddSeconds(1), 0f.Degrees(), out _));

    // ...and that is exactly when refusing to turn is the worst answer available
    t.True("but there is still a best facing", boxed.TryFindBestFacing(now.AddSeconds(1), 0f.Degrees(), out _, out var hits));
    t.Eq("hit by one gaze, not by all four", hits, 1);

    var sweep = 99;
    for (var d = 0; d < 360; ++d)
    {
        boxed.TryFindBestFacing(now.AddSeconds(1), ((float)d).Degrees(), out _, out var h);
        sweep = Math.Min(sweep, h);
    }

    t.Eq("and no heading anywhere does better", hits, sweep);

    // eight eyes: every heading is inside two arcs, and the answer is still two rather than nothing
    var dense = Looking(centre, Enumerable.Range(0, 8).Select(i => centre + ((i * 45f).Degrees().ToDirection() * 20f)), now);
    dense.TryFindBestFacing(now.AddSeconds(1), 0f.Degrees(), out _, out var denseHits);
    t.True("denser gazes degrade rather than fail", denseHits is > 0 and <= 3);

    // a facing already looking into one gaze turns just past its edge, not across the arena
    var single = Looking(stood, orbs.Take(1), now);
    var into = Angle.FromDirection(orbs[0] - stood);
    single.TryFindBestFacing(now.AddSeconds(1), into, out var moved, out var oneHits);
    t.Eq("a single gaze is always escapable", oneHits, 0);
    t.True("by a turn of about 45 degrees", MathF.Abs((moved - into).Normalized().Deg) is > 40f and < 50f);

    // gazes that have not started yet must not constrain the current turn
    var later = Looking(centre, ring, now.AddSeconds(30));
    t.True("gazes beyond the horizon are ignored", later.TryFindSafeFacing(now.AddSeconds(1), 0f.Degrees(), out _));
}

t.Section("CastType maps to the right shape");
{
    // Reported from a generated Regnant Chimera draft: two donuts came out as AOEShapeRect(30, 0). A
    // zero-width rectangle contains nothing, so the mechanic was drawn as literally empty. Cause was this
    // table -- 8 and 10 were swapped, and a donut in the rect bucket takes its width from XAxisModifier,
    // which a donut does not have. The table is copied, not derived, so it gets pinned entry by entry.
    static Minerva.Generation.ShapeHint R(byte castType, float range = 30f, float xmod = 8f, string? omen = null)
        => Minerva.Generation.CastTypeShapes.Resolve(castType, range, xmod, omen);

    t.Eq("1 is single-target", R(1).Kind, Minerva.Generation.ShapeKind.SingleTarget);
    t.Eq("2 is a circle", R(2).Kind, Minerva.Generation.ShapeKind.Circle);
    t.Eq("5 is a circle", R(5).Kind, Minerva.Generation.ShapeKind.Circle);
    t.Eq("7 is a circle, not a rect", R(7).Kind, Minerva.Generation.ShapeKind.Circle);
    t.Eq("4 is a rect", R(4).Kind, Minerva.Generation.ShapeKind.Rect);
    t.Eq("12 is a rect", R(12).Kind, Minerva.Generation.ShapeKind.Rect);
    t.Eq("11 is a cross", R(11).Kind, Minerva.Generation.ShapeKind.Cross);

    // the two that were wrong
    t.Eq("8 is a CHARGE, not a donut", R(8).Kind, Minerva.Generation.ShapeKind.Charge);
    t.Eq("10 is a DONUT, not a rect", R(10).Kind, Minerva.Generation.ShapeKind.Donut);

    // and the consequence that made it invisible rather than merely wrong
    var donut = R(10, range: 30f, xmod: 0f);
    t.Near("a donut keeps its outer radius", donut.Radius, 30f);
    t.True("and gets a non-zero inner radius to review", donut.InnerRadius > 0f && donut.InnerRadius < donut.Radius);
    t.True("a donut is flagged for review -- no sheet carries the inner radius", donut.NeedsReview);
    var emitted = donut.ToShapeExpression() ?? "";
    t.True("it emits a donut", emitted.StartsWith("new AOEShapeDonut(", StringComparison.Ordinal));
    t.True("and never a zero-width rect", !emitted.Contains("AOEShapeRect"));

    // a charge has no shape to emit at all: its length is the travel distance, known only at cast time
    var charge = R(8, range: 0f, xmod: 8f);
    t.Near("a charge keeps its half-width", charge.HalfWidth, 4f);
    t.True("but offers no shape expression", charge.ToShapeExpression() == null);
    t.True("and no shape", charge.ToShape() == null);
    t.True("and is flagged", charge.NeedsReview);

    // cones read their angle from the omen path, and say so when they cannot
    var named = R(3, range: 40f, omen: "gl_fan120_1bf");
    t.Eq("a cone is a cone", named.Kind, Minerva.Generation.ShapeKind.Cone);
    t.Near("and takes half the omen's angle", named.HalfAngleDeg, 60f);
    t.True("a named angle needs no review", !named.NeedsReview);
    var guessed = R(13, range: 40f, omen: null);
    t.Near("no omen falls back to 45", guessed.HalfAngleDeg, Minerva.Generation.CastTypeShapes.DefaultConeHalfAngleDeg);
    t.True("and admits it is a guess", guessed.NeedsReview);

    // 6 is "custom shapes" in BMR -- a circle is the best guess but must not pass silently
    t.True("6 is a guess, not a fact", R(6).NeedsReview);

    t.Eq("an unknown cast type stays unknown", R(99).Kind, Minerva.Generation.ShapeKind.Unknown);
}

t.Section("Auto-record only fires on the fight you are in");
{
    // Reported from the Occult Crescent base camp: auto-record started on a critical encounter happening
    // across the zone. The encounter director lists every event in the zone, so "a CE is in Battle" was
    // never evidence that this player was in one -- and that path started a recording on its own.
    static Actor Make(ulong id, ActorType type, float x, float z, uint maxHP, bool ally, bool inCombat = true, bool targetable = true)
    {
        var a = new Actor(id, 0x1000u + (uint)id, -1, $"a{id}", 0u, type, new Vector4(x, 0f, z, 0f), 1f,
            new ActorHPMP { CurHP = maxHP, MaxHP = maxHP }, targetable, ally);
        a.InCombat = inCombat;
        return a;
    }

    var me = Make(1, ActorType.Player, 0f, 0f, 50_000u, ally: true);
    var farBoss = Make(2, ActorType.Enemy, 400f, 0f, 5_000_000u, ally: false);
    var nearBoss = Make(3, ActorType.Enemy, 20f, 10f, 5_000_000u, ally: false);
    var trash = Make(4, ActorType.Enemy, 5f, 0f, 120_000u, ally: false);

    t.True("a boss across the zone is not our fight", EncounterTrigger.NearbyEngagedBoss(me, [farBoss]) == null);
    t.True("a boss beside us is", EncounterTrigger.NearbyEngagedBoss(me, [nearBoss]) != null);
    t.True("and it is the one we get back", ReferenceEquals(EncounterTrigger.NearbyEngagedBoss(me, [farBoss, nearBoss]), nearBoss));

    // the other two conditions still have to hold
    t.True("nearby trash is not a boss", EncounterTrigger.NearbyEngagedBoss(me, [trash]) == null);
    var outOfCombat = Make(5, ActorType.Player, 0f, 0f, 50_000u, ally: true, inCombat: false);
    t.True("a boss we have not engaged is not our fight", EncounterTrigger.NearbyEngagedBoss(outOfCombat, [nearBoss]) == null);
    var idleBoss = Make(6, ActorType.Enemy, 20f, 10f, 5_000_000u, ally: false, inCombat: false);
    t.True("a boss idling next to us is not either", EncounterTrigger.NearbyEngagedBoss(me, [idleBoss]) == null);
    var untargetable = Make(7, ActorType.Enemy, 20f, 10f, 5_000_000u, ally: false, targetable: false);
    t.True("an untargetable boss does not start it", EncounterTrigger.NearbyEngagedBoss(me, [untargetable]) == null);

    // the boundary, so the constant cannot drift unnoticed
    var atEdge = Make(8, ActorType.Enemy, EncounterTrigger.MaxDistance - 1f, 0f, 5_000_000u, ally: false);
    var pastEdge = Make(9, ActorType.Enemy, EncounterTrigger.MaxDistance + 1f, 0f, 5_000_000u, ally: false);
    t.True("just inside the range counts", EncounterTrigger.NearbyEngagedBoss(me, [atEdge]) != null);
    t.True("just outside it does not", EncounterTrigger.NearbyEngagedBoss(me, [pastEdge]) == null);

    t.True("no player, no recording", EncounterTrigger.NearbyEngagedBoss(null, [nearBoss]) == null);
}

t.Section("Analytic shape distances agree with their own outline");
{
    // Every primitive now reports a real signed distance instead of the +/-1 boolean fallback. A wrong one
    // is worse than none: it silently mis-scores every cell the dodge considers, and nothing crashes. So
    // check each against a source that cannot share its mistake -- brute-force distance to the shape's own
    // Contour(), signed by its own Check(). The two derivations have nothing in common but the geometry.
    // (A sector distance hand-derived once before was out by 10.5 yalms; only this comparison caught it.)
    static float ToContour(WPos p, IReadOnlyList<WPos> poly)
    {
        var best = float.MaxValue;
        for (var i = 0; i < poly.Count; ++i)
        {
            var a = poly[i];
            var b = poly[(i + 1) % poly.Count];
            var ab = b - a;
            var lenSq = ab.LengthSq();
            var s = lenSq > 1e-6f ? Math.Clamp((p - a).Dot(ab) / lenSq, 0f, 1f) : 0f;
            best = MathF.Min(best, (p - (a + (ab * s))).Length());
        }

        return best;
    }

    // tolerance has to admit the outline's own tessellation error: an arc drawn as N chords sits inside the
    // true arc by the sagitta, so the polygon reads slightly nearer than the analytic answer
    // loops override the shape's own Contour for shapes whose outline is not a single closed ring: a donut
    // is emitted as outer-then-inner in one list, and the bridge between them is a seam the real boundary
    // does not have. Measuring to that seam would fail the shape for the outline's shortcoming.
    void Agrees(string name, AOEShape shape, WPos origin, Angle rot, float span, float tol, IReadOnlyList<WPos>[]? loops = null)
    {
        var worst = 0f;
        var worstAt = default(WPos);
        var rings = loops ?? [shape.Contour(origin, rot)];
        var sd = shape.Distance(origin, rot);
        for (var x = -span; x <= span; x += span / 12f)
        {
            for (var z = -span; z <= span; z += span / 12f)
            {
                var p = origin + new WDir(x, z);
                var nearest = float.MaxValue;
                foreach (var ring in rings)
                    nearest = MathF.Min(nearest, ToContour(p, ring));
                var expect = nearest * (shape.Check(p, origin, rot) ? -1f : 1f);
                var got = sd.Distance(p);
                var err = MathF.Abs(expect - got);
                if (err > worst)
                {
                    worst = err;
                    worstAt = p;
                }
            }
        }

        t.True($"{name} matches its outline (worst {worst:f3}y at {worstAt.X:f1},{worstAt.Z:f1})", worst <= tol);
    }

    var o = new WPos(10f, -20f);          // deliberately off-origin: a shape that only works at (0,0) is a trap
    var rot = 37f.Degrees();              // and off-axis, to catch a dropped rotation

    Agrees("circle", new AOEShapeCircle(8f), o, rot, 16f, 0.08f);
    Agrees("donut", new AOEShapeDonut(4f, 10f), o, rot, 18f, 0.08f,
        [new AOEShapeCircle(10f).Contour(o, rot), new AOEShapeCircle(4f).Contour(o, rot)]);
    Agrees("capsule", new AOEShapeCapsule(3f, 12f), o, rot, 18f, 0.12f);
    Agrees("rect", new AOEShapeRect(12f, 4f, 3f), o, rot, 20f, 0.01f);
    Agrees("cone", new AOEShapeCone(10f, 45f.Degrees()), o, rot, 18f, 0.12f);
    Agrees("wide cone", new AOEShapeCone(10f, 150f.Degrees()), o, rot, 18f, 0.12f);
    Agrees("narrow cone", new AOEShapeCone(14f, 15f.Degrees()), o, rot, 20f, 0.12f);
    Agrees("donut sector", new AOEShapeDonutSector(5f, 12f, 60f.Degrees()), o, rot, 20f, 0.12f);
    Agrees("tri cone", new AOEShapeTriCone(10f, 30f.Degrees()), o, rot, 18f, 0.12f);
    Agrees("cone with a direction offset", new AOEShapeCone(9f, 40f.Degrees(), 90f.Degrees()), o, rot, 18f, 0.12f);
    // Lost on the Wind's roaming winds: a 4y tube sweeping 30 degrees either way round an orbit centre 16y
    // off. The counter-clockwise one is the regression: its end caps used to be drawn as fixed positive
    // half-turns, which fold back through the body, and the folded outline filled as a wedge on the radar.
    var orbit = o + new WDir(0f, 16f);
    Agrees("arc capsule, clockwise", new AOEShapeArcCapsule(4f, 30f.Degrees(), orbit), o, rot, 12f, 0.15f);
    Agrees("arc capsule, counter-clockwise", new AOEShapeArcCapsule(4f, -30f.Degrees(), orbit), o, rot, 12f, 0.15f);

    // the corner case that the old max()-only rectangle got wrong: 3 out one way and 4 the other is 5 away,
    // not 4. Understating it makes the dodge treat clear ground near a corner as marginal.
    var rectSD = new AOEShapeRect(10f, 5f).Distance(new WPos(0f, 0f), 0f.Degrees());
    // rotation 0 faces south (+Z), so the box spans z in [0,10] and x in [-5,5]
    t.Near("past a corner the distance is the diagonal", rectSD.Distance(new WPos(8f, 14f)), 5f, 0.01f);
    t.Near("straight off a face it is not", rectSD.Distance(new WPos(0f, 14f)), 4f, 0.01f);

    // and the cone's units error: an angle in radians is not a distance in yalms
    var coneSD = new AOEShapeCone(20f, 30f.Degrees()).Distance(new WPos(0f, 0f), 0f.Degrees());
    // dead behind the apex, 10 yalms out: nothing of the cone is nearer than the apex itself
    t.Near("behind the apex, the apex is the nearest point", coneSD.Distance(new WPos(0f, -10f)), 10f, 0.01f);
    t.True("which the old radians-vs-yalms form could never have said", coneSD.Distance(new WPos(0f, -10f)) > 3f);

    // the cross keeps the boolean-free path too, and outside the shape a union of exact parts is exact
    var cross = new AOEShapeCross(10f, 2f);
    var crossSD = cross.Distance(new WPos(0f, 0f), 0f.Degrees());
    t.True("inside an arm reads as inside", crossSD.Distance(new WPos(0f, 5f)) < 0f);
    t.True("the diagonal gap between arms is outside", crossSD.Distance(new WPos(6f, 6f)) > 0f);
    // the arms run out to 10, so from (6,6) the nearest boundary is the flank of an arm, not the inner
    // corner where they meet -- 4 yalms sideways to either
    t.Near("and measures to the nearer arm's flank", crossSD.Distance(new WPos(6f, 6f)), 4f, 0.01f);

    // nothing should still be falling back to the boolean
    foreach (var (name, shape) in new (string, AOEShape)[]
    {
        ("circle", new AOEShapeCircle(5f)), ("donut", new AOEShapeDonut(2f, 5f)),
        ("cone", new AOEShapeCone(5f, 30f.Degrees())), ("donut sector", new AOEShapeDonutSector(2f, 5f, 30f.Degrees())),
        ("rect", new AOEShapeRect(5f, 2f)), ("cross", new AOEShapeCross(5f, 1f)),
        ("tri cone", new AOEShapeTriCone(5f, 30f.Degrees())), ("capsule", new AOEShapeCapsule(2f, 5f)),
    })
    {
        t.True($"{name} no longer uses the boolean fallback", shape.Distance(new WPos(0f, 0f), default) is not SDShapeCheck);
    }
}

t.Section("Cast budget and lead time");
{
    var now = DateTime.Now;
    var me = new WPos(0f, 0f);

    // "Is this spot safe for eight seconds so I can hardcast a raise?" -- the question a healer has to
    // answer before committing, and one a present-tense safe/unsafe flag cannot.
    var clear = new AIHints { Center = me, Bounds = new ArenaBoundsCircle(20f), PlayerPosition = me };
    t.True("nothing pending means cast anything", clear.MaxCastTime(now) == float.MaxValue);

    // an AOE landing elsewhere is not our problem
    var elsewhere = new AIHints { Center = me, Bounds = new ArenaBoundsCircle(20f), PlayerPosition = me };
    elsewhere.AddForbiddenZone(new AOEShapeCircle(4f), new WPos(15f, 0f), default, now.AddSeconds(5));
    t.True("an AOE we are not standing in does not shorten the cast", elsewhere.MaxCastTime(now) == float.MaxValue);

    // one landing on us does, and the walk out is charged for: 10y circle, we are dead centre, so 10y of
    // escape at 6 y/s is 1.67s of the 6s we had
    var onUs = new AIHints { Center = me, Bounds = new ArenaBoundsCircle(20f), PlayerPosition = me };
    onUs.AddForbiddenZone(new AOEShapeCircle(10f), me, default, now.AddSeconds(6));
    t.Near("leaving costs time, so the budget is less than the countdown", onUs.MaxCastTime(now, 0f, 6f), 6f - (10f / 6f), 0.2f);

    // standing near the edge of the same zone is cheaper to leave, so more of the countdown survives
    var nearEdge = new AIHints { Center = me, Bounds = new ArenaBoundsCircle(20f), PlayerPosition = new WPos(0f, 9f) };
    nearEdge.AddForbiddenZone(new AOEShapeCircle(10f), me, default, now.AddSeconds(6));
    t.Near("a shallower stand keeps more of the budget", nearEdge.MaxCastTime(now, 0f, 6f), 6f - (1f / 6f), 0.2f);
    t.True("which is strictly better than standing dead centre", nearEdge.MaxCastTime(now, 0f, 6f) > onUs.MaxCastTime(now, 0f, 6f));

    // the soonest of several wins
    var two = new AIHints { Center = me, Bounds = new ArenaBoundsCircle(20f), PlayerPosition = me };
    two.AddForbiddenZone(new AOEShapeCircle(6f), me, default, now.AddSeconds(9));
    two.AddForbiddenZone(new AOEShapeCircle(6f), me, default, now.AddSeconds(3));
    t.True("the earliest one sets the budget", two.MaxCastTime(now, 0f, 6f) < 3f);

    // a stand-still punisher ends the cast whatever the ground is doing
    var pyretic = new AIHints { Center = me, Bounds = new ArenaBoundsCircle(20f), PlayerPosition = me };
    pyretic.AddSpecialMode(AIHints.SpecialMode.Pyretic, now.AddSeconds(-1));
    t.Near("no cast at all while acting is punished", pyretic.MaxCastTime(now), 0f);

    // ... and is seen coming, which is the whole point of lead time
    var soon = new AIHints { Center = me, Bounds = new ArenaBoundsCircle(20f), PlayerPosition = me };
    soon.AddSpecialMode(AIHints.SpecialMode.Pyretic, now.AddSeconds(4));
    t.Near("a punisher four seconds out caps the cast at four", soon.MaxCastTime(now), 4f, 0.2f);
    t.True("and is reported before it lands", soon.NextMustNotAct(now) != null);
    t.True("while the present-tense flag is still false", !soon.MustNotAct(now));

    // gaze lead time: a rotation needs to know whether its next ability resolves before the snapshot
    var gaze = new AIHints { Center = me, Bounds = new ArenaBoundsCircle(20f), PlayerPosition = me };
    t.True("no gaze pending reads as none", gaze.NextGazeResolve(now) == null);
    gaze.ForbiddenDirections.Add((default, 45f.Degrees(), now.AddSeconds(2.5)));
    t.True("a pending gaze has a resolve time", gaze.NextGazeResolve(now) != null);
    t.Near("and it is when facing gets snapshotted", (float)(gaze.NextGazeResolve(now)!.Value - now).TotalSeconds, 2.5f, 0.2f);
}

t.Section("Uptime is a band, not a point");
{
    var now = DateTime.Now;
    var boss = new WPos(0f, 0f);

    // Roles do not share a definition of uptime. Scoring everyone against melee reach is what walks a
    // caster into a boss's melee band to save a yard of travel on a dodge.
    t.Near("a tank wants melee reach", UptimeGoal.ReachFor(Role.Tank), 2.6f);
    t.Near("a melee dps wants the same", UptimeGoal.ReachFor(Role.Melee), 2.6f);
    t.Near("a healer wants 15", UptimeGoal.ReachFor(Role.Healer), 15f);
    t.Near("so does a ranged", UptimeGoal.ReachFor(Role.Ranged), 15f);
    // standing too far back costs damage; standing too close costs the pull. The second is worse.
    t.Near("an unknown role is treated as ranged", UptimeGoal.ReachFor(Role.None), 15f);

    // reach is measured from the hitbox, as the game measures everything
    var fatBoss = new Actor(1ul, 0x1234u, -1, "boss", 0u, ActorType.Enemy, new Vector4(0f, 0f, 0f, 0f), hitboxRadius: 8f);
    t.Near("a big hitbox pushes melee range out with it", UptimeGoal.For(fatBoss, Role.Melee).Range, 10.6f);
    t.Near("and ranged too", UptimeGoal.For(fatBoss, Role.Ranged).Range, 23f);

    // inside the band every cell is equally good for uptime, so the dodge spends its budget on safety
    var caster = new UptimeGoal(boss, default, 15f);
    t.Near("dead on the boss is inside a caster's band", caster.ExcessRange(boss), 0f);
    t.Near("so is 14 yalms out", caster.ExcessRange(new WPos(0f, 14f)), 0f);
    t.Near("the band edge is still inside it", caster.ExcessRange(new WPos(0f, 15f)), 0f);
    t.Near("and 20 yalms is five outside", caster.ExcessRange(new WPos(0f, 20f)), 5f);

    // the point of the change: a caster dodging a small AOE must not be dragged to the boss to do it
    var hints = new AIHints { Center = boss, Bounds = new ArenaBoundsCircle(30f), PlayerPosition = new WPos(0f, 12f) };
    hints.AddForbiddenZone(new AOEShapeCircle(3f), new WPos(0f, 12f), default, now.AddSeconds(2));
    var dodged = ArenaPathfinder.Solve(hints, now, 5f, 1f, 0f, caster);
    t.True("a caster dodges somewhere", dodged.Found);
    var moved = (dodged.Target - new WPos(0f, 12f)).Length();
    t.True("and does not cross the arena to do it", moved < 8f);
    t.Near("staying within its own band", caster.ExcessRange(dodged.Target), 0f, 0.01f);
    t.True("without being pulled into the boss's lap", (dodged.Target - boss).Length() > 6f);

    // melee still closes: outside the band, the pull is real
    var melee = new UptimeGoal(boss, default, 2.6f);
    var far = new AIHints { Center = boss, Bounds = new ArenaBoundsCircle(30f), PlayerPosition = new WPos(0f, 20f) };
    var back = ArenaPathfinder.Solve(far, now, 5f, 1f, 0f, melee);
    t.True("a melee out of range walks back in", back.NeedToMove);
    t.True("all the way to melee range", (back.Target - boss).Length() <= 2.6f + 1.5f);

    // and a caster at the same distance is already where it wants to be
    var casterFar = ArenaPathfinder.Solve(
        new AIHints { Center = boss, Bounds = new ArenaBoundsCircle(30f), PlayerPosition = new WPos(0f, 12f) },
        now, 5f, 1f, 0f, caster);
    t.True("a caster at 12 yalms has nothing to walk back for", !casterFar.NeedToMove);
}

t.Section("Arena footprint (no-module fallback bounds)");
{
    // The reported failure: with no boss module the dodge gets a fixed box centred on the player -- BMR a
    // 60x60 square, Minerva a 30y circle. Standing at a platform edge, half that box hangs over the drop,
    // so the far side of an edge-hugging AOE looks like clear ground and the character is steered off.
    var fp = new ArenaFootprint();
    t.True("nothing observed yet means no estimate", !fp.TryEstimate(out _, out _));

    // one actor parked in a corner is not an arena
    for (var i = 0; i < ArenaFootprint.MinSamples; ++i)
        fp.Observe(new WPos(100f, 100f));
    t.True("samples all in one spot stay untrusted", !fp.TryEstimate(out _, out _));

    // a real fight: a 20y-radius platform centred at (100, 100), people spread across it
    var arena = new ArenaFootprint();
    for (var i = 0; i < 36; ++i)
    {
        var a = (i * 10f).Degrees();
        arena.Observe(new WPos(100f, 100f) + (a.ToDirection() * 18f));
    }

    t.True("a fight's worth of samples is an estimate", arena.TryEstimate(out var center, out var bounds));
    t.Near("centred on the arena, not on anyone in it", center.X, 100f, 0.5f);
    t.Near("and on its Z too", center.Z, 100f, 0.5f);
    // 18y of spread each way plus the 2y margin -- Radius is the circumradius, so check the extent itself
    t.Near("extent tracks the observed spread plus a small margin", ((ArenaBoundsRect)bounds).HalfWidth, 20f, 0.5f);

    // The whole point. Standing at the east edge, a dodge 12y further east is over the drop. The estimate
    // knows where the platform ends because nobody has ever stood out there; the player-centred fallback
    // has no idea and calls it clear ground.
    var atEdge = new WPos(118f, 100f);
    var overTheDrop = new WPos(130f, 100f);
    t.True("standing at the edge is still inside the estimate", bounds.Contains(center, atEdge));
    t.True("a dodge past the edge is not", !bounds.Contains(center, overTheDrop));

    var blind = new ArenaBoundsCircle(30f);
    t.True("the player-centred fallback would have walked you off it", blind.Contains(atEdge, overTheDrop));

    // a different storey is not this arena
    var storeys = new ArenaFootprint();
    for (var i = 0; i < 36; ++i)
        storeys.Observe(new WPos(100f + (i * 3f), 100f), dy: 40f);
    t.True("samples on another floor are ignored", !storeys.TryEstimate(out _, out _));
    t.Eq("and never counted", storeys.Samples, 0);

    // zone change forgets the last arena
    arena.EnterZone(1);
    arena.EnterZone(2);
    t.True("a new zone starts from nothing", !arena.TryEstimate(out _, out _));
}

t.Section("Line-of-sight safe zones");
{
    // Treno's mechanic in miniature: a boss at the centre, boulders around it, each casting a shadow you can
    // hide in. Check() has always known about all of them; the drawing only ever showed the first.
    var origin = new WPos(84f, 370f);
    Shape[] shadows =
    [
        new DonutSegmentHA(origin, 6f, 30f, 0f.Degrees(), 20f.Degrees()),
        new DonutSegmentHA(origin, 6f, 30f, 90f.Degrees(), 20f.Degrees()),
        new DonutSegmentHA(origin, 6f, 30f, 180f.Degrees(), 20f.Degrees()),
    ];
    var safe = new AOEShapeCustom(origin, shadows, invertForbiddenZone: true);
    t.Eq("every blocker's shadow is drawable", safe.Contours(origin, default).Count, 3);

    // inverted: the safe zone is the shadows, so standing in one must NOT be flagged, and open ground must be
    t.True("standing in a shadow is safe", !safe.Check(origin + new WDir(0f, 12f), origin, default));
    t.True("standing in the open is not", safe.Check(origin + new WDir(12f, 12f), origin, default));

    // the field must have a gradient, or the dodge knows it is unsafe without knowing which way to walk
    var field = safe.Distance(origin, default);
    var inShadow = origin + new WDir(0f, 12f);              // inside the 90-degree shadow
    var justOutside = origin + new WDir(5f, 11f);           // open ground, near that shadow's edge
    var farAway = origin + new WDir(12f, -12f);             // open ground, far from every shadow
    // negative is forbidden (ShapeDistance.Contains), so on an inverted zone the shadows read positive
    t.True("safe ground reads as safe", field.Distance(inShadow) > 0f);
    t.True("open ground reads as forbidden", field.Distance(justOutside) <= 0f);
    t.True("the field deepens away from cover", field.Distance(farAway) < field.Distance(justOutside));

    // an annular sector is concave: a fan from vertex 0 would cover the hollow, ear clipping must not
    var sector = new DonutSegmentHA(origin, 6f, 30f, 0f.Degrees(), 20f.Degrees()).ContourWorld();
    var tris = EarClip.Triangulate(sector);
    t.Eq("triangulating a sector yields n-2 triangles", tris.Count, sector.Count - 2);
    var area = 0f;
    foreach (var (a, b, c) in tris)
        area += MathF.Abs(((sector[b].X - sector[a].X) * (sector[c].Z - sector[a].Z)) - ((sector[b].Z - sector[a].Z) * (sector[c].X - sector[a].X))) * 0.5f;
    // true area of a 40-degree slice of a 6..30 ring, which a fan across the hollow would badly overshoot
    t.Near("triangles cover the ring slice, not the hole", area, MathF.PI * (30f * 30f - 6f * 6f) * (40f / 360f), 12f);
}

t.Section("Custom shapes + arenas");
{
    // Shape containment: a rotated-agnostic square, a regular polygon, and a cross
    var square = new Square(new WPos(0, 0), 10f);
    t.True("square contains an interior point", square.Contains(new WPos(5, -5)));
    t.True("square excludes an exterior point", !square.Contains(new WPos(11, 0)));
    var hexagon = new Polygon(new WPos(0, 0), 10f, 6);
    t.True("polygon contains its centre", hexagon.Contains(new WPos(0, 0)));
    t.True("polygon excludes a far point", !hexagon.Contains(new WPos(20, 20)));
    var cross = new Cross(new WPos(0, 0), 10f, 2f);
    t.True("cross contains a point on its arm", cross.Contains(new WPos(0, 8)));
    t.True("cross excludes a diagonal gap", !cross.Contains(new WPos(8, 8)));

    // ArenaBoundsCustom: a 20y square field with a 5y circular pit in the middle
    var bounds = new ArenaBoundsCustom([new Square(new WPos(100, 100), 20f)], [new Circle(new WPos(100, 100), 5f)]);
    t.True("inside the field, outside the pit -> contained", bounds.Contains(new WPos(100, 100), new WPos(112, 100)));
    t.True("inside the central pit -> not contained", !bounds.Contains(new WPos(100, 100), new WPos(100, 100)));
    t.True("outside the field -> not contained", !bounds.Contains(new WPos(100, 100), new WPos(125, 100)));

    // AOEShapeCustom: union of two circles minus a smaller one, plus the inverted variant
    var custom = new AOEShapeCustom([new Circle(new WPos(0, 0), 6f), new Circle(new WPos(20, 0), 6f)], [new Circle(new WPos(0, 0), 2f)]);
    t.True("custom AOE hits inside the second circle", custom.Check(new WPos(20, 0), default, default));
    t.True("custom AOE misses between the circles", !custom.Check(new WPos(10, 0), default, default));
    t.True("custom AOE difference carves out the centre", !custom.Check(new WPos(0, 0), default, default));
    var inverted = new AOEShapeCustom([new Circle(new WPos(0, 0), 6f)], invertForbiddenZone: true);
    t.True("inverted custom AOE is safe inside, dangerous outside", inverted.Check(new WPos(50, 50), default, default) && !inverted.Check(new WPos(0, 0), default, default));

    // more operand shapes: ellipse, donut segment, capsule
    var ellipse = new Ellipse(new WPos(0, 0), 10f, 4f, 24);
    t.True("ellipse contains a point along its long axis", ellipse.Contains(new WPos(9, 0)));
    t.True("ellipse excludes a point along its short axis past the radius", !ellipse.Contains(new WPos(0, 5)));
    var seg = new DonutSegmentHA(new WPos(0, 0), 5f, 10f, 0f.Degrees(), 45f.Degrees()); // sector facing +Z (south)
    t.True("donut segment contains a ring point in-sector", seg.Contains(new WPos(0, 7)));
    t.True("donut segment excludes a point inside the inner hole", !seg.Contains(new WPos(0, 3)));
    t.True("donut segment excludes a point out of the sector", !seg.Contains(new WPos(0, -7)));
    var capsule = new Capsule(new WPos(0, 0), 10f, 2f, 16); // core segment ±10 along +Z, radius 2
    t.True("capsule contains a point near its core", capsule.Contains(new WPos(1, 5)));
    t.True("capsule excludes a point beyond its rounded end", !capsule.Contains(new WPos(0, 13)));
}

// ---------------------------------------------------------------------------
// 9c-quater. GameSync data spine: party slots + class/role (drives Raid.* + role logic)
// ---------------------------------------------------------------------------
t.Section("Party + class/role sync");
{
    var ws = new WorldState(10_000_000, "test");
    ws.Execute(new WorldState.OpFrameStart(Frame(ws, 0, 0f), TimeSpan.Zero));
    const ulong tank = 0x10000001, healer = 0x10000002;
    ws.Execute(new ActorState.OpCreate(tank, 1, 0, "T", 0, ActorType.Player, new Vector4(100, 0, 100, 0), 0.5f, default, true, true, 0));
    ws.Execute(new ActorState.OpCreate(healer, 2, 1, "H", 0, ActorType.Player, new Vector4(103, 0, 100, 0), 0.5f, default, true, true, 0));
    ws.Execute(new ActorState.OpClassChange(tank, Class.WAR));
    ws.Execute(new ActorState.OpClassChange(healer, Class.WHM));
    t.Eq("class change derives Tank role", (int)ws.Actors.Find(tank)!.Role, (int)Role.Tank);
    t.Eq("class change derives Healer role", (int)ws.Actors.Find(healer)!.Role, (int)Role.Healer);

    ws.Execute(new PartyState.OpModify(0, new PartyState.Member(0xAA, tank)));
    ws.Execute(new PartyState.OpModify(1, new PartyState.Member(0xBB, healer)));
    t.Eq("party WithSlot lists both live members", ws.Party.WithSlot().Length, 2);
    t.Eq("party FindSlot resolves by instance id", ws.Party.FindSlot(healer), 1);
    ws.Party.PlayerInstanceID = tank;
    t.Eq("party Player() resolves the POV", (long)ws.Party.Player()!.InstanceID, (long)tank);

    // model-state + action-timeline ops raise their events (which ModuleBase fans out to component hooks)
    byte gotModel = 0;
    ushort gotTimeline = 0;
    ws.Actors.ModelStateChanged.Subscribe((a, s) => gotModel = s);
    ws.Actors.ActionTimelineEvent.Subscribe((a, id) => gotTimeline = id);
    ws.Execute(new ActorState.OpModelState(tank, 7));
    ws.Execute(new ActorState.OpModelState(tank, 7, 1, 2));
    t.True("model state carries both animation bytes", ws.Actors.Find(tank)!.ModelState is { ModelState: 7, AnimState1: 1, AnimState2: 2 });
    ws.Execute(new ActorState.OpModelState(tank, 7));
    ws.Execute(new ActorState.OpActionTimeline(tank, 0x123));
    t.Eq("model-state op fires its event", gotModel, (byte)7);
    t.Eq("action-timeline op fires its event", gotTimeline, (ushort)0x123);

    // event-object / renderflags / event-state ops fire their events and persist where applicable
    byte gotEventState = 0;
    ushort gotEState = 0;
    uint gotEAnim = 0;
    var gotRender = 0;
    ws.Actors.EventStateChanged.Subscribe((a, v) => gotEventState = v);
    ws.Actors.EStateChanged.Subscribe((a, s) => gotEState = s);
    ws.Actors.EAnimChanged.Subscribe((a, s) => gotEAnim = s);
    ws.Actors.RenderflagsChanged.Subscribe((a, f) => gotRender = f);
    ws.Execute(new ActorState.OpEventState(tank, 5));
    ws.Execute(new ActorState.OpActorEState(tank, 0x11));
    ws.Execute(new ActorState.OpActorEAnim(tank, 0x00220011));
    ws.Execute(new ActorState.OpRenderflags(tank, 0x40));
    t.Eq("event-state op fires + persists on the actor", (int)ws.Actors.Find(tank)!.EventState, 5);
    t.Eq("event-state event carries the value", (int)gotEventState, 5);
    t.Eq("event-object state event fires", (int)gotEState, 0x11);
    t.Eq("event-object animation event fires", (long)gotEAnim, 0x00220011L);
    t.Eq("renderflags op fires + persists on the actor", ws.Actors.Find(tank)!.Renderflags, 0x40);
    t.Eq("renderflags event carries the value", gotRender, 0x40);
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

// ---------------------------------------------------------------------------
// 13. Offline validation round trip: recorder -> parser -> validator
//     This is the path `minerva-validate` (and /mine -> Replay -> Validate) runs on, with no game.
// ---------------------------------------------------------------------------
t.Section("Offline validation round trip");
{
    var ws = new WorldState(10_000_000, "test");
    var sw = new StringWriter();
    using (var rec = new ReplayRecorder(ws, sw))
    {
        var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        void Tick(double sec) => ws.Execute(new WorldState.OpFrameStart(new FrameState { Timestamp = t0.AddSeconds(sec) }, TimeSpan.FromSeconds(1)));

        Tick(0);
        ws.Execute(new WorldState.OpZoneChange((ushort)9001, (ushort)9001));
        ws.Execute(new ActorState.OpCreate(0x100, 0xBEEFu, 0, "Probe", 0, ActorType.Enemy, new Vector4(100, 0, 100, 0), 2f, default, true, false, 0));
        ws.Execute(new ActorState.OpCreate(0x200, 0u, 0, "You", 0, ActorType.Player, new Vector4(100, 0, 108, 0), 0.5f, default, true, true, 0));
        // Duty Support: an NPC party member running its own rotation. Its casts are not mechanics.
        ws.Execute(new ActorState.OpCreate(0x300, 0x41EBu, 0, "Alphinaud's Avatar", 0, ActorType.Buddy, new Vector4(100, 0, 106, 0), 0.5f, default, true, false, 0x200));
        ws.Execute(new ActorState.OpCombat(0x100, true));

        void CastFrom(ulong who, double at, uint aid)
        {
            Tick(at);
            ws.Execute(new ActorState.OpCastInfo(who, new ActorCastInfo
            { Action = ActionID.MakeSpell(aid), TargetID = 0x200, TotalTime = 3f, Location = new Vector3(100, 0, 100) }));
            Tick(at + 3);
            ws.Execute(new ActorState.OpCastEvent(who, new ActorCastEvent(ActionID.MakeSpell(aid), 0x200, default, new Vector3(100, 0, 100), 1)));
            ws.Execute(new ActorState.OpCastInfo(who, null));
        }

        void Cast(double at, uint aid) => CastFrom(0x100, at, aid);

        Cast(2, 4242u);  // covered by ProbeCovered
        Cast(8, 7777u);  // nothing watches this
        CastFrom(0x300, 14, 3333u); // the NPC healer casting: must not read as an unhandled mechanic
        Tick(20);
    }

    // a ground-targeted cast's landing spot must survive record -> parse. Without it a replay can only fall
    // back to the caster's position, so a puddle placed by a helper parked at the arena centre replays at the
    // centre instead of where it hit — and every location-derived signal (exaflare spread, concentric rings,
    // the arena estimate) reads zero movement.
    {
        var locWs = new WorldState(10_000_000, "test");
        var locOut = new StringWriter();
        using (var rec3 = new ReplayRecorder(locWs, locOut))
        {
            locWs.Execute(new WorldState.OpFrameStart(new FrameState { Timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }, TimeSpan.FromSeconds(1)));
            locWs.Execute(new ActorState.OpCreate(0x1, 0xABCu, 0, "C", 0, ActorType.Enemy, new Vector4(0, 0, 0, 0), 1f, default, true, false, 0));
            locWs.Execute(new ActorState.OpCastInfo(0x1, new ActorCastInfo
            {
                Action = ActionID.MakeSpell(4242u),
                TargetID = 0,
                TotalTime = 5f,
                Location = new Vector3(12.5f, -3f, -47.25f),
            }));
        }

        var locTimeline = ReplayParser.ParseTimeline(new StringReader(locOut.ToString()));
        var locCast = locTimeline.Ops.Select(o => o.Op).OfType<ActorState.OpCastInfo>().FirstOrDefault(o => o.Value != null);
        t.True("ground cast survived the round trip", locCast?.Value != null);
        t.Near("cast landing X preserved", locCast?.Value?.LocXZ.X ?? 0f, 12.5f);
        t.Near("cast landing Z preserved", locCast?.Value?.LocXZ.Z ?? 0f, -47.25f);

        // a pre-location recording ends after TotalTime; it must still parse rather than throw
        var legacy = ReplayParser.ParseTimeline(new StringReader(
            "MINERVA-REPLAY 1 10000000 test" + Environment.NewLine + "0 CST+ 1 1092 0 0.000 0.000 5.000" + Environment.NewLine));
        var legacyCast = legacy.Ops.Select(o => o.Op).OfType<ActorState.OpCastInfo>().FirstOrDefault();
        t.True("a pre-location recording still parses", legacyCast?.Value != null);
        var legacyLoc = legacyCast?.Value?.LocXZ ?? new WPos(1f, 1f); // ?. would lift WPos to WPos? and compare against null
        t.True("legacy cast has no landing spot", legacyLoc == default);
    }

    // cast events carry the per-target effect list the game sends; it must survive record -> parse.
    // Older recordings have no target section at all, so the parser must still accept those.
    {
        var evWs = new WorldState(10_000_000, "test");
        var evOut = new StringWriter();
        using (var rec2 = new ReplayRecorder(evWs, evOut))
        {
            evWs.Execute(new WorldState.OpFrameStart(new FrameState { Timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }, TimeSpan.FromSeconds(1)));
            evWs.Execute(new ActorState.OpCreate(0x1, 0xABCu, 0, "C", 0, ActorType.Enemy, new Vector4(0, 0, 0, 0), 1f, default, true, false, 0));
            var ev = new ActorCastEvent(ActionID.MakeSpell(1234u), 0x2, default, default, 7u);
            ev.Targets.Add(new ActorCastEvent.Target(0x2, [1ul, 0, 0, 0, 0, 0, 0, 0]));
            ev.Targets.Add(new ActorCastEvent.Target(0x3, [0xDEADul, 0xBEEFul, 0, 0, 0, 0, 0, 0]));
            evWs.Execute(new ActorState.OpCastEvent(0x1, ev));
        }

        var evTimeline = ReplayParser.ParseTimeline(new StringReader(evOut.ToString()));
        var parsed = evTimeline.Ops.Select(o => o.Op).OfType<ActorState.OpCastEvent>().FirstOrDefault();
        t.True("cast event survived the round trip", parsed != null);
        t.Eq("both targets round-tripped", parsed!.Value.Targets.Count, 2);
        t.Eq("target id preserved", parsed.Value.Targets[1].ID, 0x3ul);
        t.Eq("effect slot preserved", parsed.Value.Targets[1].Effects[1], 0xBEEFul);

        // a pre-targets recording (no count/target tokens) still parses, with an empty target list
        var legacy = evOut.ToString().Split('\n')
            .Select(l => l.Contains("CST!") ? string.Join(' ', l.Split(' ').Take(7)) : l);
        var legacyTimeline = ReplayParser.ParseTimeline(new StringReader(string.Join("\n", legacy)));
        var legacyEv = legacyTimeline.Ops.Select(o => o.Op).OfType<ActorState.OpCastEvent>().FirstOrDefault();
        t.True("pre-targets recording still parses", legacyEv != null);
        t.Eq("legacy event has no targets", legacyEv!.Value.Targets.Count, 0);
    }

    var timeline = ReplayParser.ParseTimeline(new StringReader(sw.ToString()));
    t.True("recording parsed back into a timeline", timeline.Ops.Count > 0);

    var registry = ModuleRegistry.Build(typeof(ValidateProbeModule).Assembly);
    t.True("probe module registered", registry.ForCFC(9001u).Count == 1);

    var result = ReplayValidator.Validate(timeline, registry);
    t.Eq("validator picked the right module", result.ModuleName, "ValidateProbeModule");
    // Duty Support NPCs run full job rotations. Counting their casts as enemy actions put 21 of them into
    // one Treno report's uncovered list, against 2 real ones -- and this report is the module loop's
    // pass/fail signal, so noise in it is not cosmetic.
    t.Eq("validator saw both enemy casts and neither ally cast", result.EnemyActions, 2);
    t.True("a Duty Support cast is not an uncovered mechanic",
        !result.UncoveredMechanics.Contains(3333u) && !result.UncoveredVisuals.Contains(3333u));
    t.True("the covered cast is reported as drawn", result.Drawn.Contains(4242u));
    t.True("the unwatched cast is reported as uncovered",
        result.UncoveredMechanics.Contains(7777u) || result.UncoveredVisuals.Contains(7777u));
    t.True("render produces a report", result.Render().Length > 0);
}

// ---------------------------------------------------------------------------
// Party roles from an external roster
// ---------------------------------------------------------------------------
t.Section("Party roles");
{
    const string full = """
    [
      {"name":"A","contentId":1,"role":"MT"},
      {"name":"B","contentId":2,"role":"OT"},
      {"name":"C","contentId":3,"role":"H1"},
      {"name":"D","contentId":4,"role":"H2"},
      {"name":"E","contentId":5,"role":"M1"},
      {"name":"F","contentId":6,"role":"M2"},
      {"name":"G","contentId":7,"role":"R1"},
      {"name":"H","contentId":8,"role":"R2"}
    ]
    """;

    var roles = PartyRoleRoster.Parse(full);
    t.Eq("full party assigns all eight", roles.Count, 8);
    t.Eq("MT keyed on content id", roles[1], PartyRolesConfig.Assignment.MT);
    t.Eq("R2 keyed on content id", roles[8], PartyRolesConfig.Assignment.R2);

    // A member the producer could not place sends "", which is not a role.
    var partial = PartyRoleRoster.Parse("""[{"contentId":1,"role":"MT"},{"contentId":2,"role":""}]""");
    t.Eq("unplaceable member is skipped", partial.Count, 1);
    t.Eq("the placed one still resolves", partial[1], PartyRolesConfig.Assignment.MT);

    // Two members claiming one slot means the sides disagree; sending both to one tower is worse
    // than sending nobody, so the slot is dropped entirely.
    var clash = PartyRoleRoster.Parse("""[{"contentId":1,"role":"H1"},{"contentId":2,"role":"H1"},{"contentId":3,"role":"MT"}]""");
    t.True("a duplicated slot is dropped from both", !clash.ContainsKey(1) && !clash.ContainsKey(2));
    t.Eq("unaffected slots survive the clash", clash[3], PartyRolesConfig.Assignment.MT);

    // Junk must never take the party down: an absent Daedalus is the normal case.
    t.Eq("empty array parses to nothing", PartyRoleRoster.Parse("[]").Count, 0);
    t.Eq("non-array parses to nothing", PartyRoleRoster.Parse("{}").Count, 0);
    t.Eq("missing contentId is skipped", PartyRoleRoster.Parse("""[{"role":"MT"}]""").Count, 0);
    t.Eq("zero contentId is skipped", PartyRoleRoster.Parse("""[{"contentId":0,"role":"MT"}]""").Count, 0);
    t.Eq("unknown role word is skipped", PartyRoleRoster.Parse("""[{"contentId":1,"role":"Tank"}]""").Count, 0);

    // Older Daedalus builds predate the field entirely.
    t.Eq("entry without a role is skipped",
        PartyRoleRoster.Parse("""[{"contentId":1,"name":"A"}]""").Count, 0);
}


// ---------------------------------------------------------------------------
// Encounter config: group assignments and persistence
//
// These decide which half of the party a raid module sends where, so a wrong answer is not a cosmetic
// bug -- it walks four people into the mechanic. The validation rules are the part worth pinning: each
// flavour must reject an arrangement it cannot resolve rather than resolve it half way.
// ---------------------------------------------------------------------------
t.Section("Encounter config");
{
    // Unique: all eight spots taken exactly once
    var uniq = GroupAssignmentUnique.DefaultRoles();
    t.True("unique default is valid", uniq.Validate());
    uniq[A.R2] = uniq[A.R1]; // duplicate a spot, leaving one unfilled
    t.True("unique rejects a duplicated spot", !uniq.Validate());

    // Light parties: each role pair split one per party
    var lp = GroupAssignmentLightParties.DefaultLightParties();
    t.True("light parties default is valid", lp.Validate());
    t.Eq("light parties put MT in group 0", lp[A.MT], 0);
    t.Eq("light parties put OT in group 1", lp[A.OT], 1);
    lp[A.OT] = 0; // both tanks in one party
    t.True("light parties reject both tanks in one group", !lp.Validate());

    // DD/support pairs: every group gets exactly one support and one DD
    var pairs = GroupAssignmentDDSupportPairs.DefaultMeleeTogether();
    t.True("dd/support pairs default is valid", pairs.Validate());
    pairs[A.M2] = pairs[A.M1]; // two DD in one pair, none in another
    t.True("dd/support pairs reject two DD in one group", !pairs.Validate());

    // Resolving needs a fully-assigned party; a partial one yields nothing rather than half a plan
    var ws = new WorldState(10_000_000, "test");
    ws.Execute(new WorldState.OpFrameStart(Frame(ws, 0, 0f), TimeSpan.Zero));
    var roles = new PartyRolesConfig();
    var order = new[] { A.MT, A.OT, A.H1, A.H2, A.M1, A.M2, A.R1, A.R2 };
    for (var i = 0; i < 8; ++i)
    {
        ulong inst = 0x20000001u + (uint)i;
        ulong cid = 0xB0u + (uint)i;
        ws.Execute(new ActorState.OpCreate(inst, 1, 0, $"P{i}", 0, ActorType.Player, new Vector4(100, 0, 100, 0), 0.5f, default, true, true, 0));
        ws.Execute(new PartyState.OpModify(i, new PartyState.Member(cid, inst)));
        roles.Assignments[cid] = order[i];
    }
    var slots = roles.SlotsPerAssignment(ws.Party);
    t.Eq("SlotsPerAssignment covers all eight roles", slots.Length, 8);
    t.Eq("MT resolves to slot 0", slots[(int)A.MT], 0);
    t.Eq("R2 resolves to slot 7", slots[(int)A.R2], 7);

    var g1 = GroupAssignmentLightParties.DefaultLightParties().BuildGroupMask(0, ws.Party, roles);
    t.Eq("light party 1 has four members", g1.NumSetBits(), 4);
    t.True("light party 1 contains the MT slot", g1[0]);
    t.True("light party 1 excludes the OT slot", !g1[1]);

    roles.Assignments.Remove(0xB3u); // one member unassigned
    t.Eq("SlotsPerAssignment gives up on a partial party", roles.SlotsPerAssignment(ws.Party).Length, 0);
    t.Eq("an unresolvable group mask is empty",
        GroupAssignmentLightParties.DefaultLightParties().BuildGroupMask(0, ws.Party, roles).Raw, 0ul);

    // Round-trip: saved values must land back on the *same* node instance a module already holds
    var root = new ConfigRoot();
    var node = root.Get<PartyRolesConfig>();
    node.Assignments[0xFEED] = A.H2;
    var json = root.SaveToString();

    var reloaded = new ConfigRoot();
    var held = reloaded.Get<PartyRolesConfig>(); // as a module would, before the file is read
    reloaded.LoadFrom(json);
    t.Eq("loaded config lands on the instance modules already hold", (int)held[0xFEED], (int)A.H2);
    t.True("load leaves the node identity intact", ReferenceEquals(held, reloaded.Get<PartyRolesConfig>()));

    var survivor = new ConfigRoot();
    survivor.LoadFrom("{ not json at all ");
    t.NotNull("malformed config keeps defaults instead of throwing", survivor.Get<PartyRolesConfig>());
    survivor.LoadFrom("{\"Minerva.NoSuchConfigNode\": {\"X\": 1}}");
    t.NotNull("unknown config node is skipped", survivor.Get<PartyRolesConfig>());
}


// ---------------------------------------------------------------------------
// Positioning primitives added for the Ultimates
//
// These decide where the auto-dodge sends you, so the sign convention is the whole game: negative means
// forbidden. An inverted shape that got its sign backwards forbids exactly the safe spot.
// ---------------------------------------------------------------------------
t.Section("Ultimate positioning primitives");
{
    var c = new WPos(100f, 100f);

    // Half-plane: forbids the side the normal points at
    var half = new SDHalfPlane(c, new WDir(0f, 1f));
    t.True("half-plane forbids the normal side", half.Distance(c + new WDir(0f, 5f)) < 0f);
    t.True("half-plane allows the far side", half.Distance(c - new WDir(0f, 5f)) > 0f);
    t.Near("half-plane is zero on the line", half.Distance(c + new WDir(7f, 0f)), 0f);

    // Inverted union: you must stand in one of the listed shapes
    var union = new SDInvertedUnion([new SDCircle(c + new WDir(10f, 0f), 2f), new SDCircle(c - new WDir(10f, 0f), 2f)]);
    t.True("inverted union allows the first circle", union.Distance(c + new WDir(10f, 0f)) > 0f);
    t.True("inverted union allows the second circle", union.Distance(c - new WDir(10f, 0f)) > 0f);
    t.True("inverted union forbids the gap between them", union.Distance(c) < 0f);

    // Precise position: everything forbidden but one grid cell...
    var precise = new SDPrecisePosition(c, new WDir(0f, 1f), 0.5f, c + new WDir(9f, 0f), 0.1f);
    t.True("precise position allows the destination cell", precise.Distance(c) > 0f);
    t.True("precise position forbids a yalm away", precise.Distance(c + new WDir(1f, 0f)) < 0f);

    // ...unless you are already there, in which case it forbids nothing and stops tugging
    var satisfied = new SDPrecisePosition(c, new WDir(0f, 1f), 0.5f, c + new WDir(0.05f, 0f), 0.1f);
    t.True("precise position stands down once you are within tolerance", satisfied.Distance(c + new WDir(30f, 0f)) > 0f);

    // Inverted capsule: stay inside the swept corridor
    var corridor = new SDInvertedCapsule(c, new WDir(1f, 0f), 10f, 2f);
    t.True("inverted capsule allows a point along the corridor", corridor.Distance(c + new WDir(5f, 0f)) > 0f);
    t.True("inverted capsule forbids a point beside it", corridor.Distance(c + new WDir(5f, 5f)) < 0f);

    // Angle helpers: sign says which edge is nearer, so a module knows which way to turn
    var (min, max) = (30f.Degrees(), 60f.Degrees());
    t.Near("angle inside the range has zero distance", 45f.Degrees().DistanceToRange(min, max).Deg, 0f);
    t.True("angle below the range reports a positive distance", 10f.Degrees().DistanceToRange(min, max).Deg > 0f);
    t.True("angle above the range reports a negative distance", 80f.Degrees().DistanceToRange(min, max).Deg < 0f);
    t.Near("ClosestInRange snaps up to the near edge", 10f.Degrees().ClosestInRange(min, max).Deg, 30f);
    t.Near("ClosestInRange leaves an angle already inside alone", 45f.Degrees().ClosestInRange(min, max).Deg, 45f);

    // Ray/circle intersection, used for arena-edge and melee-ring spot picking. It returns the distance to
    // where the ray LEAVES the circle (the far root), not where it enters -- that is what a bounds cast
    // wants: "how far can I travel this way before I am clear".
    var hit = Intersect.RayCircle(c - new WDir(10f, 0f), new WDir(1f, 0f), c, 3f);
    t.Near("ray/circle returns the exit distance, not the entry", hit, 13f, 0.05f);
    t.True("ray pointing away misses", Intersect.RayCircle(c - new WDir(10f, 0f), new WDir(-1f, 0f), c, 3f) == float.MaxValue);

    // Enmity ordering is a guess, and the guess has to put the tank first
    t.Eq("BitMask folds with a shift", (new BitMask(0b1010_0000) >> 4).Raw, 0b1010ul);
    t.True("BitMask.Any is false when empty", !default(BitMask).Any());

    // Waymarks: unplaced reads as null rather than the origin
    var wm = new WaymarkState();
    t.True("unplaced waymark is null, not (0,0)", wm[Waymark.A] == null);
    t.True("no waymarks placed", !wm.AnyWaymarks);
    wm[Waymark.A] = new Vector3(1f, 2f, 3f);
    t.Near("placed waymark reads back", wm[Waymark.A]!.Value.Z, 3f);
    t.True("AnyWaymarks once one is placed", wm.AnyWaymarks);
    t.Eq("waymark round-trips through CompareToInitial", wm.CompareToInitial().Count, 1);
}


// ---------------------------------------------------------------------------
// Counter-rotating rings (Occult Crescent necromancer)
//
// Two concentric bands of ground AOE with a few openings, turning in opposite directions. They stop, and
// anything not in an opening dies outright. The property under test is that the component answers about
// the moment it RESOLVES, not the moment you are looking at it: an opening drawn where it is now walks you
// into a wall, and on an instant kill that is fatal advice.
// ---------------------------------------------------------------------------
t.Section("Counter-rotating rings");
{
    var ws = new WorldState(10_000_000, "test");
    ws.Execute(new WorldState.OpFrameStart(Frame(ws, 0, 0f), TimeSpan.Zero));
    var center = new WPos(100f, 100f);

    var comp = new CounterRotatingRings(new TestRingModule(ws, center));
    comp.Center = center;
    comp.ReferenceTime = ws.CurrentTime;
    comp.Resolve = ws.CurrentTime.AddSeconds(10d);

    // Inner band: one opening due north now, turning +9 deg/s -> +90 deg over the ten seconds to resolve.
    // Outer band: one opening due north now, turning the other way -> -90 deg.
    comp.Bands.Add(new(0f, 10f, [0f.Degrees()], 20f.Degrees(), 9f.Degrees()));
    comp.Bands.Add(new(10f, 20f, [0f.Degrees()], 20f.Degrees(), (-9f).Degrees()));
    comp.Update();

    t.Eq("one opening per band", comp.SafeSpots.Count, 2);
    t.Near("inner opening has turned +90 deg by resolve", comp.SafeSpots[0].Dir.Deg, 90f, 0.5f);
    t.Near("outer opening has turned -90 deg by resolve", comp.SafeSpots[1].Dir.Deg, -90f, 0.5f);

    // A single opening per band leaves a single (wrapping) wall arc per band
    t.Eq("one danger arc per band", comp.ActiveAOEs(0, null!).Length, 2);

    var aoes = comp.ActiveAOEs(0, null!).ToArray();
    bool Forbidden(WPos p)
    {
        foreach (var a in aoes)
            if (a.Shape.Check(p, a.Origin, a.Rotation))
                return true;
        return false;
    }

    // Where the inner opening STARTED (due north, 5y out) is a wall by the time it stops -- this is the
    // whole point of the component.
    t.True("the spot that looks safe now is lethal at resolve", Forbidden(center + new WDir(0f, 5f)));

    // Where it will BE is safe.
    t.True("the projected inner opening is safe", !Forbidden(center + (90f.Degrees().ToDirection() * 5f)));

    // ...and the outer band's opening has gone the other way, so the same bearing is lethal further out.
    t.True("counter-rotation puts the outer wall at the inner opening's bearing", Forbidden(center + (90f.Degrees().ToDirection() * 15f)));
    t.True("the projected outer opening is safe", !Forbidden(center + ((-90f).Degrees().ToDirection() * 15f)));

    // Outside every band is untouched: the rings do not claim ground they do not cover
    t.True("beyond the outer radius is not forbidden", !Forbidden(center + new WDir(0f, 30f)));

    // Nothing is claimed before the module says when it lands
    var idle = new CounterRotatingRings(new TestRingModule(ws, center));
    idle.Bands.Add(new(0f, 10f, [0f.Degrees()], 20f.Degrees(), 9f.Degrees()));
    idle.Update();
    t.Eq("no resolve time means nothing is drawn", idle.ActiveAOEs(0, null!).Length, 0);

    // A band whose openings overlap into a full circle forbids nothing rather than forbidding everything
    var open = new CounterRotatingRings(new TestRingModule(ws, center));
    open.Center = center;
    open.ReferenceTime = ws.CurrentTime;
    open.Resolve = ws.CurrentTime.AddSeconds(1d);
    open.Bands.Add(new(0f, 10f, [0f.Degrees(), 180f.Degrees()], 90f.Degrees(), default));
    open.Update();
    t.Eq("fully-open band forbids nothing", open.ActiveAOEs(0, null!).Length, 0);

    // ---- the centre is always lethal, and that is a trap for UPTIME, not for dodging ----
    //
    // The boss stands in the middle, so melee range is inside the kill circle. BossmodReborn's stick-to-
    // target range walks people in. Minerva must not: the core is published with no activation time, which
    // makes it forbidden *now* rather than when the rings stop, and the uptime pass skips forbidden cells.
    {
        var lethal = new CounterRotatingRings(new TestRingModule(ws, center));
        lethal.Center = center;
        lethal.ReferenceTime = ws.CurrentTime;
        lethal.Resolve = ws.CurrentTime.AddSeconds(10d);
        lethal.LethalCore = 6f;
        lethal.Bands.Add(new(6f, 20f, [0f.Degrees()], 20f.Degrees(), default));
        lethal.Update();

        var core = lethal.ActiveAOEs(0, null!).ToArray()[0];
        t.True("the lethal core is published with no activation, i.e. dangerous now", core.Activation == default);
        t.True("the centre itself is forbidden", core.Shape.Check(center, core.Origin, core.Rotation));

        // Feed it to the real solver, with a melee uptime goal on a boss standing dead centre.
        var hints = new AIHints { Center = center, Bounds = new ArenaBoundsCircle(25f) };
        hints.PlayerPosition = center + (90f.Degrees().ToDirection() * 12f);   // safe: in the opening
        foreach (var a in lethal.ActiveAOEs(0, null!))
            hints.AddForbiddenZone(a.ShapeDistance!, a.Activation);

        var boss = new Actor(2, 2, 0, "boss", 0, ActorType.Enemy, new Vector4(center.X, 0f, center.Z, 0f));
        var goal = UptimeGoal.For(boss, Role.Melee);
        t.True("melee range around a centred boss lies inside the kill circle", goal.Range < lethal.LethalCore);

        var solved = ArenaPathfinder.Solve(hints, ws.CurrentTime, goal: goal);
        var destination = solved.NeedToMove ? solved.Target : hints.PlayerPosition;
        t.True("uptime never walks into the lethal core", (destination - center).Length() > lethal.LethalCore);
        t.True("uptime leaves a safe player where they are rather than chasing unreachable range", !solved.NeedToMove);
    }

    // ---- Pallmagia's Roulette, with the real numbers off a recording ----
    //
    // CE204 Appalling Behavior. The boss sits 0.03y from the arena centre at every Roulette, its hitbox is
    // 3.504, and melee reach adds 2.6 -- so a melee uptime goal covers 0-6.1y from centre while the kill
    // circle is 5y. Four fifths of melee range is instant death. BossmodReborn declares that circle
    // dangerous only 18.3s ahead, which is why its AI holds position in it.
    {
        const float hitbox = 3.504f, killCircle = 5f;
        var arena = new WPos(807f, -562f);
        var boss = new Actor(3, 0x4D8F, 0, "Pallmagia", 0, ActorType.Enemy, new Vector4(arena.X, 61f, arena.Z + 0.03f, 0f), hitbox);
        var goal = UptimeGoal.For(boss, Role.Melee);

        t.Near("melee uptime range on Pallmagia", goal.Range, hitbox + 2.6f, 0.01f);
        t.True("most of melee range lies inside the kill circle", goal.Range > killCircle);
        t.True("but a survivable sliver of melee range does exist", goal.Range - killCircle > 0.5f);

        var hints = new AIHints { Center = arena, Bounds = new ArenaBoundsCircle(20f) };
        hints.PlayerPosition = arena + new WDir(0f, 2f);        // parked on the boss, inside the core

        // The core as the module now publishes it: no activation, i.e. dangerous for the whole window.
        var core = new AOEShapeCircle(killCircle);
        hints.AddForbiddenZone(core.Distance(arena, default), default);

        var solved = ArenaPathfinder.Solve(hints, ws.CurrentTime, goal: goal);
        t.True("a player parked in the kill circle is told to move", solved.NeedToMove);
        t.True("and the spot they are sent to is outside it", (solved.Target - arena).Length() > killCircle);
        t.True("while still holding melee range", (solved.Target - boss.Position).Length() <= goal.Range + 0.75f);

        // The same zone declared 18.3s out, as BossmodReborn does it: a 3s lookahead sees nothing to avoid.
        var bmrStyle = new AIHints { Center = arena, Bounds = new ArenaBoundsCircle(20f) };
        bmrStyle.PlayerPosition = arena + new WDir(0f, 2f);
        bmrStyle.AddForbiddenZone(core.Distance(arena, default), ws.CurrentTime.AddSeconds(18.3d));
        t.True("an 18.3s activation reads as safe to a 3s lookahead -- the bug this guards against",
            !bmrStyle.InImminentDanger(bmrStyle.PlayerPosition, ws.CurrentTime.AddSeconds(3d)));
    }

    // ---- Roulette's gaps really are the complement of its wedges ----
    //
    // The module derives the safe arcs from the danger wedges instead of listing them separately, so the two
    // cannot drift apart. This checks the derivation against the shapes themselves: a point in a drawn gap
    // must miss both wedges, and a point just outside it must be caught.
    {
        foreach (var (innerR, outerR, halfAngleDeg, offsetDeg) in
                 new[] { (12f, 20f, 67.5f, 22.5f), (5f, 12f, 60f, -60f) })
        {
            var shape = new AOEShapeDonutSector(innerR, outerR, halfAngleDeg.Degrees(), offsetDeg.Degrees());
            var rot = 33f.Degrees();                       // arbitrary ring facing
            var midRadius = (innerR + outerR) * 0.5f;
            var arena = new WPos(807f, -562f);

            var gapHalf = 90f.Degrees() - shape.HalfAngle;
            var gapCenter = rot + shape.DirectionOffset + 90f.Degrees();

            bool Hit(Angle bearing)
            {
                var p = arena + (bearing.ToDirection() * midRadius);
                return shape.Check(p, arena, rot) || shape.Check(p, arena, rot + 180f.Degrees());
            }

            var label = $"{halfAngleDeg * 2f:F0}-deg wedges";
            t.True($"{label}: the gap centre is safe", !Hit(gapCenter));
            t.True($"{label}: the opposite gap centre is safe", !Hit(gapCenter + 180f.Degrees()));
            t.True($"{label}: just inside the gap edge is still safe", !Hit(gapCenter + (gapHalf - 1f.Degrees())));
            t.True($"{label}: just past the gap edge is a wall", Hit(gapCenter + (gapHalf + 1f.Degrees())));
            t.Near($"{label}: gap width", gapHalf.Deg * 2f, 180f - (halfAngleDeg * 2f), 0.1f);
        }
    }

    // ---- EAnim state byte order ----
    //
    // The game sends an event-object animation as two 16-bit halves. BossmodReborn combines them
    // (p1 << 16) | p2, and every ported module's state constant is written against that; Minerva's sync had
    // them the other way round, so 48 modules compared against values that could never occur. Nothing
    // errors when the halves are swapped -- the mechanic just never draws.
    //
    // These are the real values off a Pallmagia recording: the Roulette rings report p1=0x0004 with
    // p2=0x0010 (counter-clockwise) or 0x0020 (clockwise), and CE204 tests for 0x00040010 / 0x00040020.
    {
        const uint recordedCCW = 0x00100004u, recordedCW = 0x00200004u;   // as a v1 log stored them
        uint Repair(uint s) => ((s & 0xFFFFu) << 16) | (s >> 16);

        t.Eq("a v1 CCW state repairs to the constant CE204 tests for", Repair(recordedCCW), 0x00040010u);
        t.Eq("a v1 CW state repairs to the constant CE204 tests for", Repair(recordedCW), 0x00040020u);
        t.Eq("repairing is its own inverse", Repair(Repair(recordedCW)), recordedCW);

        // and the current packing produces those constants directly from the two halves
        const ushort p1 = 0x0004, p2 = 0x0020;
        t.Eq("current sync packing matches BMR's ordering", ((uint)p1 << 16) | p2, 0x00040020u);
    }

    // Measured turn rate, as a module would take it from a spinning actor between two frames
    t.Near("angular velocity from two samples", CounterRotatingRings.MeasureAngularVelocity(0f.Degrees(), 30f.Degrees(), 2d).Deg, 15f, 0.1f);
    t.Near("measuring across the seam takes the short way", CounterRotatingRings.MeasureAngularVelocity(170f.Degrees(), (-170f).Degrees(), 1d).Deg, 20f, 0.1f);
}


// ---------------------------------------------------------------------------
// The dodge should give up as little range as safety allows
//
// Reported from play: dodging an AOE walked the character away from the boss and left them there. The cost
// is travel^2 + excess^2 * UptimeWeight, and at the old 0.6 a yard outside melee was worth less than a yard
// of walking, so the nearest safe cell won essentially always -- the uptime term only ever broke ties.
//
// That was over-cautious for a reason that does not hold: cells the character cannot reach before the AOE
// lands are already discarded by the time budget before anything is scored. Among cells that are both safe
// and reachable, travel is tidiness, not safety.
// ---------------------------------------------------------------------------
t.Section("Dodge keeps uptime");
{
    var c = new WPos(100f, 100f);
    var boss = new Actor(9, 9, 0, "boss", 0, ActorType.Enemy, new Vector4(c.X, 0f, c.Z, 0f), 3.5f);
    var goal = UptimeGoal.For(boss, Role.Melee);

    SafeSpot Dodge(WPos player, WPos dangerAt, float dangerRadius, float arena = 30f)
    {
        var h = new AIHints { Center = c, Bounds = new ArenaBoundsCircle(arena) };
        h.PlayerPosition = player;
        var shape = new AOEShapeCircle(dangerRadius);
        h.AddForbiddenZone(shape.Distance(dangerAt, default), default);
        return ArenaPathfinder.Solve(h, DateTime.Now, goal: goal);
    }

    // Out of melee already, with the direct approach blocked: it must not drift further out. Before the
    // weight was raised this went from 10.0y to 11.0y, walking 1.4y -- away from the boss.
    var start = new WPos(100f, 110f);
    var away = Dodge(start, new WPos(100f, 106f), 5f);
    var before = (start - boss.Position).Length();
    var after = (away.Target - boss.Position).Length();
    t.True("dodging does not push the character further from the boss", after < before);
    t.True("and it closes a meaningful part of the gap", before - after > 2f);

    // Already in melee: stepping out of a small AOE should stay in melee, not relocate.
    var near = new WPos(100f, 104f);
    var stepped = Dodge(near, near, 3f);
    t.True("a dodge from inside melee stays in melee", (stepped.Target - boss.Position).Length() <= goal.Range);
    t.True("and does not walk further than it must", (stepped.Target - near).Length() < 6f);

    // Far out: uptime is worth a real walk back, which is what a melee wants.
    var far = new WPos(100f, 125f);
    var returned = Dodge(far, far, 3f);
    t.True("from well out of range the dodge heads back toward the boss",
        (returned.Target - boss.Position).Length() < (far - boss.Position).Length() - 5f);

    // Safety still leads: it will not take an unsafe cell however much uptime it would buy.
    var hints = new AIHints { Center = c, Bounds = new ArenaBoundsCircle(30f) };
    hints.PlayerPosition = new WPos(100f, 115f);
    var core = new AOEShapeCircle(8f);                       // swallows the whole melee band
    hints.AddForbiddenZone(core.Distance(c, default), default);
    var solved = ArenaPathfinder.Solve(hints, DateTime.Now, goal: goal);
    var dest = solved.NeedToMove ? solved.Target : hints.PlayerPosition;
    t.True("uptime never buys an unsafe cell", (dest - c).Length() > 8f);
}


// ---------------------------------------------------------------------------
// Hint lines do not repeat
//
// Each component warns independently, so four overlapping AOEs produced four identical "GTFO from AOE!"
// lines above the arena. Seen in play; the same instruction four times crowds out everything else.
// ---------------------------------------------------------------------------
t.Section("Hints");
{
    var hints = new ModuleComponent.TextHints();
    hints.Add("GTFO from AOE!");
    hints.Add("GTFO from AOE!");
    hints.Add("GTFO from AOE!");
    t.Eq("an identical line is added once", hints.Count, 1);

    hints.Add("Stack!");
    t.Eq("a different line still gets through", hints.Count, 2);

    hints.Add("GTFO from AOE!", risk: false);
    t.Eq("the same text at a different risk does not duplicate it", hints.Count, 2);
    t.True("and the first, more urgent one is what survives", hints[0].risk);
}

// ---------------------------------------------------------------------------
// Module logic survives with no renderer
//
// Arena is the radar's drawing surface: the renderer assigns it during UiBuilder.Draw, and it is null
// on every game tick before that -- which, now the radar is detached and usually closed, is most of
// them. Everything below runs on the tick, so none of it may route through Arena.
//
// This is not hypothetical. A live NullReferenceException came out of a module assigning Arena.Bounds
// from OnMapEffect; the same fault was then found in 45 more places reaching InBounds through the
// renderer, in AddHints, CheckPull and the dodge path. Both now go through the module, and the
// geometry is identical either way -- the renderer only ever held a per-frame copy of it.
// ---------------------------------------------------------------------------
t.Section("No renderer attached");
{
    var ws = new WorldState(10_000_000, "test");
    ws.Execute(new WorldState.OpFrameStart(Frame(ws, 0), TimeSpan.Zero));
    const ulong boss = 0x4000000AA;
    ws.Execute(new ActorState.OpCreate(boss, 0x1234, 0, "Boss", 0, ActorType.Enemy, new Vector4(100, 0, 100, 0), 5f, default, true, false, 0));

    // deliberately NOT setting Arena -- this is the state every module is in during the sync tick
    var module = new TestModule(ws, ws.Actors.Find(boss)!);
    module.BuildStates();
    t.True("no renderer is attached", module.Arena == null);

    t.True("a point inside the arena reads as in bounds", module.InBounds(new WPos(100f, 100f)));
    t.True("and one outside does not", !module.InBounds(new WPos(100f, 200f)));
    t.True("a ray to the edge measures the square's half-width", Math.Abs(module.IntersectRayBounds(module.Center, new WDir(0f, 1f)) - 20f) < 0.01f);
    t.True("clamping pulls an outside point back in", module.InBounds(module.ClampToBounds(new WPos(100f, 200f))));
    t.True("and leaves an inside point alone", module.ClampToBounds(new WPos(100f, 105f)) == new WPos(100f, 105f));

    // the crash itself: a mid-fight arena swap arriving on a tick the radar is not drawing
    var before = module.Bounds;
    module.Bounds = new ArenaBoundsCircle(15f);
    t.True("an arena change lands on the module, not the renderer", !ReferenceEquals(module.Bounds, before));
    t.True("and the new bounds are what the dodge engine will read", module.InBounds(new WPos(100f, 110f)) && !module.InBounds(new WPos(100f, 120f)));

    // hints and AI hints both run on the tick
    var player = new Actor(0x1, 1, 0, "P", 0, ActorType.Player, new Vector4(100, 0, 100, 0));
    var hints = new ModuleComponent.TextHints();
    module.AddHints(0, player, hints);
    var ai = new AIHints();
    module.BuildAIHints(0, player, ai);
    t.True("AI hints inherit the module's live geometry", ReferenceEquals(ai.Bounds, module.Bounds) && ai.Center == module.Center);

    module.Dispose();
}

// ---------------------------------------------------------------------------
// Clearance lead
//
// Being clear exactly AS a mechanic resolves is not being safe. Measured in Double Trouble: the one
// charge that connected was left 0.39s before it landed, and the tightest survivor was 0.53s -- the
// boundary sat under half a second, inside server latency. The lead solves against a clock shifted
// forward, so a zone counts as imminent sooner and the budget to escape it shrinks by the same amount.
// ---------------------------------------------------------------------------
t.Section("Clearance lead");
{
    var c = new WPos(100f, 100f);
    static AIHints Zone(WPos c, DateTime at)
    {
        var h = new AIHints { Center = c, Bounds = new ArenaBoundsCircle(20f), PlayerPosition = c };
        h.AddForbiddenZone(new AOEShapeCircle(8f), c, default, at);
        return h;
    }

    var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // a hit 4s out, with a 3s horizon: not yet imminent without a lead...
    t.True("without a lead, a 4s-away AOE is not yet urgent",
        !ArenaPathfinder.Solve(Zone(c, now.AddSeconds(4)), now, horizonSeconds: 3f).NeedToMove);
    // ...and urgent with a 2s lead, because the solve runs 2s into the future
    t.True("a 2s lead makes the same AOE urgent now",
        ArenaPathfinder.Solve(Zone(c, now.AddSeconds(4)), now, horizonSeconds: 3f, clearanceLead: 2f).NeedToMove);

    // the lead also tightens the travel budget: same zone, but no time left to cross it
    var tight = ArenaPathfinder.Solve(Zone(c, now.AddSeconds(2)), now, horizonSeconds: 5f, clearanceLead: 1.9f);
    t.True("with almost no budget left the solve still commits to moving", tight.NeedToMove);

    // a lead of zero is exactly the old behaviour
    var plain = ArenaPathfinder.Solve(Zone(c, now.AddSeconds(2)), now, horizonSeconds: 5f);
    var zero = ArenaPathfinder.Solve(Zone(c, now.AddSeconds(2)), now, horizonSeconds: 5f, clearanceLead: 0f);
    t.True("lead 0 changes nothing", plain.NeedToMove == zero.NeedToMove && plain.Target == zero.Target);

    // and it never sends anyone into the zone it is avoiding
    var led = ArenaPathfinder.Solve(Zone(c, now.AddSeconds(3)), now, horizonSeconds: 5f, clearanceLead: 1.5f);
    t.True("the spot it picks is outside the AOE", !led.Found || (led.Target - c).Length() > 8f);
}

// ---------------------------------------------------------------------------
// The route, not just the destination
//
// A navmesh follower walks the points it is handed and does no pathfinding of its own. Handing it only
// the destination makes it take the straight chord -- through whatever the dodge was bending around.
// Seen in Dark Artistry: the player was clear at 8.79y from an 8y circle and was walked inward to 7.26y
// at full run speed, still inside when it resolved.
//
// The setup boxes the player in: standing in danger, ringed by more of it, with one gap. Escaping means
// going through the gap, so the straight line to safety is guaranteed to be unsafe.
// ---------------------------------------------------------------------------
t.Section("Dodge route");
{
    var c = new WPos(100f, 100f);
    var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    var hints = new AIHints { Center = c, Bounds = new ArenaBoundsCircle(30f), PlayerPosition = c };

    hints.AddForbiddenZone(new AOEShapeCircle(6.5f), c, default, now);          // the ground underfoot
    for (var deg = 0; deg < 360; deg += 20)                                     // a ring with one gap
    {
        if (deg is >= 60 and <= 100)
            continue;
        var a = deg * MathF.PI / 180f;
        hints.AddForbiddenZone(new AOEShapeCircle(4f), c + new WDir(MathF.Sin(a) * 9f, MathF.Cos(a) * 9f), default, now);
    }

    var spot = ArenaPathfinder.Solve(hints, now, horizonSeconds: 5f, safetyMargin: 0f, cellSize: 0.5f);
    t.True("boxed in, it decides to move", spot.NeedToMove && spot.Found);
    t.True("a route was produced", spot.Route is { Count: > 0 });

    if (spot.Route is { Count: > 0 } route)
    {
        static bool CrossesDanger(AIHints h, WPos a, WPos b)
        {
            var d = b - a;
            var steps = Math.Max(1, (int)MathF.Ceiling(d.Length() / 0.25f));
            for (var s = 0; s <= steps; ++s)
            {
                var pt = a + (d * ((float)s / steps));
                foreach (var z in h.ForbiddenZones)
                    if (z.Contains(pt))
                        return true;
            }
            return false;
        }

        // every leg except the first (which starts on the dangerous ground underfoot) must stay clear
        var clear = true;
        for (var i = 1; i < route.Count; ++i)
            if (CrossesDanger(hints, route[i - 1], route[i]))
                clear = false;
        t.True("no later leg of the route passes back through danger", clear);
        t.True("the route ends at the chosen destination", route[^1].AlmostEqual(spot.Target, 1f));
        // without this the rest proves nothing: the destination has to be somewhere the old
        // single-point handoff would have walked straight through danger to reach
        t.True("and the straight line to it would NOT have been safe", CrossesDanger(hints, c, spot.Target));
    }
}

// ---------------------------------------------------------------------------
// Pending effects
//
// The server announces a resolved action before the client applies it. In that window a healer can
// already know a raidwide has landed and for how much — which is the whole value, since acting on it
// after the HP bar moves is acting late.
//
// Pruned by expiry rather than by confirmation: some effects never confirm at all (overkill damage, a
// heal into a full bar, a reapplied buff), so a list waiting for confirmation would leak instead of
// settling.
// ---------------------------------------------------------------------------
t.Section("Pending effects");
{
    var ws = new WorldState(10_000_000, "test");
    ws.Execute(new WorldState.OpFrameStart(Frame(ws, 0), TimeSpan.Zero));
    const ulong boss = 0x40000BB1, player = 0x10000BB1;
    ws.Execute(new ActorState.OpCreate(boss, 0x1234, 0, "Boss", 0, ActorType.Enemy, new Vector4(0, 0, 0, 0), 5f, default, true, false, 0));
    ws.Execute(new ActorState.OpCreate(player, 0, 1, "P", 0, ActorType.Player, new Vector4(0, 0, 10, 0), 0.5f, default, true, true, 0));
    ws.Execute(new ActorState.OpHPMP(player, new ActorHPMP(100000, 100000, 0, 10000, 10000)));

    // one damage effect of 40000 aimed at the player: type 3 in byte 0, value in the top ushort
    var dmg = (ulong)ActionEffectType.Damage | ((ulong)40000 << 48);
    var ev = new ActorCastEvent(ActionID.MakeSpell(1000u), player, default, default, 42u);
    ev.Targets.Add(new ActorCastEvent.Target(player, [dmg, 0, 0, 0, 0, 0, 0, 0]));
    ws.Execute(new ActorState.OpCastEvent(boss, ev));

    var pc = ws.Actors.Find(player)!;
    t.Eq("the announced damage is pending", pc.PendingHPDifference, -40000);
    t.Eq("HP after it lands is known now", pc.PendingHPRaw, 60000);
    t.True("the HP bar has NOT moved yet", pc.HPMP.CurHP == 100000);
    t.True("and it does not read as lethal", !pc.PendingDead);

    // a second hit that would kill
    var lethal = (ulong)ActionEffectType.Damage | ((ulong)65000 << 48);
    var ev2 = new ActorCastEvent(ActionID.MakeSpell(1001u), player, default, default, 43u);
    ev2.Targets.Add(new ActorCastEvent.Target(player, [lethal, 0, 0, 0, 0, 0, 0, 0]));
    ws.Execute(new ActorState.OpCastEvent(boss, ev2));
    t.True("two hits stack into a lethal total", pc.PendingDead);

    // a status application
    var status = (ulong)ActionEffectType.ApplyStatusEffectTarget | ((ulong)1234 << 48);
    var ev3 = new ActorCastEvent(ActionID.MakeSpell(1002u), player, default, default, 44u);
    ev3.Targets.Add(new ActorCastEvent.Target(player, [status, 0, 0, 0, 0, 0, 0, 0]));
    ws.Execute(new ActorState.OpCastEvent(boss, ev3));
    t.True("a status application is pending too", pc.PendingStatuses.Count == 1 && pc.PendingStatuses[0].StatusId == 1234u);

    // nothing confirms; everything expires rather than leaking
    ws.Execute(new WorldState.OpFrameStart(Frame(ws, 1, 4f), TimeSpan.Zero));
    t.Eq("unconfirmed damage expires", pc.PendingHPDifference, 0);
    t.Eq("unconfirmed statuses expire", pc.PendingStatuses.Count, 0);
    t.True("and HP reads as the real value again", pc.PendingHPRaw == 100000);
}


// ---- Timeline: NextTransitionWithFlag ----
//
// Built from the module author's DECLARED durations, anchored at the cast that last moved the pointer.
// BossmodReborn does the same, so a ported component gets the answer it was written against -- including
// DateTime.MaxValue for "I cannot say", which is part of the contract rather than a stub: a fight that has
// gone off-script must not invent a time.
{
    var tlWs = new WorldState(1000UL, "test");
    const ulong tlBossId = 0x1000UL;
    tlWs.Execute(new WorldState.OpFrameStart(Frame(tlWs, 0), TimeSpan.Zero));
    tlWs.Execute(new ActorState.OpCreate(tlBossId, 0x9000u, 1, "boss", 0, ActorType.Enemy,
        new Vector4(100, 0, 100, 0), 5f, new ActorHPMP(1000, 1000, 0, 0, 0), true, false, 0));
    var tlBoss = tlWs.Actors.Find(tlBossId)!;
    var tlM = new TimelineModule(tlWs, tlBoss);
    tlM.BuildStates();   // states are built explicitly, not by Update
    tlM.Update();

    t.True("no cast seen yet -> no active state", tlM.StateMachine.ActiveState == null);
    t.True("and no answer, rather than a guess",
        tlM.StateMachine.NextTransitionWithFlag(StateMachine.StateHint.Raidwide) == DateTime.MaxValue);

    var tl0 = tlWs.CurrentTime;
    tlWs.Execute(new ActorState.OpCastInfo(tlBossId, new ActorCastInfo { Action = ActionID.MakeSpell(7001u), TotalTime = 5f }));
    t.True("the opener's cast moves the pointer onto its state", tlM.StateMachine.ActiveState?.Name == "opener");

    // opener 5 + buster 7 + wide 8 = 20s from the transition
    var tlWide = tlM.StateMachine.NextTransitionWithFlag(StateMachine.StateHint.Raidwide);
    t.Near("raidwide is the sum of declared durations up to and including it",
        (float)(tlWide - tl0).TotalSeconds, 20f, 0.2f);

    // the tankbuster is nearer: the walk must stop at the FIRST match, not the last
    var tlBuster = tlM.StateMachine.NextTransitionWithFlag(StateMachine.StateHint.Tankbuster);
    t.Near("tankbuster is the nearer flag", (float)(tlBuster - tl0).TotalSeconds, 12f, 0.2f);
    t.True("and it lands before the raidwide", tlBuster < tlWide);

    t.True("a flag nothing carries is unknown, not zero",
        tlM.StateMachine.NextTransitionWithFlag(StateMachine.StateHint.Knockback) == DateTime.MaxValue);

    tlWs.Execute(new ActorState.OpCastInfo(tlBossId, null));
    tlWs.Execute(new ActorState.OpCastInfo(tlBossId, new ActorCastInfo { Action = ActionID.MakeSpell(7002u), TotalTime = 4f }));
    t.True("the next cast moves it again", tlM.StateMachine.ActiveState?.Name == "buster");
    t.True("and the raidwide is nearer than it was",
        tlM.StateMachine.NextTransitionWithFlag(StateMachine.StateHint.Raidwide) < tlWide);
}

// ---------------------------------------------------------------------------
// Arena clipping: the radar cuts AOE fills off at the field edge by geometry (cone contour, intersected
// with the field polygon, triangulated). This is the exact sequence ImGuiArena.FillClipped runs, minus
// the draw calls -- so what is asserted here is what the radar paints.
// ---------------------------------------------------------------------------
t.Section("Arena clipping");
{
    var center = new WPos(100, 100);
    var clipper = new PolygonClipper();

    static WDir[] Offsets(IReadOnlyList<WPos> loop, WPos center)
    {
        var r = new WDir[loop.Count];
        for (var i = 0; i < loop.Count; ++i)
            r[i] = loop[i] - center;
        return r;
    }

    static float Area(RelTriangle[] tris)
    {
        var sum = 0f;
        foreach (var tri in tris)
            sum += MathF.Abs(((tri.B.X - tri.A.X) * (tri.C.Z - tri.A.Z)) - ((tri.B.Z - tri.A.Z) * (tri.C.X - tri.A.X))) * 0.5f;
        return sum;
    }

    static float Reach(RelTriangle[] tris)
    {
        var max = 0f;
        foreach (var tri in tris)
            max = MathF.Max(max, MathF.Max(tri.A.Length(), MathF.Max(tri.B.Length(), tri.C.Length())));
        return max;
    }

    // A Rotten Affair's Glory: a 50y, 90-degree cone on a 25y FATE ring
    var field = new PolygonClipper.Operand(clipper.Simplify(new PolygonClipper.Operand(Offsets(new ArenaBoundsCircle(25f).Contour(center), center))));
    var cone = new PolygonClipper.Operand();
    foreach (var loop in new AOEShapeCone(50f, 45f.Degrees()).Contours(center, default))
        cone.AddContour(Offsets(loop, center));
    var clippedCone = clipper.Intersect(cone, field);
    var coneTris = clippedCone.Triangulate();
    t.Eq("cone clipped to one piece", clippedCone.Parts.Count, 1);
    t.True("cone clip produced triangles", coneTris.Length > 0);
    t.True("nothing reaches past the ring", Reach(coneTris) <= 25.05f);
    t.Near("clipped cone is a quarter of the ring", Area(coneTris), MathF.PI * 25f * 25f / 4f, 12f);

    // Lost on the Wind's Tendon Ripper: a 60y plus, 8 wide, on a 24y ring -- the first concave shape to go
    // through the clip-and-triangulate path. A wedge-shaped fill on the radar means the triangulator bridged
    // the notches, so beyond area and reach every triangle's centre must itself lie inside the cross.
    static float GridArea(Func<WPos, bool> inside, WPos center, float half)
    {
        const float step = 0.25f;
        var n = 0;
        for (var x = -half; x <= half; x += step)
            for (var z = -half; z <= half; z += step)
                if (inside(center + new WDir(x, z)))
                    n++;
        return n * step * step;
    }
    var ringField = new PolygonClipper.Operand(clipper.Simplify(new PolygonClipper.Operand(Offsets(new ArenaBoundsCircle(24f).Contour(center), center))));
    foreach (var (origin, rot, label) in new[] { (center, default(Angle), "centred"), (center + new WDir(-8f, 4f), -135f.Degrees(), "off-centre, rotated") })
    {
        var cross = new AOEShapeCross(60f, 4f);
        var subject = new PolygonClipper.Operand();
        foreach (var loop in cross.Contours(origin, rot))
            subject.AddContour(Offsets(loop, center));
        var clippedCross = clipper.Intersect(subject, ringField);
        var crossTris = clippedCross.Triangulate();
        t.True($"cross ({label}) clip produced triangles", crossTris.Length > 0);
        t.True($"cross ({label}) stays inside the ring", Reach(crossTris) <= 24.05f);
        var expected = GridArea(q => cross.Check(q, origin, rot) && q.InCircle(center, 24f), center, 24f);
        t.Near($"cross ({label}) fill area matches the shape", Area(crossTris), expected, expected * 0.03f);
        var outside = 0;
        foreach (var tri in crossTris)
        {
            var c = center + (tri.A + tri.B + tri.C) * (1f / 3f);
            if (!cross.Check(c, origin, rot))
                outside++;
        }
        t.Eq($"cross ({label}): no triangle lies outside the cross", outside, 0);
    }

    // a 10-25y donut on a 20y ring: the donut contour is one loop with a seam, and the hole must survive
    var donut = new PolygonClipper.Operand();
    foreach (var loop in new AOEShapeDonut(10f, 25f).Contours(center, default))
        donut.AddContour(Offsets(loop, center));
    var ring = new PolygonClipper.Operand(clipper.Simplify(new PolygonClipper.Operand(Offsets(new ArenaBoundsCircle(20f).Contour(center), center))));
    var clippedDonut = clipper.Intersect(donut, ring);
    var donutTris = clippedDonut.Triangulate();
    t.True("donut clip produced triangles", donutTris.Length > 0);
    t.True("donut clip stays inside the ring", Reach(donutTris) <= 20.05f);
    t.Near("donut hole survives the clip", Area(donutTris), MathF.PI * ((20f * 20f) - (10f * 10f)), 30f);
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


sealed class TimelineModule(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(100f, 100f), new ArenaBoundsSquare(20f));

// A linear stretch with a tankbuster and a raidwide behind it -- the shape NextTransitionWithFlag reasons about.
sealed class TimelineModuleStates : StateMachineBuilder
{
    public TimelineModuleStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase();
        this.Cast(0x100u, 7001u, 0f, 5f, "opener");        // duration 5, trigger 7001
        this.Cast(0x200u, 7002u, 3f, 4f, "buster")
            .SetHint(StateMachine.StateHint.Tankbuster);   // duration 7
        this.Cast(0x300u, 7003u, 2f, 6f, "wide")
            .SetHint(StateMachine.StateHint.Raidwide);     // duration 8
    }
}


sealed class TestModule(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(100f, 100f), new ArenaBoundsSquare(20f));

// exposes StayMove's protected SetState so the test can drive a requirement directly
// a registrable module for the offline-validation round trip (CFC 9001, boss OID 0xBEEF)
[ModuleInfo(CFCID = 9001u, PrimaryActorOID = 0xBEEFu, NameID = 1u)]
public sealed class ValidateProbeModule(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(100f, 100f), new ArenaBoundsSquare(20f));

sealed class ValidateProbeModuleStates : StateMachineBuilder
{
    public ValidateProbeModuleStates(ModuleBase module) : base(module)
        => this.TrivialPhase().ActivateOnEnter<ProbeCovered>();
}

// a quest solo-duty module, keyed the way the MSQ ports are: the quest id in CFCID
[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 70001u, PrimaryActorOID = 0xBEEFu, NameID = 1u)]
public sealed class QuestProbeModule(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(0f, 0f), new ArenaBoundsCircle(20f));

sealed class QuestProbeModuleStates : StateMachineBuilder
{
    public QuestProbeModuleStates(ModuleBase module) : base(module) => this.TrivialPhase();
}

sealed class ProbeCovered(ModuleBase module) : Minerva.Components.SimpleAOEs(module, 4242u, new AOEShapeCircle(6f));

sealed class StayMoveProbe(ModuleBase module) : Minerva.Components.StayMove(module)
{
    public void Set(int slot, Requirement req, DateTime activation) => this.SetState(slot, new PlayerState(req, activation));
}

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
sealed class TestGaze(ModuleBase module, WPos eye, bool inverted) : Minerva.Components.GenericGaze(module)
{
    private readonly Eye[] eyes = [new(eye, default, default, 10000f, inverted)];
    public override ReadOnlySpan<Eye> ActiveEyes(int slot, Actor actor) => this.eyes;
}

sealed class TestLineOfSight(ModuleBase module, uint aid, Actor[] blockers) : Minerva.Components.CastLineOfSightAOE(module, aid, 30f)
{
    private readonly Actor[] blockers = blockers;
    public override ReadOnlySpan<Actor> BlockerActors() => this.blockers;
}

/// <summary>Minimal concrete line-of-sight component, so the base class's obstacle rule can be tested
/// without standing up a whole boss module.</summary>
sealed class LoSProbe(uint aid, bool blockersImpassable, bool safeInsideHitbox)
    : Minerva.Components.GenericLineOfSightAOE(null!, aid, 30f, blockersImpassable, false, safeInsideHitbox);

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


// Minimal module for exercising components that only need a centre and a world.
sealed class TestRingModule(WorldState ws, WPos center)
    : ModuleBase(ws, new Actor(1, 1, 0, "ring", 0, ActorType.Enemy, new Vector4(center.X, 0f, center.Z, 0f)), center, new ArenaBoundsCircle(25f));

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
