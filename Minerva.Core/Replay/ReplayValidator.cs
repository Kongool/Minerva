using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Minerva.Generation;

namespace Minerva;

/// <summary>
/// Replays a recording through the matching boss module and reports how well the module covers the
/// fight — which enemy casts it actually drew an AOE for, which it merely hints/warns about, and which
/// it ignores entirely (candidates for a missing component). This is the automated round-trip check
/// BMR authors do by eye. Fully offline: it only needs the timeline + the module registry.
/// </summary>
public sealed class ReplayValidator
{
    /// <summary>
    /// Per-fight coverage: <see cref="Drawn"/> produced an AOE, <see cref="Hinted"/> is watched by a
    /// non-AOE component (raidwide/tankbuster/gaze/…), <see cref="Uncovered"/> was cast but the module
    /// did nothing (single-target, or a mechanic you haven't handled yet).
    /// </summary>
    public sealed record Result(
        string ModuleName, int EnemyActions,
        IReadOnlyList<uint> Drawn, IReadOnlyList<uint> Hinted,
        IReadOnlyList<uint> UncoveredMechanics, IReadOnlyList<uint> UncoveredVisuals,
        string? ArenaNote)
    {
        /// <summary>Every enemy hit the POV took, each judged against what the dodge knew at that instant.</summary>
        public IReadOnlyList<Hit> Hits { get; init; } = [];

        /// <summary>Whose hits these are: "you" for the recorder, else the player named with --pov.</summary>
        public string PovName { get; init; } = "you";

        public int Uncovered => this.UncoveredMechanics.Count + this.UncoveredVisuals.Count;

        public string Render(INameResolver? names = null)
        {
            string Join(IReadOnlyList<uint> aids)
            {
                var parts = new List<string>(aids.Count);
                foreach (var a in aids)
                    parts.Add(names?.ActionName(a) is { Length: > 0 } n ? $"{n} ({a})" : a.ToString());
                return string.Join(", ", parts);
            }

            var b = new StringBuilder();
            b.AppendLine($"Validation vs recording — module: {this.ModuleName}");
            b.AppendLine($"Enemy actions cast: {this.EnemyActions}   drawn: {this.Drawn.Count}   hinted: {this.Hinted.Count}   uncovered: {this.Uncovered}");
            if (this.Drawn.Count > 0)
                b.AppendLine("  drawn AOE:  " + Join(this.Drawn));
            if (this.Hinted.Count > 0)
                b.AppendLine("  hint only:  " + Join(this.Hinted));
            // helper casts are only ever mechanics, so an uncovered one is a likely miss; boss self-casts are usually just visuals
            if (this.UncoveredMechanics.Count > 0)
                b.AppendLine("  UNCOVERED (helper-cast — likely a missed mechanic): " + Join(this.UncoveredMechanics));
            if (this.UncoveredVisuals.Count > 0)
                b.AppendLine("  uncovered (boss visual / single-target — usually fine): " + Join(this.UncoveredVisuals));
            if (this.ArenaNote != null)
                b.AppendLine("  " + this.ArenaNote);
            if (this.Hits.Count > 0)
            {
                b.AppendLine($"  Hits on {this.PovName}: {this.Hits.Count}");
                foreach (var h in this.Hits)
                {
                    var name = names?.ActionName(h.Action) is { Length: > 0 } n ? $"{n} ({h.Action})" : h.Action.ToString();
                    b.AppendLine($"    {h.Seconds,6:0.0}s  {name} from {h.Caster}: {h.Why}");
                }
            }
            return b.ToString();
        }
    }

    /// <summary>
    /// One enemy hit on the POV. <see cref="Drawn"/> is whether the module had the player inside a drawn
    /// AOE in the two seconds before the hit -- components clear their zones on cast end, a frame before
    /// the damage event arrives, so "inside right now" would call every dodged-late hit a module gap.
    /// </summary>
    public sealed record Hit(double Seconds, uint Action, string Caster, bool Drawn, DodgeDecision Decision, float Distance, bool Watched, uint StatusID, float FacingDeg, bool Gaze)
    {
        /// <summary>Instance id of the caster, for matching a status it applies a moment later.</summary>
        public ulong CasterID { get; init; }

