using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Minerva;

namespace Minerva.Validate;

/// <summary>
/// Runs Minerva's module and BossmodReborn's module over the same recording in lockstep and reports where
/// their drawn AOEs disagree (dual-viewer phase 2). Disagreement means "look here", not "Minerva is
/// wrong" — BMR is a second implementation, not ground truth.
/// </summary>
internal sealed class CompareDriver
{
    public sealed record Diff(int Frame, DateTime Time, string Side, string Aoe);

    /// <summary>One captured frame, for the viewer.</summary>
    public sealed record Frame(
        int Index, double Seconds,
        float CenterX, float CenterZ, float Radius,
        List<AoeSample> Minerva, List<AoeSample> Bmr,
        bool[] MinervaMatched, bool[] BmrMatched,
        List<(float X, float Z, bool IsPlayer, bool IsPrimary)> Actors);

    public sealed record Result(
        string? MinervaModule, string? BmrModule,
        int Frames, int FramesCompared, int FramesAgreeing,
        Diff[] Diffs, (string Tag, int Count)[] Unmapped,
        List<Frame> Captured,
        int MinervaAoeFrames, int BmrAoeFrames, int MatchedPairs, string? BmrError, string BmrDiag);

    /// <summary>Tolerances for calling two AOEs "the same". Tuned loose enough to absorb the two engines'
    /// differing quantisation, tight enough that a wrong radius or a mis-aimed cone still shows up.</summary>
    public const float PosTolerance = 0.5f;
    public const float RotTolerance = 5f;
    public const float DimTolerance = 0.5f;

