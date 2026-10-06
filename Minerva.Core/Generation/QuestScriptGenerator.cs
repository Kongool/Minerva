using System.Globalization;
using System.Text;

namespace Minerva.Generation;

/// <summary>
/// What the player did in a solo duty, read from a recording as it replays: where they walked, what they clicked, what
/// they fought and in what order, and whom they healed. The raw material for <see cref="QuestScriptGenerator"/>.
///
/// <para>Recordings already carry all of it: the player's position each frame, their target changes, every action
/// they used and on whom, and each object's spawn, death, targetability and event state. A click is not recorded as
/// such; it is read as "targeted an event object, stood next to it, and the object then changed", or as a cast bar
/// the player started on an object.</para>
/// </summary>
public sealed class QuestScriptCapture
{
    public sealed record class ActorInfo(ulong InstanceID, uint OID, ActorType Type, string Name, bool IsAlly);

    public readonly record struct Sample(DateTime At, Vector3 Position);

    /// <summary>The object stopped being clickable, so the click it was waiting for happened.</summary>
    public enum InteractEnd { None, State7, Destroyed, Untargetable }

    public sealed record class Interaction(DateTime At, Vector3 Where, ActorInfo Target, InteractEnd End, DateTime EndAt);

    public sealed record class Fight(DateTime Start, DateTime End, Vector3 Where, List<(DateTime At, ActorInfo Enemy)> Kills, List<ActorInfo> AttackOrder);

    public sealed record class Heal(DateTime At, uint ActionId, ActorInfo Target, float HPRatio);

    public uint CFC;
    public ushort Zone;
    public DateTime Start, End;
    public readonly List<Sample> Path = [];
    public readonly List<Interaction> Interactions = [];
    public readonly List<Fight> Fights = [];
    public readonly List<Heal> Heals = [];
    public readonly Dictionary<uint, int> ActionsUsed = [];
    public readonly List<(DateTime At, uint UpdateID, uint P1, uint P2)> DirectorUpdates = [];
    public bool RolePlaying;

    /// <summary>How close the player must stand to an object they targeted for it to count as clicked.</summary>
    public const float InteractRange = 5f;

    private readonly WorldState ws;
    private readonly Dictionary<ulong, ActorInfo> actors = [];
    private (ActorInfo Target, DateTime Since)? pendingClick;
    private Fight? fight;
    private DateTime lastSample;

    private QuestScriptCapture(WorldState ws) => this.ws = ws;

    /// <summary>Subscribe to a world before a recording replays into it.</summary>
    public static QuestScriptCapture Attach(WorldState ws)
    {
        var c = new QuestScriptCapture(ws);
        ws.FrameStarted.Subscribe(_ => c.OnFrame());
        ws.CurrentZoneChanged.Subscribe(op =>
        {
            if (op.CFCID != 0 && c.CFC == 0)
            {
                c.CFC = op.CFCID;
                c.Zone = op.Zone;
            }
        });
        ws.Actors.Added.Subscribe(a => c.actors[a.InstanceID] = new ActorInfo(a.InstanceID, a.OID, a.Type, a.Name, a.IsAlly));
        ws.Actors.TargetChanged.Subscribe(c.OnTarget);
        ws.Actors.CastStarted.Subscribe(c.OnCastStarted);
        ws.Actors.EventStateChanged.Subscribe((a, state) => c.OnObjectChanged(a, state == 7 ? InteractEnd.State7 : InteractEnd.None));
        ws.Actors.Removed.Subscribe(a => c.OnObjectChanged(a, InteractEnd.Destroyed));
        ws.Actors.IsTargetableChanged.Subscribe(a => c.OnObjectChanged(a, a.IsTargetable ? InteractEnd.None : InteractEnd.Untargetable));
        ws.Actors.InCombatChanged.Subscribe(c.OnCombat);
        ws.Actors.IsDeadChanged.Subscribe(c.OnDead);
        ws.Actors.CastEvent.Subscribe(c.OnCast);
        ws.Actors.StatusGain.Subscribe((a, i) =>
        {
            if (a == c.Player && a.Statuses[i].ID == (uint)Roleplay.SID.RolePlaying)
                c.RolePlaying = true;
        });
        ws.DirectorUpdate.Subscribe(op => c.DirectorUpdates.Add((ws.CurrentTime, op.UpdateID, op.Param1, op.Param2)));
        return c;
    }