        /// <summary>An action of the POV's own that rooted it inside the dodge window, with when it fired
        /// relative to the cast start -- the rotation stepping on the dodge. Empty when nothing did.</summary>
        public string RootedBy { get; init; } = "";

        /// <summary>The hit is on another player: the module's picture is here, their dodge's decisions are not.</summary>
        public bool Foreign { get; init; }

        /// <summary>The same action hit every player present within half a second: a raidwide, whatever the module calls it.</summary>
        public bool PartyWide { get; init; }

        public string Why
        {
            get
            {
                var gave = this.StatusID != 0 ? $" It gave you status {this.StatusID}." : "";
                if (this.PartyWide && !this.Gaze && this.StatusID == 0)
                    return "hit everyone in the party at once: a raidwide, expected damage whatever the module calls it.";
                var everyone = this.PartyWide ? " It hit everyone at once and gave everyone a status: a mechanic the whole party failed, not a raidwide." : "";
                if (this.Foreign)
                {
                    if (this.Gaze)
                        return $"a gaze; they were facing it {this.FacingDeg.ToString("0", CultureInfo.InvariantCulture)} degrees off. Their own decisions are on their box's recording.{gave}";
                    if (!this.Drawn)
                        return "Minerva drew nothing for this: the module has no component for it, so no dodge was possible on any box (module gap)." + gave;
                    return "the module drew it and they were inside it when it landed. Why their dodge did not move them is on their box's recording, not this one." + gave + this.RootedBy;
                }
                if (this.Gaze)
                {
                    // a gaze component watches this action: the facing at the event is the whole story
                    var facing = this.FacingDeg.ToString("0", CultureInfo.InvariantCulture);
                    if (this.FacingDeg > 45f)
                        return $"a gaze you were not looking into ({facing} degrees off it), so this is not the gaze landing.{gave}";
                    return $"a gaze you were facing ({facing} degrees off it): " + (this.Decision.GazeUp
                        ? (this.Decision.Turning ? "Minerva was turning you away and the turn did not take." : "Minerva flagged the gaze but Face is off, so it only warned.")
                        : "the decision log carries no gaze flag (recorded before facing was logged, or the module never raised one).") + gave;
                }
                if (this.Watched)
                    return this.StatusID != 0
                        ? $"the module watches this as a non-AOE (raidwide, tankbuster, knockback) and draws no zone for it, yet it gave you status {this.StatusID}: if that is a vulnerability, the zone is missing (module gap)."
                        : "a raidwide or tankbuster the module watches: expected damage, not a dodge miss.";
                return this.Decision.Explain(this.Drawn, this.Distance) + gave + everyone + this.RootedBy;
            }
        }
    }

    /// <summary>
    /// Player actions that root the character for longer than an animation lock, by field measurement:
    /// Occult Jump is two seconds in the air, and three of the day's vulnerability stacks were the dodge
    /// starting the instant it left the ground (2026-09-05). Onslaught and Primal Rend are shorter but
    /// also move the character toward the target, which is usually the wrong way. Extend as found.
    /// </summary>
    private static readonly Dictionary<uint, (string Name, float Seconds)> RootingActions = new()
    {
        [49077] = ("Occult Jump", 2.0f),
        [25753] = ("Primal Rend", 1.1f),
        [7386] = ("Onslaught", 0.7f),
    };

    public static Result Validate(ReplayTimeline timeline, ModuleRegistry registry) => Validate(timeline, registry, 0);

