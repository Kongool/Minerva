using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Minerva;
using Minerva.Generation;
using Minerva.Validate;

// Offline replay validator. Runs the same coverage check as /mine -> Replay -> Validate, but from the
// command line over a whole folder of recordings, so module work can be reviewed without the game
// running. Exits non-zero when any fight has uncovered helper casts, so it can gate a build.
//
//   minerva-validate <recording.log | folder> [--modules <Minerva.dll>] [--quiet] [--strict]
//
// The boss modules live in the plugin assembly (Minerva.dll), which is loaded by path rather than
// referenced — the Dalamud-dependent types in it fail to load harmlessly, and ModuleRegistry keeps the
// module types that do load.

var args_ = Environment.GetCommandLineArgs().Skip(1).ToArray();
if (args_.Length == 0 || args_.Contains("--help") || args_.Contains("-h"))
{
    Console.WriteLine("""
        usage: minerva-validate <recording.log | folder> [options]

          --modules <path>   Minerva.dll containing the boss modules
                             (default: ./Minerva.dll, else ../../../../bin/x64/Minerva.dll)
          --quiet            one summary line per recording instead of the full report
          --strict           also fail on uncovered boss-visual casts, not just helper casts

        minerva-validate --compare-bmr <recording.log> [--html <out.html>]
          run Minerva's and BMR's module over one recording and report AOE disagreements;
          --html also writes the side-by-side viewer (dual-viewer phases 2-3)

        minerva-validate --generate <recording.log | folder> [--game <sqpack>] [--out <dir>]
          write a module draft per recording, reading shapes and names from the game's own sheets.
          --game defaults to the usual install locations; --out defaults beside each recording

        minerva-validate --bmr-probe [--bmr <BossModReborn.dll>] [--dalamud <dir>] [--oid 0x4612]
          load BossmodReborn out of game and construct a module from it (dual-viewer phase 0)
        """);
    return 0;
}

// --- phase 0 of the dual viewer: prove BMR's module logic constructs with no game running ---
if (args_[0] == "--bmr-probe")
{
    return BmrProbe(ArgValue("--bmr"), ArgValue("--dalamud"), ArgValue("--oid"), ArgValue("--game"));
}

if (args_[0] == "--generate")
{
    return Generate(args_.Length > 1 ? args_[1] : null, ArgValue("--game"), ArgValue("--out"));
}

if (args_[0] == "--compare-bmr")
{
    return CompareBmr(args_.Length > 1 ? args_[1] : null, ArgValue("--modules") ?? FindPluginAssembly(),
        ArgValue("--bmr"), ArgValue("--dalamud"), ArgValue("--game"));
}

if (args_[0] == "--bmr-bridge")
{
    return BmrBridgeRun(args_.Length > 1 ? args_[1] : null, ArgValue("--bmr"), ArgValue("--dalamud"), ArgValue("--game"), ArgValue("--oid"));
}

var target = args_[0];
var quiet = args_.Contains("--quiet");
var strict = args_.Contains("--strict");
var modulesPath = ArgValue("--modules") ?? FindPluginAssembly();

if (modulesPath == null || !File.Exists(modulesPath))
{
    Console.Error.WriteLine($"error: could not find the plugin assembly with the boss modules{(modulesPath != null ? $" at {modulesPath}" : "")}.");
    Console.Error.WriteLine("       build the solution first, or pass --modules <path to Minerva.dll>.");
    return 2;
}

ModuleRegistry registry;
try
{
    var asm = Assembly.LoadFrom(Path.GetFullPath(modulesPath));
    registry = ModuleRegistry.Build(asm, typeof(ModuleRegistry).Assembly);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"error: could not load modules from {modulesPath}: {ex.Message}");
    return 2;
}

if (registry.Count == 0)
{
    Console.Error.WriteLine($"error: {modulesPath} contained no boss modules.");
    return 2;
}

var files = Directory.Exists(target)
    ? Directory.GetFiles(target, "*.log", SearchOption.AllDirectories).OrderBy(f => f).ToArray()
    : File.Exists(target) ? [target] : [];

if (files.Length == 0)
{
    Console.Error.WriteLine($"error: no recordings found at {target} (expected .log files).");
    return 2;
}

Console.WriteLine($"{registry.Count} modules loaded from {Path.GetFileName(modulesPath)}; {files.Length} recording(s) to check\n");