    private Actor? Player => this.ws.Party.Player();

    private ActorInfo Info(Actor a) => this.actors.TryGetValue(a.InstanceID, out var i) ? i : this.actors[a.InstanceID] = new ActorInfo(a.InstanceID, a.OID, a.Type, a.Name, a.IsAlly);

    private void OnFrame()
    {
        var now = this.ws.CurrentTime;
        if (this.Start == default && now != default)
            this.Start = now;
        this.End = now;
        if (this.Player is not { } pc || this.CFC == 0)
            return;
        var pos = pc.PosRot.XYZ();
        // a sample every half second is plenty for a path that is simplified afterwards
        if (now - this.lastSample >= TimeSpan.FromSeconds(0.5) && (this.Path.Count == 0 || Vector3.Distance(this.Path[^1].Position, pos) > 0.25f))
        {
            this.Path.Add(new Sample(now, pos));
            this.lastSample = now;
        }

        // a targeted object becomes a click once the player stands next to it
        if (this.pendingClick is { } click && this.ws.Actors.Find(click.Target.InstanceID) is { } obj
            && (obj.Position - pc.Position).Length() <= InteractRange + obj.HitboxRadius
            && !this.Interactions.Exists(i => i.Target.InstanceID == obj.InstanceID && i.End == InteractEnd.None))
        {
            this.Interactions.Add(new Interaction(now, pos, click.Target, InteractEnd.None, default));
            this.pendingClick = null;
        }
    }

    private void OnTarget(Actor a)
    {
        if (a != this.Player)
            return;
        this.pendingClick = this.ws.Actors.Find(a.TargetID) is { Type: ActorType.EventObj or ActorType.EventNpc } t ? (this.Info(t), this.ws.CurrentTime) : null;
    }

    // a cast bar on an object is a click too: an interact sent without targeting (a script's, The Oracle of Light's
    // Bindings, 2026-10-04) shows no target change, only the player's cast on the object
    private void OnCastStarted(Actor a)
    {
        if (a != this.Player || a.CastInfo is not { } cast || this.ws.Actors.Find(cast.TargetID) is not { Type: ActorType.EventObj or ActorType.EventNpc } obj
            || this.Interactions.Exists(i => i.Target.InstanceID == obj.InstanceID && i.End == InteractEnd.None))
            return;
        this.Interactions.Add(new Interaction(this.ws.CurrentTime, a.PosRot.XYZ(), this.Info(obj), InteractEnd.None, default));
        if (this.pendingClick?.Target.InstanceID == obj.InstanceID)
            this.pendingClick = null;
    }

    private void OnObjectChanged(Actor a, InteractEnd end)
    {
        if (end == InteractEnd.None)
            return;
        var i = this.Interactions.FindLastIndex(x => x.Target.InstanceID == a.InstanceID && x.End == InteractEnd.None);
        if (i >= 0)
            this.Interactions[i] = this.Interactions[i] with { End = end, EndAt = this.ws.CurrentTime };
    }

    private void OnCombat(Actor a)
    {
        if (a != this.Player)
            return;
        if (a.InCombat && this.fight == null)
            this.fight = new Fight(this.ws.CurrentTime, default, a.PosRot.XYZ(), [], []);
        else if (!a.InCombat && this.fight != null)
        {
            this.Fights.Add(this.fight with { End = this.ws.CurrentTime });
            this.fight = null;
        }
    }

    private void OnDead(Actor a)
    {
        if (a.IsDead && this.fight != null && a.Type == ActorType.Enemy && !a.IsAlly)
            this.fight.Kills.Add((this.ws.CurrentTime, this.Info(a)));
    }