    /// <summary>
    /// Validate with the hits judged for <paramref name="povOverride"/> instead of the recorder -- another
    /// player in the same pull. The module's zones are the same for everyone, so "did it draw what they
    /// stood in" is answerable here; their dodge's decisions are not, and the wording says so.
    /// </summary>
    public static Result Validate(ReplayTimeline timeline, ModuleRegistry registry, ulong povOverride)
    {
        var world = new WorldState(timeline.QPF, timeline.GameVersion);
        // without this the whole validation runs against the wrong character (see ReplayTimeline.PlayerInstanceID)
        world.Party.PlayerInstanceID = timeline.PlayerInstanceID;
        ModuleBase? module = null;
        var watched = new HashSet<uint>();
        var castCount = new SortedDictionary<uint, int>();
        var drawn = new HashSet<uint>();
        var helperCast = new HashSet<uint>(); // AIDs cast by Helpers (0x233C) — only ever mechanics
        ArenaBounds? initialBounds = null;
        bool boundsChanged = false, arenaMarkerSpawned = false;
        List<int> countsBefore = [], countsAfter = []; // reused each op; the timeline is millions of ops long
        var pov = povOverride != 0 ? povOverride : timeline.PlayerInstanceID;
        var foreign = pov != timeline.PlayerInstanceID;
        var povName = "you";
        if (foreign)
        {
            povName = $"0x{pov:X}";
            foreach (var (_, o) in timeline.Ops)
                if (o is ActorState.OpCreate c && c.InstanceID == pov)
                {
                    povName = c.Name;
                    break;
                }
        }
        var start = timeline.StartTicks;
        var hits = new List<Hit>();
        var lastInsideDrawnTicks = long.MinValue; // when the POV was last inside something the module drew
        var autos = new HashSet<uint> { 7u, 8u, 870u, 871u, 872u, 873u }; // the generic auto-attacks; the module adds its own
        var gazes = new HashSet<uint>(); // actions a gaze component watches: a status from one is judged by facing, not by zones
        var myActions = new List<(long Ticks, uint Action)>(); // the POV's own resolved actions, for the rooting look-back
        var myCasts = new List<(long Start, long End, uint Action, float Total)>(); // the POV's own hardcasts
        var enemyEvents = new List<(long Ticks, uint Action, HashSet<ulong> Players)>(); // who each enemy event hit, for the raidwide test
        var playersSeen = new HashSet<ulong>();
        var povStatuses = new List<(long Ticks, uint Status, ulong Source)>(); // statuses the POV gained, to attach late ones to their hit
        var castStarts = new Dictionary<(ulong Caster, uint Action), long>(); // when each enemy cast began
        foreach (var (ticks, op) in timeline.Ops)
        {
            // A damaging enemy hit on the POV, judged against what the dodge knew at that instant. Read
            // before the op applies: the decision and the drawn-AOE memory are "what Minerva had", not what
            // the hit changed.
            if (pov != 0 && op is ActorState.OpCastEvent mine && mine.InstanceID == pov && mine.Value is { } my)
                myActions.Add((ticks, my.Action.ID));
            if (op is ActorState.OpCastEvent any && any.Value is { } anyEv && world.Actors.Find(any.InstanceID) is { Type: not (ActorType.Player or ActorType.Pet or ActorType.Chocobo or ActorType.Buddy) })
            {
                var hitPlayers = new HashSet<ulong>();
                foreach (var t in anyEv.Targets)
                    if (playersSeen.Contains(t.ID))
                        hitPlayers.Add(t.ID);
                if (hitPlayers.Count > 0)
                    enemyEvents.Add((ticks, anyEv.Action.ID, hitPlayers));
            }
            if (pov != 0 && op is ActorState.OpCastEvent cev && cev.Value is { } ev && !autos.Contains(ev.Action.ID) && HitsPov(ev, pov, out var statusID)
                && world.Actors.Find(cev.InstanceID) is { } src
                && src.Type is not (ActorType.Player or ActorType.Pet or ActorType.Chocobo or ActorType.Buddy))
            {
                var me = world.Actors.Find(pov);
                var d = world.LastDodge;
                var dist = me != null && d.Found ? (d.Target - me.Position).Length() : 0f;
                // "drawn" means the module drew THIS action's zone, when the action is one that is cast; an
                // uncast helper hit (no cast bar) can only be judged by whether the POV stood in any drawn
                // zone in the two seconds before -- components clear zones on cast end, a frame before the hit
                // Either the module drew a zone when THIS action's cast began, or the POV stood in a drawn zone
                // in the two seconds before the hit (zones drawn ahead of the cast, from a marker or a tether,
                // never "rise" at cast start -- Pallmagia's Esoteric Instruction). Neither alone is right.
                var drawnRecently = drawn.Contains(ev.Action.ID) || ticks - lastInsideDrawnTicks <= 2 * TimeSpan.TicksPerSecond;
                var facingDeg = 180f;
                if (me != null && (src.Position - me.Position).LengthSq() > 0.01f)
                {
                    var dot = me.Rotation.ToDirection().Dot((src.Position - me.Position).Normalized());
                    facingDeg = MathF.Acos(Math.Clamp(dot, -1f, 1f)) * 180f / MathF.PI;
                }
                // the dodge window: from the cast start (or three seconds back for an uncast hit) to the hit
                var windowStart = castStarts.TryGetValue((cev.InstanceID, ev.Action.ID), out var cs) ? cs : ticks - 3 * TimeSpan.TicksPerSecond;
                var rooted = "";
                for (var i = myActions.Count - 1; i >= 0 && myActions[i].Ticks >= windowStart - TimeSpan.TicksPerSecond; --i)
                {
                    if (RootingActions.TryGetValue(myActions[i].Action, out var r))
                    {
                        var at = (myActions[i].Ticks - windowStart) / (double)TimeSpan.TicksPerSecond;
                        rooted = $" Your rotation fired {r.Name} at {at:+0.0;-0.0}s from the cast start, which roots you for about {r.Seconds:0.0}s: the dodge could not move until it ended.";
                        break;
                    }
                }
                // a hardcast of your own overlapping the window: movement written below the input layer does not interrupt it
                for (var i = myCasts.Count - 1; i >= 0 && myCasts[i].Start <= ticks; --i)
                {
                    var c = myCasts[i];
                    if (c.End >= windowStart && c.Start <= ticks && c.Total >= 1f)
                    {
                        var at = (c.Start - windowStart) / (double)TimeSpan.TicksPerSecond;
                        rooted += $" You were hardcasting action {c.Action} ({c.Total:0.0}s) from {at:+0.0;-0.0}s: injected movement cannot interrupt a cast, so the dodge stood still until it ended (Minerva now cancels the cast when the ground is lethal).";
                        break;
                    }
                }
                hits.Add(new Hit((ticks - start) / (double)TimeSpan.TicksPerSecond, ev.Action.ID, src.Name, drawnRecently, d, dist, watched.Contains(ev.Action.ID) && !drawn.Contains(ev.Action.ID), statusID, facingDeg, gazes.Contains(ev.Action.ID)) { RootedBy = rooted, Foreign = foreign, CasterID = cev.InstanceID });
            }
            // detect the start of an enemy/helper cast (players are ignored — their skills aren't mechanics)
            uint enemyCastAid = 0;
            var byHelper = false;
            if (pov != 0 && op is ActorState.OpCastInfo pc && pc.InstanceID == pov)
            {
                if (pc.Value is { } myCast)
                    myCasts.Add((ticks, long.MaxValue, myCast.Action.ID, myCast.TotalTime));
                else if (myCasts.Count > 0 && myCasts[^1].End == long.MaxValue)
                    myCasts[^1] = myCasts[^1] with { End = ticks };
            }
            if (op is ActorState.OpCreate made && made.Type == ActorType.Player)
                playersSeen.Add(made.InstanceID);
            if (pov != 0 && op is ActorState.OpStatus st && st.InstanceID == pov && st.Value.ID != 0)
                povStatuses.Add((ticks, st.Value.ID, st.Value.SourceID));
            if (op is ActorState.OpCastInfo ci && ci.Value is { } cast && cast.Action.ID != 0)
            {
                castStarts[(ci.InstanceID, cast.Action.ID)] = ticks;
                var caster = world.Actors.Find(ci.InstanceID);
                // Buddy is Duty Support: Alphinaud's Avatar and friends run full job rotations, and without
                // this every one of their casts lands in the uncovered list as a mechanic nobody handled.
                // A Treno run reported 23 uncovered casts, 21 of which were the NPC party's own abilities —
                // and this report is the pass/fail signal the module loop runs on, so the noise is not
                // cosmetic. ReplayAnalysis already excludes Buddy; this was the copy that did not.
                if (caster != null && caster.Type is not (ActorType.Player or ActorType.Pet or ActorType.Chocobo or ActorType.Buddy))
                {
                    enemyCastAid = cast.Action.ID;
                    byHelper = caster.Type == ActorType.Helper;
                }
            }

            DrawCounts(module, countsBefore);
            world.Execute(op);
            if (module == null)
            {
                module = TryActivate(world, registry, watched);
                if (module != null)
                {
                    initialBounds = module.Bounds;
                    autos.UnionWith(AutoAttackIds(module));
                    foreach (var c in module.Components)
                        if (c is Components.GenericGaze g && g.WatchedAction != 0)
                            gazes.Add(g.WatchedAction);
                }
            }
            module?.Update();
            DrawCounts(module, countsAfter);
            if (pov != 0 && module != null && op is WorldState.OpFrameStart && world.Actors.Find(pov) is { } pcNow && InsideAnyAoe(module, pcNow))
                lastInsideDrawnTicks = ticks;

            if (module != null)
            {
                // Reference, not radius. A dynamic arena that swaps which holes are cut out of the floor
                // -- Treno's rocks crumbling one by one -- keeps exactly the same extent, so a radius
                // comparison reports "the module never changed bounds" for a module that changes them
                // thirteen times. Bounds are immutable, so a reassignment is the change.
                if (!ReferenceEquals(module.Bounds, initialBounds))
                    boundsChanged = true;
                if (op is ActorState.OpCreate created && created.Type == ActorType.EventObj)
                    arenaMarkerSpawned = true; // an environment object appeared mid-fight (likely an arena change)
            }

            if (enemyCastAid != 0)
            {
                castCount[enemyCastAid] = castCount.GetValueOrDefault(enemyCastAid) + 1;
                if (byHelper)
                    helperCast.Add(enemyCastAid);
                if (AnyRose(countsBefore, countsAfter))
                    drawn.Add(enemyCastAid); // some component started showing something for this cast
            }
        }

        var drawnList = new List<uint>();
        var hinted = new List<uint>();
        var uncoveredMechanics = new List<uint>();
        var uncoveredVisuals = new List<uint>();
        foreach (var aid in castCount.Keys)
        {
            if (drawn.Contains(aid))
                drawnList.Add(aid);
            else if (watched.Contains(aid))
                hinted.Add(aid);
            else if (helperCast.Contains(aid))
                uncoveredMechanics.Add(aid); // a Helper cast the module ignores is a likely missing mechanic
            else
                uncoveredVisuals.Add(aid);    // a boss self-cast is usually a visual / single-target
        }

        string? arenaNote = boundsChanged
            ? "arena: bounds change mid-fight (handled by the module)"
            : arenaMarkerSpawned
                ? "⚠ arena: environment objects spawned but the module never changed bounds — check for a dynamic arena (Components.ArenaChange)"
                : null;

        // a status the same caster applies within the next second belongs to the hit (Bad Breath's Heavy)
        for (var i = 0; i < hits.Count; ++i)
        {
            var h = hits[i];
            if (h.StatusID != 0)
                continue;
            var at = start + (long)(h.Seconds * TimeSpan.TicksPerSecond);
            foreach (var (t, status, source) in povStatuses)
                if (t >= at && t - at <= TimeSpan.TicksPerSecond && source == h.CasterID)
                {
                    hits[i] = h with { StatusID = status };
                    break;
                }
        }

        // a raidwide arrives as several helper events, each listing a slice of the party; the union tells
        for (var i = 0; i < hits.Count; ++i)
        {
            var h = hits[i];
            var at = start + (long)(h.Seconds * TimeSpan.TicksPerSecond);
            var union = new HashSet<ulong>();
            foreach (var e in enemyEvents)
                if (e.Action == h.Action && Math.Abs(e.Ticks - at) <= TimeSpan.TicksPerSecond / 2)
                    union.UnionWith(e.Players);
            if (playersSeen.Count >= 2 && union.Count >= playersSeen.Count)
                hits[i] = h with { PartyWide = true };
        }

        return new Result(module?.GetType().Name ?? "(no module activated)", castCount.Count, drawnList, hinted, uncoveredMechanics, uncoveredVisuals, arenaNote) with { Hits = hits, PovName = povName };
    }