var failures = 0;
var checkedCount = 0;
foreach (var file in files)
{
    ReplayValidator.Result result;
    try
    {
        using var reader = new StreamReader(file);
        var timeline = ReplayParser.ParseTimeline(reader);
        result = ReplayValidator.Validate(timeline, registry);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"!! {Path.GetFileName(file)}: could not read ({ex.Message})");
        failures++;
        continue;
    }

    checkedCount++;
    var bad = result.UncoveredMechanics.Count + (strict ? result.UncoveredVisuals.Count : 0);
    if (bad > 0)
        failures++;

    if (quiet)
    {
        var flag = bad > 0 ? "FAIL" : "ok  ";
        Console.WriteLine($"{flag} {Path.GetFileName(file),-52} drawn {result.Drawn.Count,3}  hinted {result.Hinted.Count,3}  uncovered {result.Uncovered,3}");
    }
    else
    {
        Console.WriteLine($"=== {Path.GetFileName(file)} ===");
        Console.WriteLine(result.Render());
        Console.WriteLine();
    }
}

Console.WriteLine($"\n{checkedCount} recording(s) validated, {failures} with uncovered {(strict ? "casts" : "helper casts")}.");
return failures > 0 ? 1 : 0;

// Loads BossmodReborn out of game and constructs a module from it. This is the milestone for phase 0
// in docs/dual-viewer-plan.md — everything downstream (op bridge, differ, viewer) depends on it working.
int BmrProbe(string? bmrPath, string? dalamudDir, string? oidArg, string? gameDir)
{
    BmrHost host;
    try
    {
        host = BmrHost.Load(bmrPath, dalamudDir, gameDir);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"error: could not load BossmodReborn headlessly: {ex.Message}");
        if (ex.InnerException != null)
            Console.Error.WriteLine($"       inner: {ex.InnerException.Message}");
        return 2;
    }

    Console.WriteLine($"loaded  {host.Location}");
    Console.WriteLine($"modules {host.ModuleCount} registered by primary-actor OID");

    var oids = host.RegisteredOIDs();
    // default probe set: whatever OIDs Minerva's own Dawntrail dungeon modules use, so a success here
    // means the modules we actually want to compare against are constructible
    var probes = oidArg != null
        ? [ParseOID(oidArg)]
        : new uint[] { 0x4612u, 0x41D3u, 0x421Fu, 0x4841u }.Where(oids.Contains).ToArray();

    if (probes.Length == 0)
    {
        Console.WriteLine("no probe OIDs matched BMR's registry; pass --oid 0x…");
        return 1;
    }

    var failed = 0;
    foreach (var oid in probes)
    {
        var info = host.InfoForOID(oid);
        try
        {
            var module = host.CreateModuleForOID(oid);
            if (module == null)
            {
                Console.WriteLine($"  0x{oid:X4}  no module registered");
                failed++;
                continue;
            }
            // StateMachine and Components are public *fields* on BMR's BossModule, not properties
            var mt = module.GetType();
            var sm = mt.GetField("StateMachine")?.GetValue(module);
            var comps = mt.GetField("Components")?.GetValue(module) as System.Collections.ICollection;
            // Components stay empty here by design: they are added when the state machine enters a phase,
            // and CreateModuleForTimeline's throwaway WorldState has a zero clock so no phase is ever
            // entered. Driving that needs real ops on a real clock — that is phase 1 (the op bridge).
            Console.WriteLine($"  0x{oid:X4}  OK  {mt.Name,-28} cfc {info?.GroupID,-6} " +
                              $"{(sm != null ? "state machine built" : "NO state machine")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  0x{oid:X4}  FAILED  {info?.Type ?? "?"}");
            for (var e = ex; e != null; e = e.InnerException)
                Console.WriteLine($"          {e.GetType().Name}: {e.Message}");
            failed++;
        }
    }

    Console.WriteLine(failed == 0
        ? $"{Environment.NewLine}phase 0 OK — {probes.Length} BMR module(s) constructed headlessly."
        : $"{Environment.NewLine}{failed} of {probes.Length} probe(s) failed.");
    return failed == 0 ? 0 : 1;

    static uint ParseOID(string s) => s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
        ? Convert.ToUInt32(s[2..], 16)
        : uint.Parse(s);
}