    public static Result Run(ReplayTimeline timeline, ModuleRegistry minervaRegistry, BmrHost host, int maxDiffs = 40, bool capture = false)
    {
        var mWorld = new WorldState(timeline.QPF, timeline.GameVersion);
        var bridge = new BmrBridge(host.Assembly, timeline.QPF, timeline.GameVersion);

        ModuleBase? mModule = null;
        object? bModule = null;
        MethodInfo? bUpdate = null;
        string? bName = null;

        var diffs = new List<Diff>();
        var captured = new List<Frame>();
        DateTime? t0 = null;
        int frames = 0, compared = 0, agreeing = 0;
        // Without these the agreement percentage is unreadable: two engines that both draw nothing agree
        // perfectly. If one side's frame count is zero the harness is broken, not the module.
        int mineFrames = 0, theirsFrames = 0, matchedPairs = 0;
        string? bmrError = null;
        var bmrDiag = ""; // first failure out of BMR's Update, so a silent catch cannot hide a dead run

        foreach (var (_, op) in timeline.Ops)
        {
            // --- Minerva side ---
            mWorld.Execute(op);
            mModule ??= TryActivateMinerva(mWorld, minervaRegistry);
            mModule?.Update();

            // --- BMR side ---
            BmrDriver.Apply(bridge, op);
            if (bModule == null && op is ActorState.OpCreate c && host.InfoForOID(c.OID) != null)
            {
                var primary = bridge.FindActor(c.InstanceID);
                if (primary != null && (bModule = host.CreateModuleForActor(bridge.WorldState, primary)) != null)
                {
                    bName = bModule.GetType().Name;
                    bUpdate = bModule.GetType().GetMethod("Update", Type.EmptyTypes);
                }
            }
            if (bModule != null)
            {
                // a BMR component needing state we didn't bridge must not abort the run — but it must not
                // pass unnoticed either: swallowing this is what let the comparison report agreement while
                // BMR was contributing nothing at all
                try { bUpdate!.Invoke(bModule, null); }
                catch (Exception ex) { bmrError ??= (ex.InnerException ?? ex).ToString(); }
                if (bmrDiag.Length == 0 && compared > 500)
                {
                    var bt = bModule.GetType();
                    var comps = (bt.GetField("Components")?.GetValue(bModule) as ICollection)?.Count ?? -1;
                    var sm = bt.GetField("StateMachine")?.GetValue(bModule) ?? bt.GetProperty("StateMachine")?.GetValue(bModule);
                    var phase = sm?.GetType().GetField("ActivePhaseIndex")?.GetValue(sm) ?? sm?.GetType().GetProperty("ActivePhaseIndex")?.GetValue(sm);
                    var primary = bt.GetField("PrimaryActor")?.GetValue(bModule) ?? bt.GetProperty("PrimaryActor")?.GetValue(bModule);
                    bmrDiag = $"components={comps} activePhase={phase} targetable={Member(primary, "IsTargetable")} inCombat={Member(primary, "InCombat")} deadOrDestroyed={Member(primary, "IsDeadOrDestroyed")}";

                    static object? Member(object? o, string name)
                    {
                        if (o == null)
                            return null;
                        var ty = o.GetType();
                        return ty.GetField(name)?.GetValue(o) ?? ty.GetProperty(name)?.GetValue(o);
                    }
                }
            }

            if (op is not WorldState.OpFrameStart)
                continue;

            frames++;
            if (mModule == null || bModule == null)
                continue;

            var pc = mWorld.Party.Player() ?? mWorld.Actors.FirstOrDefault(a => a.Type == ActorType.Player);
            if (pc == null)
                continue;

            compared++;
            var mine = AoeSample.FromMinerva(mModule, pc);
            var theirs = AoeSample.FromBmr(bModule, bridge.FindActor(pc.InstanceID));
            if (mine.Count != 0)
                mineFrames++;
            if (theirs.Count != 0)
                theirsFrames++;

            // pair them up: each Minerva AOE claims at most one BMR AOE
            var mineMatched = new bool[mine.Count];
            var theirsMatched = new bool[theirs.Count];
            for (var i = 0; i < mine.Count; ++i)
            {
                for (var j = 0; j < theirs.Count; ++j)
                {
                    if (theirsMatched[j] || !mine[i].Matches(theirs[j], PosTolerance, RotTolerance, DimTolerance))
                        continue;
                    mineMatched[i] = theirsMatched[j] = true;
                    matchedPairs++;
                    break;
                }
            }

            var onlyMine = mine.Where((_, i) => !mineMatched[i]).ToList();
            var unmatchedTheirs = theirs.Where((_, j) => !theirsMatched[j]).ToList();

            if (capture)
            {
                t0 ??= mWorld.CurrentTime;
                captured.Add(new Frame(frames, (mWorld.CurrentTime - t0.Value).TotalSeconds,
                    mModule.Center.X, mModule.Center.Z, mModule.Bounds.Radius,
                    mine, theirs, mineMatched, theirsMatched,
                    [.. mWorld.Actors.Where(a => !a.IsDestroyed)
                        .Select(a => (a.Position.X, a.Position.Z, a.Type == ActorType.Player, a.InstanceID == mModule.PrimaryActor.InstanceID))]));
            }

            if (onlyMine.Count == 0 && unmatchedTheirs.Count == 0)
            {
                agreeing++;
                continue;
            }

            foreach (var a in onlyMine)
                if (diffs.Count < maxDiffs)
                    diffs.Add(new Diff(frames, mWorld.CurrentTime, "minerva-only", a.Describe()));
            foreach (var b in unmatchedTheirs)
                if (diffs.Count < maxDiffs)
                    diffs.Add(new Diff(frames, mWorld.CurrentTime, "bmr-only", b.Describe()));
        }

        return new Result(mModule?.GetType().Name, bName, frames, compared, agreeing, [.. diffs],
            bridge.Unmapped.OrderByDescending(kv => kv.Value).Select(kv => (kv.Key, kv.Value)).ToArray(), captured,
            mineFrames, theirsFrames, matchedPairs, bmrError, bmrDiag);
    }

    private static ModuleBase? TryActivateMinerva(WorldState world, ModuleRegistry registry)
    {
        if (world.CurrentCFCID == 0)
            return null;
        foreach (var info in registry.ForCFC(world.CurrentCFCID))
            foreach (var actor in world.Actors)
                if (actor.OID == info.PrimaryActorOID && !actor.IsDestroyed)
                    return info.Create(world, actor);
        return null;
    }
}