    private static ModuleBase? TryActivate(WorldState world, ModuleRegistry registry, HashSet<uint> watched)
    {
        if (world.CurrentCFCID == 0)
            return null;
        foreach (var info in registry.ForCFC(world.CurrentCFCID))
            foreach (var actor in world.Actors)
                if (actor.OID == info.PrimaryActorOID && !actor.IsDestroyed)
                {
                    var module = info.Create(world, actor);
                    CollectWatchedActions(module, watched);
                    return module;
                }
        return null;
    }

    /// <summary>
    /// How many things each component is currently telling the player to move out of — AOE zones, plus stack
    /// and spread circles, which hang off a different base class and used to be counted as nothing at all
    /// (so a spread mechanic could never be seen as covered, however correctly it was handled).
    /// <para>Kept per component rather than summed. Components interact: a module may blank one overlay while
    /// another lights up, and a single total then stays flat across the very op that drew something.</para>
    /// </summary>
    // The module's own auto-attack ids: every ported module names them AutoAttack in its AID enum. An
    // auto-attack is not a mechanic, and a hits list that is mostly "the boss hit the tank" explains nothing.
    private static IEnumerable<uint> AutoAttackIds(ModuleBase module)
    {
        var ns = module.GetType().Namespace;
        var aid = ns != null ? module.GetType().Assembly.GetType(ns + ".AID") : null;
        if (aid is not { IsEnum: true })
            yield break;
        foreach (var name in Enum.GetNames(aid))
            if (name.StartsWith("AutoAttack", StringComparison.OrdinalIgnoreCase))
                yield return Convert.ToUInt32(Enum.Parse(aid, name));
    }