// Phase 1: replay a Minerva recording into a BMR WorldState and tick BMR's module alongside.
int BmrBridgeRun(string? recording, string? bmrPath, string? dalamudDir, string? gameDir, string? oidArg)
{
    if (recording == null || !File.Exists(recording))
    {
        Console.Error.WriteLine("usage: minerva-validate --bmr-bridge <recording.log> [--oid 0x…]");
        return 2;
    }

    BmrHost host;
    try { host = BmrHost.Load(bmrPath, dalamudDir, gameDir); }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"error: could not load BossmodReborn: {ex.Message}");
        return 2;
    }

    ReplayTimeline timeline;
    using (var reader = new StreamReader(recording))
        timeline = ReplayParser.ParseTimeline(reader);

    uint? forceOID = oidArg == null ? null
        : oidArg.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? Convert.ToUInt32(oidArg[2..], 16) : uint.Parse(oidArg);

    BmrDriver.Diagnose = args_.Contains("--verbose");
    var r = BmrDriver.Run(timeline, host, forceOID);

    Console.WriteLine($"recording  {Path.GetFileName(recording)}");
    Console.WriteLine($"ops        {r.OpsApplied} applied of {r.OpsRead} read, {r.Frames} frames");
    Console.WriteLine($"bmr module {r.ModuleName ?? "(none created — no BMR module for any actor OID in this recording)"}");
    Console.WriteLine($"components {r.ComponentCount} active at end, {r.MaxComponentCount} peak");

    if (r.Unmapped.Length != 0)
    {
        Console.WriteLine("unmapped ops (not translated to BMR — extend BmrBridge as these matter):");
        foreach (var (tag, count) in r.Unmapped)
            Console.WriteLine($"  {count,6}  {tag}");
    }

    var ok = r.ModuleName != null && r.MaxComponentCount > 0;
    Console.WriteLine(ok
        ? $"{Environment.NewLine}phase 1 milestone OK — BMR module driven from a Minerva recording, components activated."
        : $"{Environment.NewLine}phase 1 milestone NOT met — components never activated.");
    return ok ? 0 : 1;
}

// Phase 2: run both engines over one recording and report where their drawn AOEs disagree.
int CompareBmr(string? recording, string? modulesPath, string? bmrPath, string? dalamudDir, string? gameDir)
{
    if (recording == null || !File.Exists(recording))
    {
        Console.Error.WriteLine("usage: minerva-validate --compare-bmr <recording.log> [--modules <Minerva.dll>]");
        return 2;
    }
    if (modulesPath == null || !File.Exists(modulesPath))
    {
        Console.Error.WriteLine("error: could not find Minerva.dll — pass --modules <path>.");
        return 2;
    }

    BmrHost host;
    try { host = BmrHost.Load(bmrPath, dalamudDir, gameDir); }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"error: could not load BossmodReborn: {ex.Message}");
        return 2;
    }

    var mineRegistry = ModuleRegistry.Build(Assembly.LoadFrom(Path.GetFullPath(modulesPath)), typeof(ModuleRegistry).Assembly);

    ReplayTimeline timeline;
    using (var reader = new StreamReader(recording))
        timeline = ReplayParser.ParseTimeline(reader);

    var htmlOut = ArgValue("--html");
    var r = CompareDriver.Run(timeline, mineRegistry, host, capture: htmlOut != null);

    Console.WriteLine($"recording  {Path.GetFileName(recording)}");
    Console.WriteLine($"minerva    {r.MinervaModule ?? "(no module activated)"}");
    Console.WriteLine($"bmr        {r.BmrModule ?? "(no module activated)"}");
    Console.WriteLine($"frames     {r.Frames} total, {r.FramesCompared} with both modules live, {r.FramesAgreeing} agreeing");
    // agreement alone is misleading — two engines drawing nothing agree on every frame
    Console.WriteLine($"drew       minerva {r.MinervaAoeFrames} frame(s), bmr {r.BmrAoeFrames} frame(s), {r.MatchedPairs} matched AOE pair(s)");
    if (r.BmrAoeFrames == 0 && r.MinervaAoeFrames != 0)
        Console.WriteLine("           !! BMR produced nothing all run — treat the agreement figure as meaningless and fix the harness first");
    if (r.BmrError != null)
        Console.WriteLine("bmr error  " + r.BmrError[..Math.Min(220, r.BmrError.Length)]);
    if (r.BmrDiag.Length != 0)
        Console.WriteLine("bmr state  " + r.BmrDiag);

    if (r.MinervaModule == null || r.BmrModule == null)
    {
        Console.WriteLine($"{Environment.NewLine}cannot compare: only one side has a module for this fight.");
        return 1;
    }

    if (htmlOut != null)
    {
        ViewerWriter.Write(htmlOut, Path.GetFileName(recording), r);
        Console.WriteLine($"viewer     {Path.GetFullPath(htmlOut)} ({r.Captured.Count} frames)");
    }

    if (r.Diffs.Length == 0)
    {
        Console.WriteLine($"{Environment.NewLine}no disagreements (tolerance: {CompareDriver.PosTolerance}y position, " +
                          $"{CompareDriver.RotTolerance} deg rotation, {CompareDriver.DimTolerance}y dimensions).");
        return 0;
    }

    Console.WriteLine($"{Environment.NewLine}disagreements (first {r.Diffs.Length}):");
    foreach (var d in r.Diffs)
        Console.WriteLine($"  frame {d.Frame,4} {d.Time:mm:ss}  {d.Side,-12} {d.Aoe}");
    Console.WriteLine($"{Environment.NewLine}NOTE: BMR is a second implementation, not ground truth — a disagreement");
    Console.WriteLine("      means \"look here\", not \"Minerva is wrong\".");
    return 1;
}

