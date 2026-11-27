using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace Minerva;

/// <summary>
/// The navmesh's way out of danger, where no boss module gives the floor a shape: trash rooms, deep dungeon floors, the
/// open world. The solve there works on a guessed box and the ground people have stood on; it knows nothing of walls,
/// so its nearest safe spot can be the other side of one or the end of an alcove. Pilgrim's Traverse 91-94, 2026-11-26:
/// toons ran from an Invoked Dreamer's Endless Nightmare (an 18y circle) into dead ends and died there; the blocked
/// heading (<see cref="StuckDodge"/>) only notices half a second after the wall is met.
/// <para>So when a dodge out of danger starts, the mesh is asked for the walk to the nearest safe spot in each of
/// <see cref="Directions"/> directions, and to the solve's own spot. A route counts when it ends on safe ground on this
/// floor; the shortest one that is clear on the clock of the walk is taken (failing that, the shortest). When the
/// solve's own spot is reached about as directly as the mesh allows, the solve keeps the steer: its route bends round
/// the AOEs, which the mesh knows nothing of.</para>
/// <para>Known ground is left out while spots are judged, as in <see cref="MeshWalk.Clear"/>: here the mesh is the
/// floor.</para>
/// </summary>
public sealed class MeshEscape
{
    public const int Directions = 12;

    /// <summary>How far along a direction a safe spot is looked for, and in what steps.</summary>
    public const float ReachYalms = 30f;
    public const float StepYalms = 1f;

    /// <summary>The solve's spot is reached directly when the mesh's walk to it is no longer than this times the
    /// chord, plus <see cref="DetourYalms"/>.</summary>
    public const float DetourFactor = 1.25f;
    public const float DetourYalms = 1.5f;

    /// <summary>And is kept unless a mesh route is shorter by more than this.</summary>
    public const float KeepSolveYalms = 2f;

    /// <summary>A route that ends this far above or below the character ends on another floor.</summary>
    public const float SameFloorYalms = 6f;

    public const float OffRouteYalms = 3f;

    /// <summary>Answers usually come the next frame. After this long the ones that came are used; after
    /// <see cref="GiveUpSeconds"/> with none, the solve steers.</summary>
    public const double WaitSeconds = 0.25d;
    public const double GiveUpSeconds = 1d;

    /// <summary>Never ask more often than this.</summary>
    public const double ReaskSeconds = 0.5d;

    private readonly List<Task<List<Vector3>>?> asks = [];
    private bool asking;
    private bool decided;
    private bool askNow;
    private bool solveAsked;
    private float solveChord;
    private DateTime askedAt;

    private List<Vector3>? route;
    private WPos routeFrom;

    /// <summary>The last <see cref="Route"/> handed back a mesh route.</summary>
    public bool Walking { get; private set; }

    /// <summary>Set on the frame a route is chosen or the mesh has none: one line for the log.</summary>
    public string? Verdict { get; private set; }

    /// <summary>How many times the mesh was asked, for tests.</summary>
    public int Asked { get; private set; }

    /// <summary>The dodge is over.</summary>
    public void Reset()
    {
        this.asks.Clear();
        this.asking = this.decided = this.askNow = false;
        this.route = null;
        this.Walking = false;
        this.Verdict = null;
    }

    /// <summary>The walk did not move the character: ask again from where it stands.</summary>
    public void Replan()
    {
        this.route = null;
        this.askNow = true;
    }

    /// <summary>
    /// The mesh route still ahead of a character at <paramref name="from"/> that stands in danger, with its heights;
    /// null to steer as the solve says (the mesh is thinking, has no way out, or the solve's own spot is reached
    /// directly). <paramref name="solved"/> is the solve's spot, null when it found none.
    /// <paramref name="deadline"/> is the time by which ground must be safe to count as safe.
    /// </summary>
    public IReadOnlyList<Vector3>? Route(AIHints hints, Vector3 from, WPos? solved, DateTime now, DateTime deadline, float margin, float moveSpeed,
        Func<Vector3, Vector3, Task<List<Vector3>>?> pathfind)
    {
        this.Walking = false;
        this.Verdict = null;
        var at = new WPos(from.X, from.Z);
        var ground = hints.WalkableGround;
        hints.WalkableGround = null;
        try
        {
            if (this.route != null)
            {
                var kept = MeshWalk.Ahead(this.routeFrom, this.route, at, out var off);
                var end = kept[^1];
                if (off > OffRouteYalms || hints.InImminentDanger(new WPos(end.X, end.Z), deadline, margin))
                {
                    // knocked off it, or something new covers where it ends: ask again
                    this.route = null;
                    this.askNow = true;
                }
            }

            if (!this.asking && (!this.decided || this.askNow) && (this.Asked == 0 || !this.decided || (now - this.askedAt).TotalSeconds >= ReaskSeconds))
                this.Ask(hints, from, at, solved, now, deadline, margin, pathfind);

            if (this.asking)
            {
                var waited = (now - this.askedAt).TotalSeconds;
                var done = 0;
                foreach (var a in this.asks)
                    if (a == null || a.IsCompleted)
                        ++done;
                if (done == this.asks.Count || (waited >= WaitSeconds && this.AnyAnswer()) || waited >= GiveUpSeconds)
                    this.Decide(hints, from, at, now, deadline, margin, moveSpeed);
            }

            if (this.route == null)
                return null;
            this.Walking = true;
            return MeshWalk.Ahead(this.routeFrom, this.route, at, out _);
        }
        finally
        {
            hints.WalkableGround = ground;
        }
    }