    // a damaging effect on the POV among the event's resolved targets -- or a status applied to it (a gaze
    // petrifies without a point of damage), reported through statusID
    private static bool HitsPov(ActorCastEvent ev, ulong pov, out uint statusID)
    {
        statusID = 0;
        foreach (var t in ev.Targets)
        {
            if (t.ID != pov)
                continue;
            var damaged = false;
            var anything = false;
            foreach (var raw in t.Effects)
            {
                var eff = new ActionEffect(raw);
                if (eff.Type is ActionEffectType.Damage or ActionEffectType.BlockedDamage or ActionEffectType.ParriedDamage && eff.DamageHealValue > 0)
                    damaged = true;
                else if (eff.Type == ActionEffectType.ApplyStatusEffectTarget && statusID == 0)
                    statusID = eff.Value;
                // Bad Breath (Pallmagia) lands as an effect of a type this enum does not name, with no damage
                // and its Heavy arriving half a second later as a status op: anything the game bothered to
                // resolve on the POV beyond a miss is a hit
                else if ((byte)eff.Type is not (0 or 1 or 2 or 7 or 8 or 9 or 20 or 27))
                    anything = true;
            }
            return damaged || statusID != 0 || anything;
        }
        return false;
    }

    private static bool InsideAnyAoe(ModuleBase module, Actor me)
    {
        foreach (var c in module.Components)
            if (c is Components.GenericAOEs g)
                foreach (ref readonly var aoe in g.ActiveAOEs(0, me))
                    if (aoe.Shape.Check(me.Position, aoe.Origin, aoe.Rotation))
                        return true;
        return false;
    }