    private void OnCast(Actor caster, ActorCastEvent ev)
    {
        if (caster != this.Player || ev.Action.Type != ActionType.Spell)
            return;
        this.ActionsUsed[ev.Action.ID] = this.ActionsUsed.GetValueOrDefault(ev.Action.ID) + 1;
        if (this.ws.Actors.Find(ev.MainTargetID) is not { } target || target == caster)
            return;
        if (target.Type == ActorType.Enemy && !target.IsAlly)
        {
            if (this.fight != null && !this.fight.AttackOrder.Exists(e => e.OID == target.OID))
                this.fight.AttackOrder.Add(this.Info(target));
        }
        else if (target.Type is not ActorType.Player)
        {
            // an ally that is not a player: an NPC the duty wants kept alive, or cured
            this.Heals.Add(new Heal(this.ws.CurrentTime, ev.Action.ID, this.Info(target), target.HPMP.MaxHP > 0 ? target.HPRatio : 1f));
        }
    }

    /// <summary>Close a fight still open when the recording ended.</summary>
    public void Finish()
    {
        if (this.fight != null)
        {
            this.Fights.Add(this.fight with { End = this.End });
            this.fight = null;
        }
    }
}

/// <summary>
/// Writes a quest battle script (<see cref="QuestBattle.QuestBattle"/>) from a recording of the duty played by hand:
/// the walk between steps, each click and the event that ended it, each fight with the order things died in, and a heal
/// rule for each ally the player healed. A draft, like <see cref="ModuleGenerator"/>'s: it says what happened, and the
/// comments say where it guessed. The user, 2026-10-01: "something that would write it out into a script file ... where
/// you need to path to, what you need to interact with, what you need to kill".
/// </summary>
public static class QuestScriptGenerator
{
    /// <summary>A walk is kept to the corners that matter: points off the straight line by more than this.</summary>
    public const float PathTolerance = 2f;

    private sealed record class Step(DateTime At, DateTime Until, Vector3 Where, string Kind, QuestScriptCapture.Interaction? Click, QuestScriptCapture.Fight? Fight);