    private bool AnyAnswer()
    {
        foreach (var a in this.asks)
            if (a is { IsCompletedSuccessfully: true, Result.Count: > 0 })
                return true;
        return false;
    }

    private void Ask(AIHints hints, Vector3 from, WPos at, WPos? solved, DateTime now, DateTime deadline, float margin,
        Func<Vector3, Vector3, Task<List<Vector3>>?> pathfind)
    {
        this.asks.Clear();
        this.askNow = false;
        this.decided = false;
        this.askedAt = now;
        ++this.Asked;

        // the solve's own spot first: Decide reads it from slot 0
        this.solveAsked = solved != null;
        if (solved is { } s)
        {
            this.solveChord = (s - at).Length();
            this.asks.Add(pathfind(from, new Vector3(s.X, from.Y, s.Z)));
        }

        // the nearest safe spot each way, one step past the first safe sample so the mesh's snap does not put it back
        for (var k = 0; k < Directions; ++k)
        {
            var dir = (k * 360f / Directions).Degrees().ToDirection();
            for (var d = 2f; d <= ReachYalms; d += StepYalms)
            {
                var spot = at + (dir * (d + StepYalms));
                if (hints.InImminentDanger(at + (dir * d), deadline, margin) || hints.InImminentDanger(spot, deadline, margin))
                    continue;
                this.asks.Add(pathfind(from, new Vector3(spot.X, from.Y, spot.Z)));
                break;
            }
        }

        this.asking = true;
    }

    private void Decide(AIHints hints, Vector3 from, WPos at, DateTime now, DateTime deadline, float margin, float moveSpeed)
    {
        List<Vector3>? best = null;
        var bestLength = float.MaxValue;
        var bestClear = false;
        var solveDirect = false;
        var solveLength = float.MaxValue;
        var answered = 0;
        var flat = new List<WPos>();

        for (var i = 0; i < this.asks.Count; ++i)
        {
            if (this.asks[i] is not { IsCompletedSuccessfully: true, Result: { Count: > 0 } path })
                continue;
            ++answered;
            var end = path[^1];
            if (MathF.Abs(end.Y - from.Y) > SameFloorYalms || hints.InImminentDanger(new WPos(end.X, end.Z), deadline, margin))
                continue; // another floor, or the mesh could not reach the spot and stopped short of safety

            flat.Clear();
            var length = 0f;
            var prev = at;
            foreach (var p in path)
            {
                var q = new WPos(p.X, p.Z);
                length += (q - prev).Length();
                prev = q;
                flat.Add(q);
            }

            var clear = ArenaPathfinder.RouteIsClear(hints, at, flat, now, margin, moveSpeed);
            if (i == 0 && this.solveAsked)
            {
                solveDirect = clear && length <= (this.solveChord * DetourFactor) + DetourYalms;
                solveLength = length;
            }

            if ((clear && !bestClear) || (clear == bestClear && length < bestLength))
            {
                best = path;
                bestLength = length;
                bestClear = clear;
            }
        }

        var asked = this.asks.Count;
        this.asks.Clear();
        this.asking = false;
        this.decided = true;
        this.route = null;

        if (best == null)
        {
            this.Verdict = answered == 0 ? null : $"the navmesh has no way out of danger ({answered} of {asked} routes answered, none ends on safe ground)";
            return;
        }

        if (solveDirect && solveLength <= bestLength + KeepSolveYalms)
            return; // the solve's spot is where the mesh would go too: its route knows the AOEs

        this.route = best;
        this.routeFrom = at;
        var e = best[^1];
        var solve = !this.solveAsked ? "the solve found no spot"
            : solveLength == float.MaxValue ? "the solve's spot cannot be reached"
            : $"the solve's spot is a {solveLength:0.0}y walk for {this.solveChord:0.0}y";
        this.Verdict = $"walking the navmesh's way out, {bestLength:0.0}y to ({e.X:0.0},{e.Z:0.0}){(bestClear ? string.Empty : ", not clear in time")}; {solve}";
    }
}