    private static void DrawCounts(ModuleBase? module, List<int> into)
    {
        into.Clear();
        if (module == null)
            return;
        foreach (var c in module.Components)
            into.Add(c switch
            {
                Components.GenericAOEs g => g.ActiveAOEs(0, module.PrimaryActor).Length,
                Components.GenericStackSpread s => s.ActiveStacks.Count + s.ActiveSpreads.Count,
                Components.GenericKnockback k => k.ActiveKnockbacks(0, module.PrimaryActor).Length,
                _ => 0,
            });
    }

    /// <summary>Did any single component start showing more than it was? Components can be added mid-fight
    /// by a phase change, so a slot that did not exist before counts as having been at zero.</summary>
    private static bool AnyRose(List<int> before, List<int> after)
    {
        for (var i = 0; i < after.Count; ++i)
            if (after[i] > (i < before.Count ? before[i] : 0))
                return true;
        return false;
    }

    // collect the action ids components explicitly watch so raidwides, tankbusters, gazes etc. count as
    // "handled" even though they draw no AOE zone. Any uint field whose name ends in Action qualifies:
    // CastCounter calls it WatchedAction, but CastStackSpread splits it into StackAction/SpreadAction, and
    // matching only the first name filed every cast-driven stack and spread as an uncovered mechanic.
    private static void CollectWatchedActions(ModuleBase module, HashSet<uint> watched)
    {
        foreach (var c in module.Components)
            foreach (var f in c.GetType().GetFields())
            {
                if (f.FieldType == typeof(uint) && f.Name.EndsWith("Action", StringComparison.Ordinal))
                    watched.Add((uint)f.GetValue(c)!);
                else if (f.FieldType == typeof(uint[]) && f.Name is "AIDs" or "Actions" or "WatchedActions")
                    foreach (var a in (uint[])f.GetValue(c)!)
                        watched.Add(a);
            }
    }
}