    public static string Generate(QuestScriptCapture cap, string className, string dutyName, INameResolver names, string sourceFile)
    {
        var steps = new List<Step>();
        foreach (var i in cap.Interactions)
            steps.Add(new Step(i.At, i.End != QuestScriptCapture.InteractEnd.None ? i.EndAt : i.At, i.Where, "click", i, null));
        foreach (var f in cap.Fights)
            if (f.Kills.Count > 0 || f.AttackOrder.Count > 0)
                steps.Add(new Step(f.Start, f.End, f.Where, "fight", null, f));
        steps.Sort((a, b) => a.At.CompareTo(b.At));

        var sb = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;
        string T(DateTime at) => (at - cap.Start).TotalSeconds.ToString("0.0", inv) + "s";
        string V(Vector3 p) => string.Create(inv, $"new Vector3({p.X:F2}f, {p.Y:F2}f, {p.Z:F2}f)");
        string Name(QuestScriptCapture.ActorInfo a) => (a.Name.Length > 0 ? a.Name : names.ObjectName(a.OID)) ?? "?";

        sb.AppendLine($"// Generated by Minerva.Validate --generate-quest from {sourceFile}: {dutyName} played by hand.");
        sb.AppendLine("// A DRAFT. Each objective is one thing the recording shows; the comments say where it guessed. Review the");
        sb.AppendLine("// kill order (it may not matter), the click endings, and anything marked TODO, then move it under Modules/QuestBattle.");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using Minerva;");
        sb.AppendLine("using Minerva.QuestBattle;");
        sb.AppendLine();
        sb.AppendLine("namespace Minerva.QuestBattle.Generated;");
        sb.AppendLine();
        sb.AppendLine($"[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, {cap.CFC})]");
        sb.AppendLine($"public sealed class {className}(WorldState ws) : QuestBattle(ws)");
        sb.AppendLine("{");
        sb.AppendLine("    public override List<QuestObjective> DefineObjectives(WorldState ws) => [");

        var from = cap.Start;
        foreach (var s in steps)
        {
            var walk = Simplify(cap.Path.Where(p => p.At >= from && p.At <= s.At).Select(p => p.Position).ToList(), PathTolerance);
            if (walk.Count > 0)
                walk.RemoveAt(0); // where the last step left the character
            if (s.Click is { } c)
            {
                sb.AppendLine($"        // {T(s.At)}: clicked {Name(c.Target)} (0x{c.Target.OID:X})");
                sb.AppendLine("        new QuestObjective(ws)");
                sb.AppendLine($"            .Named(\"{Escape(Name(c.Target))}\")");
                foreach (var p in walk)
                    sb.AppendLine($"            .WithConnection({V(p)})");
                sb.AppendLine($"            .WithConnection({V(c.Where)})");
                sb.AppendLine($"            .WithInteract(0x{c.Target.OID:X}u)");
                sb.AppendLine(c.End switch
                {
                    QuestScriptCapture.InteractEnd.State7 => $"            .CompleteOnState7(0x{c.Target.OID:X}u),",
                    QuestScriptCapture.InteractEnd.Destroyed => $"            .CompleteOnDestroyed(0x{c.Target.OID:X}u),",
                    QuestScriptCapture.InteractEnd.Untargetable => $"            .CompleteOnTargetable(0x{c.Target.OID:X}u, false),",
                    _ => $"            .CompleteOnState7(0x{c.Target.OID:X}u), // TODO: the recording showed no change on the object after the click; pick the event that ends this step",
                });
            }
            else if (s.Fight is { } f)
            {
                var killed = f.Kills.Select(k => k.Enemy).GroupBy(e => e.OID).Select(g => (g.First(), g.Count())).ToList();
                var what = killed.Count > 0 ? string.Join(", ", killed.Select(k => $"{k.Item2}x {Name(k.Item1)} (0x{k.Item1.OID:X})")) : "nothing died";
                sb.AppendLine($"        // {T(s.At)}-{T(s.Until)}: fight -- killed {what}");
                sb.AppendLine("        new QuestObjective(ws)");
                sb.AppendLine($"            .Named(\"Fight {Escape(killed.Count > 0 ? Name(killed[^1].Item1) : "pack")}\")");
                foreach (var p in walk)
                    sb.AppendLine($"            .WithConnection({V(p)})");
                sb.AppendLine($"            .WithConnection({V(f.Where)})");
                var order = killed.Count > 0 ? killed.Select(k => k.Item1).ToList() : f.AttackOrder;
                if (order.Count > 0)
                {
                    // what died first is fought first: the user's point, 2026-10-01, that some duties want one mob before
                    // another. Every one of them is raised to at least 0, so the character also pulls ones not yet fighting.
                    sb.AppendLine("            .Hints((player, hints) =>");
                    sb.AppendLine("            {");
                    if (order.Count > 1)
                        sb.AppendLine("                // the order they died in the recording; flatten to one priority if it does not matter");
                    for (var i = 0; i < order.Count; ++i)
                        sb.AppendLine($"                hints.PrioritizeTargetsByOID(0x{order[i].OID:X}u, {order.Count - i}); // {Name(order[i])}");
                    sb.AppendLine("            })");
                }
                sb.AppendLine("            .With(obj => obj.OnActorCombatChanged += act => obj.CompleteIf(act.OID == 0 && !act.InCombat)),");
            }
            from = s.Until > s.At ? s.Until : s.At;
        }

        var tail = Simplify(cap.Path.Where(p => p.At >= from).Select(p => p.Position).ToList(), PathTolerance);
        if (tail.Count > 1)
        {
            tail.RemoveAt(0);
            sb.AppendLine($"        // {T(from)}-{T(cap.End)}: the walk to where the recording ended");
            sb.AppendLine("        new QuestObjective(ws)");
            sb.AppendLine("            .Named(\"Finish\")");
            foreach (var p in tail)
                sb.AppendLine($"            .WithConnection({V(p)})");
            sb.AppendLine("            .CompleteAtDestination(),");
        }
        sb.AppendLine("    ];");

        // allies the player healed or cured: kept up the same way, from the highest HP the player acted at
        var heals = cap.Heals.GroupBy(h => (h.Target.OID, h.ActionId)).ToList();
        if (heals.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("    public override void AddQuestAIHints(Actor player, AIHints hints)");
            sb.AppendLine("    {");
            foreach (var g in heals)
            {
                var first = g.First();
                var top = Math.Min(0.95f, g.Max(h => h.HPRatio) + 0.05f);
                var at = string.Join(", ", g.Select(h => (h.HPRatio * 100).ToString("0", inv) + "%").Take(8));
                sb.AppendLine($"        // used {names.ActionName(g.Key.ActionId) ?? g.Key.ActionId.ToString(inv)} on {Name(first.Target)} {g.Count()} time(s), at {at} HP");
                sb.AppendLine(string.Create(inv, $"        if (World.Actors.FirstOrDefault(a => a.OID == 0x{g.Key.OID:X}u && a.IsTargetable && !a.IsDead) is {{ }} ally{g.Key.OID:X} && ally{g.Key.OID:X}.HPMP.MaxHP > 0 && ally{g.Key.OID:X}.HPRatio < {top:0.00}f)"));
                sb.AppendLine($"            hints.ActionsToExecute.Push(ActionID.MakeSpell({g.Key.ActionId}u), ally{g.Key.OID:X}, ActionQueue.Priority.High);");
            }
            sb.AppendLine("    }");
        }
        sb.AppendLine("}");

        sb.AppendLine();
        if (cap.RolePlaying || cap.ActionsUsed.Count > 0)
        {
            sb.AppendLine(cap.RolePlaying
                ? "// The player was role-playing someone (status RolePlaying): the character needs a kit (QuestBattle.UnmanagedRotation)"
                : "// Actions the player used, for reference:");
            if (cap.RolePlaying)
                sb.AppendLine("// built from the actions below, run from AddQuestAIHints. Most used first:");
            foreach (var (id, n) in cap.ActionsUsed.OrderByDescending(kv => kv.Value))
                sb.AppendLine($"//   {id,6} {names.ActionName(id) ?? "?"} x{n}");
        }
        if (cap.DirectorUpdates.Count > 0)
        {
            sb.AppendLine("// Director updates, for an objective that should end on one (obj.OnDirectorUpdate):");
            foreach (var d in cap.DirectorUpdates.Take(40))
                sb.AppendLine(string.Create(inv, $"//   {T(d.At),7}  UpdateID 0x{d.UpdateID:X8}  Param1 0x{d.P1:X}  Param2 0x{d.P2:X}"));
        }
        return sb.ToString();
    }

    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    /// <summary>Ramer-Douglas-Peucker in X/Z: the corners of a walk, without the hundred points along each straight.</summary>
    public static List<Vector3> Simplify(List<Vector3> points, float tolerance)
    {
        if (points.Count <= 2)
            return [.. points];
        var keep = new bool[points.Count];
        keep[0] = keep[^1] = true;
        var stack = new Stack<(int, int)>();
        stack.Push((0, points.Count - 1));
        while (stack.Count > 0)
        {
            var (a, b) = stack.Pop();
            var best = -1;
            var bestD = tolerance;
            for (var i = a + 1; i < b; ++i)
            {
                var d = DistanceToSegment(points[i], points[a], points[b]);
                if (d > bestD)
                {
                    bestD = d;
                    best = i;
                }
            }
            if (best >= 0)
            {
                keep[best] = true;
                stack.Push((a, best));
                stack.Push((best, b));
            }
        }
        var result = new List<Vector3>();
        for (var i = 0; i < points.Count; ++i)
            if (keep[i])
                result.Add(points[i]);
        return result;
    }

    private static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        var ab = new Vector2(b.X - a.X, b.Z - a.Z);
        var ap = new Vector2(p.X - a.X, p.Z - a.Z);
        var len = ab.LengthSquared();
        var t = len > 0 ? Math.Clamp(Vector2.Dot(ap, ab) / len, 0f, 1f) : 0f;
        // height counts too: a ramp or a stair is a corner the navmesh must be told about
        var flat = (ap - (ab * t)).Length();
        var dy = Math.Abs(p.Y - (a.Y + ((b.Y - a.Y) * t)));
        return MathF.Max(flat, dy);
    }
}