string? ArgValue(string name)
{
    var i = Array.IndexOf(args_, name);
    return i >= 0 && i + 1 < args_.Length ? args_[i + 1] : null;
}

// look next to the tool first, then in the repo's plugin output
static string? FindPluginAssembly()
{
    foreach (var candidate in new[]
    {
        Path.Combine(AppContext.BaseDirectory, "Minerva.dll"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "bin", "x64", "Debug", "Minerva.dll"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "bin", "x64", "Release", "Minerva.dll"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "bin", "Debug", "Minerva.dll"),
    })
    {
        if (File.Exists(candidate))
            return Path.GetFullPath(candidate);
    }
    return null;
}


/// <summary>
/// Turn recordings into module drafts with no game running.
/// <para>The generator's one dependency on the game was the Action sheet — shapes, cone angles, readable
/// names — reached through Dalamud. That single lookup meant a draft could only be produced in-game and
/// carried back by hand, so the extract → generate → validate → compare-against-BMR loop could never run
/// unattended. Lumina reads the same sheets off the install, so it can.</para>
/// </summary>
static int Generate(string? target, string? gamePath, string? outDir)
{
    if (target == null)
    {
        Console.Error.WriteLine("error: --generate needs a recording or a folder.");
        return 2;
    }

    var logs = Directory.Exists(target)
        ? Directory.GetFiles(target, "*.log").OrderBy(f => f).ToArray()
        : File.Exists(target) ? [target] : [];
    if (logs.Length == 0)
    {
        Console.Error.WriteLine($"error: no recordings at {target}.");
        return 2;
    }

    var candidates = gamePath != null ? [gamePath] : OfflineGameSheets.LikelyPaths().ToArray();
    OfflineGameSheets? sheets = null;
    var reasons = new List<string>();
    foreach (var c in candidates)
    {
        sheets = OfflineGameSheets.TryOpen(c, out var why);
        if (sheets != null)
        {
            Console.WriteLine($"game data: {c}");
            break;
        }

        reasons.Add(why);
    }

    if (sheets == null)
    {
        Console.Error.WriteLine(@"error: could not open the game's Excel sheets. Pass --game <...\game\sqpack>.");
        foreach (var r in reasons.Take(3))
            Console.Error.WriteLine($"       {r}");
        return 2;
    }

    var generator = new ModuleGenerator(sheets, sheets);
    var written = 0;
    foreach (var log in logs)
    {
        ReplayAnalysis? analysis = null;
        try
        {
            using var reader = new StreamReader(log);
            ReplayParser.Replay(reader, ws => analysis = ReplayAnalysis.Attach(ws));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"{Path.GetFileName(log)}: could not parse — {ex.Message}");
            continue;
        }

        var input = analysis?.BuildGenerationInput();
        if (input == null || input.BossOID == 0)
        {
            Console.WriteLine($"{Path.GetFileName(log)}: no boss found, skipped");
            continue;
        }

        var result = generator.Generate(input);
        var dir = outDir ?? Path.GetDirectoryName(Path.GetFullPath(log))!;
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, input.DraftFileName());
        File.WriteAllText(path, result.Code);
        ++written;

        Console.WriteLine();
        Console.WriteLine($"=== {Path.GetFileName(log)} ===");
        Console.WriteLine(result.Report);
        Console.WriteLine($"-> {path}");
    }

    Console.WriteLine();
    Console.WriteLine($"{written} draft(s) written from {logs.Length} recording(s).");
    return written == 0 ? 1 : 0;
}
