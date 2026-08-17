using System.Globalization;
using System.Text;

namespace Minerva.Generation;

/// <summary>Outcome of a generation run: the module source plus a coverage summary.</summary>
public sealed record GenerationResult(string Code, string Report, int Total, int Drawn, int Special, int NeedsReview);

/// <summary>
/// Turns correlated replay facts into a compilable Minerva module. It names enums from game data
/// (<see cref="INameResolver"/>), sizes shapes from game data (<see cref="IShapeResolver"/>), and
/// classifies each cast using the correlation the analyzer produced — cast shape, player-target
/// counts, preceding icons/tethers, and object lifetimes — into the right component
/// (SimpleAOEs / RaidwideCast / Spread / Stack / Bait / Tankbuster / Voidzone). Detected phases are
/// grouped in the state machine. Anything the rules can't pin down is a compiling stub with a TODO,
/// never a silent guess. Deterministic and offline — no LLM.
/// </summary>
public sealed class ModuleGenerator(IShapeResolver? shapeResolver = null, INameResolver? nameResolver = null)
{
    private const uint HelperOID = 0x233C;
    private const float RaidwideCircleRadius = 35f;

    private readonly IShapeResolver shapes = shapeResolver ?? new NullShapeResolver();
    private readonly INameResolver names = nameResolver ?? new NullNameResolver();
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    // allocates unique C# identifiers (shared by an AID enum member and its component class, BMR-style)
    private sealed class NameAllocator
    {
        private readonly HashSet<string> used = [];

        public string Alloc(string? name, string fallback)
        {
            var ident = Sanitize(name);
            if (ident.Length == 0)
                ident = fallback;
            var candidate = ident;
            var n = 2;
            while (!this.used.Add(candidate))
                candidate = ident + n++;
            return candidate;
        }

        private static string Sanitize(string? s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            var b = new StringBuilder(s.Length);
            var upNext = true;
            foreach (var ch in s)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    b.Append(upNext ? char.ToUpperInvariant(ch) : ch);
                    upNext = false;
                }
                else
                {
                    upNext = true; // word boundary -> PascalCase
                }
            }
            if (b.Length > 0 && char.IsDigit(b[0]))
                b.Insert(0, '_');
            return b.ToString();
        }
    }

    public GenerationResult Generate(GenerationInput input)
    {
        var className = $"D{input.CFCID}";
        var ns = $"Minerva.Generated.{className}";

        var aidNames = new NameAllocator();
        var aidMember = new Dictionary<uint, string>();
        foreach (var a in input.Actions)
            aidMember[a.AID] = aidNames.Alloc(this.names.ActionName(a.AID), $"A{a.AID}");

        int drawn = 0, special = 0, review = 0;
        var components = new StringBuilder();
        var todos = new List<string>();
        var activationsByPhase = new Dictionary<int, List<string>>();

        void Activate(int phase, string name)
        {
            if (!activationsByPhase.TryGetValue(phase, out var list))
                activationsByPhase[phase] = list = [];
            list.Add(name);
        }

        // per-cast components
        foreach (var act in input.Actions)
        {
            var member = aidMember[act.AID];
            var (line, kind, needsReview) = this.EmitComponent(member, act);
            components.AppendLine(line);
            Activate(act.Phase, member);
            switch (kind)
            {
                case "draw": drawn++; break;
                case "special": special++; break;
            }
            if (needsReview)
            {
                review++;
                todos.Add($"{member} ({act.CasterName}->{act.Target}{(act.PlayerMechanic != PlayerMechanic.None ? $"/{act.PlayerMechanic}" : "")}, {act.CastTime.ToString("0.#", Inv)}s): confirm");
            }
        }

        // voidzone components from lingering hazard objects
        var oidNames = new NameAllocator();
        oidNames.Alloc("Boss", "Boss"); // reserve
        var oidMember = new Dictionary<uint, string>();
        foreach (var o in input.Objects)
            oidMember[o.OID] = o.OID == input.BossOID ? "Boss"
                : o.OID == HelperOID ? "Helper"
                : oidNames.Alloc(this.names.ObjectName(o.OID) ?? o.Name, $"_{o.OID:X}");

        var voidzones = 0;
        foreach (var o in input.Objects)
        {
            if (!o.VoidzoneCandidate)
                continue;
            var vz = $"{oidMember[o.OID]}Voidzone";
            components.AppendLine($"sealed class {vz}(ModuleBase module) : Components.Voidzone(module, {F(o.HitboxRadius)}f, (uint)OID.{oidMember[o.OID]}); // TODO: confirm voidzone radius");
            Activate(0, vz);
            voidzones++;
            review++;
            todos.Add($"{vz}: voidzone radius is an estimate from hitbox");
        }

        // arena-change scaffold: an environment object appeared mid-fight — a replay can't tell us the new
        // bounds, so emit a commented ArenaChange for the author to fill in, never a fabricated one
        foreach (var o in input.Objects)
        {
            if (!o.IsArenaMarker)
                continue;
            components.AppendLine($"// arena may change when '{o.Name}' (OID 0x{o.OID:X}) appears — set the new bounds and uncomment + ActivateOnEnter:");
            components.AppendLine($"// sealed class Arena{oidMember[o.OID]}(ModuleBase module) : Components.ArenaChange(module, new ArenaBoundsCircle(20f), triggerOID: (uint)OID.{oidMember[o.OID]});");
            review++;
            todos.Add($"possible arena change on '{o.Name}' (OID.{oidMember[o.OID]}) — add Components.ArenaChange with the real bounds if the field shrinks/breaks");
        }

        var code = new StringBuilder();
        code.AppendLine(this.Header(input, drawn, special + voidzones, review, todos));
        code.AppendLine("using System;");
        code.AppendLine("using Minerva;");
        code.AppendLine();
        code.AppendLine($"namespace {ns};");
        code.AppendLine();
        code.AppendLine(this.OidEnum(input, oidMember));
        code.AppendLine();
        code.AppendLine(this.AidEnum(input, aidMember));
        code.AppendLine();
        code.Append(components);
        code.AppendLine();
        code.AppendLine(this.StatesClass(className, input, activationsByPhase, oidMember));
        code.AppendLine();
        code.AppendLine(this.ModuleClass(className, input));

        var report = this.Report(input, drawn, special, voidzones, review);
        return new GenerationResult(code.ToString(), report, input.Actions.Count, drawn, special + voidzones, review);
    }

    private (string line, string kind, bool review) EmitComponent(string name, ActionFact act)
    {
        var aidRef = $"(uint)AID.{name}";
        var hint = this.shapes.Resolve(act.AID);

        // player-targeted mechanics classified from correlation
        if (act.Target == TargetKind.Player)
        {
            // a rectangle aimed at a player is a line-stack — the party lines up to share it
            if (hint.Kind == ShapeKind.Rect)
            {
                var lineHalfWidth = hint.HalfWidth > 0f ? hint.HalfWidth : 4f;
                var lineLength = hint.Radius > 0f ? hint.Radius : 50f;
                var todo = hint.NeedsReview ? " // TODO: confirm line width/length" : "";
                return ($"sealed class {name}(ModuleBase module) : Components.LineStack(module, {aidRef}, {F(lineHalfWidth)}f, {F(lineLength)}f);{todo}", "special", hint.NeedsReview);
            }
            switch (act.PlayerMechanic)
            {
                case PlayerMechanic.Bait when act.PrecedingTether != 0:
                    return ($"sealed class {name}(ModuleBase module) : Components.BaitAwayTethers(module, {act.PrecedingTether}u); // tether-bait", "special", false);
                case PlayerMechanic.Stack when act.PrecedingIcon != 0:
                    return ($"sealed class {name}(ModuleBase module) : Components.StackWithIcon(module, {act.PrecedingIcon}u, {SpreadRadius(hint)}f); // icon stack", "special", false);
                case PlayerMechanic.Stack:
                    return ($"sealed class {name}(ModuleBase module) : Components.StackWithCastTargets(module, {aidRef}, {SpreadRadius(hint)}f);", "special", false);
                case PlayerMechanic.Spread when act.PrecedingIcon != 0:
                    return ($"sealed class {name}(ModuleBase module) : Components.SpreadFromIcon(module, {act.PrecedingIcon}u, {SpreadRadius(hint)}f); // icon spread", "special", true);
                case PlayerMechanic.Spread:
                    return ($"sealed class {name}(ModuleBase module) : Components.SpreadFromCastTargets(module, {aidRef}, {SpreadRadius(hint)}f);", "special", true);
                case PlayerMechanic.Tankbuster:
                    return ($"sealed class {name}(ModuleBase module) : Components.CastHint(module, {aidRef}, \"{name}: tankbuster\"); // TODO: tankbuster component", "special", true);
                default:
                    return ($"sealed class {name}(ModuleBase module) : Components.CastHint(module, {aidRef}, \"{name}: TODO (player-targeted)\");", "stub", true);
            }
        }

        // big self-targeted circle => raidwide (don't draw a whole-arena circle)
        if (hint.Kind == ShapeKind.Circle && act.Target == TargetKind.Self && hint.Radius >= RaidwideCircleRadius)
            return ($"sealed class {name}(ModuleBase module) : Components.RaidwideCast(module, {aidRef});", "special", false);

        var shapeExpr = hint.ToShapeExpression();
        if (shapeExpr != null)
        {
            var confirm = hint.NeedsReview ? " (confirm shape)" : "";
            // tether-telegraphed AOE (leash, then the tethered target erupts) — draws on the target, not the caster
            if (act.PrecedingTether != 0)
                return ($"sealed class {name}(ModuleBase module) : Components.TetherAOEs(module, {act.PrecedingTether}u, {aidRef}, {shapeExpr}); // tether {act.PrecedingTether}: erupts on the tether target — confirm target vs source + delay{confirm}", "special", true);
            // one ring of a same-origin bullseye — draws fine per-cast, flag it for optional merging
            if (act.ConcentricCandidate)
                return ($"sealed class {name}(ModuleBase module) : Components.SimpleAOEs(module, {aidRef}, {shapeExpr}); // concentric? one ring of a same-origin bullseye — consider merging into Components.ConcentricAOEs{confirm}", "draw", true);
            // a marching line of location casts draws fine as per-cast AOEs, but flag it so the author
            // can upgrade to SimpleExaflare for genuine look-ahead
            if (act.ExaflareCandidate)
                return ($"sealed class {name}(ModuleBase module) : Components.SimpleAOEs(module, {aidRef}, {shapeExpr}); // exaflare? repeated marching casts — consider Components.SimpleExaflare for look-ahead{confirm}", "draw", true);
            var todo = hint.NeedsReview ? " // TODO: confirm shape" : "";
            return ($"sealed class {name}(ModuleBase module) : Components.SimpleAOEs(module, {aidRef}, {shapeExpr});{todo}", "draw", hint.NeedsReview);
        }

        // no ground shape resolved, but correlation points at a positional knockback or a look-away gaze
        if (act.KnockbackDistance > 0f)
            return ($"sealed class {name}(ModuleBase module) : Components.SimpleKnockbacks(module, {aidRef}, {F(act.KnockbackDistance)}f); // TODO: confirm knockback distance", "special", true);
        if (act.GazeCandidate)
            return ($"sealed class {name}(ModuleBase module) : Components.Gaze(module, {aidRef}); // TODO: confirm gaze (look away)", "special", true);

        return ($"sealed class {name}(ModuleBase module) : Components.CastHint(module, {aidRef}, \"{name}: TODO (unknown shape)\"); // TODO: replace with the right component", "stub", true);
    }

    private static float SpreadRadius(ShapeHint hint) => hint.Kind == ShapeKind.Circle && hint.Radius > 0f ? hint.Radius : 6f;

    private string OidEnum(GenerationInput input, Dictionary<uint, string> member)
    {
        var b = new StringBuilder();
        b.AppendLine("public enum OID : uint");
        b.AppendLine("{");
        b.AppendLine($"    Boss = 0x{input.BossOID:X},");
        foreach (var o in input.Objects)
        {
            if (o.OID == input.BossOID)
                continue;
            var vz = o.VoidzoneCandidate ? " [voidzone?]" : "";
            b.AppendLine($"    {member[o.OID]} = 0x{o.OID:X}, // '{o.Name}' R{o.HitboxRadius.ToString("0.##", Inv)}{vz}");
        }
        b.Append('}');
        return b.ToString();
    }

    private string AidEnum(GenerationInput input, Dictionary<uint, string> member)
    {
        var b = new StringBuilder();
        b.AppendLine("public enum AID : uint");
        b.AppendLine("{");
        foreach (var a in input.Actions)
        {
            var mech = a.PlayerMechanic != PlayerMechanic.None ? $" [{a.PlayerMechanic}]" : "";
            b.AppendLine($"    {member[a.AID]} = {a.AID}, // {a.CasterName}->{a.Target}{mech}, {a.CastTime.ToString("0.#", Inv)}s cast, x{a.Count}, P{a.Phase + 1}");
        }
        b.Append('}');
        return b.ToString();
    }

    private string StatesClass(string className, GenerationInput input, Dictionary<int, List<string>> byPhase, Dictionary<uint, string> oidMember)
    {
        var phaseCount = Math.Max(1, input.Phases.Count);

        // fold any activations parked beyond the detected phase count into the last real phase (safety)
        foreach (var (p, list) in byPhase)
            if (p >= phaseCount && p != phaseCount - 1)
            {
                if (!byPhase.TryGetValue(phaseCount - 1, out var last))
                    byPhase[phaseCount - 1] = last = [];
                last.AddRange(list);
            }

        var body = new StringBuilder();
        for (var p = 0; p < phaseCount; ++p)
        {
            // a single phase keeps the old "everything on" TrivialPhase; multiple phases are named + transitioned
            body.Append("        ").Append(phaseCount == 1 ? "this.TrivialPhase()" : $"this.Phase(\"P{p + 1}\")");
            if (byPhase.TryGetValue(p, out var list))
                foreach (var name in list)
                    body.Append("\n            .").Append($"ActivateOnEnter<{name}>()");
            // transition into the next phase from the signal that started it: a new boss form becoming
            // targetable, or the boss reaching an HP threshold (it went untargetable there and returned)
            string? comment = null;
            if (p + 1 < phaseCount)
            {
                var next = input.Phases[p + 1];
                var call = next.Trigger switch
                {
                    PhaseTrigger.PrimaryHP => $"TransitionOnPrimaryHP({F(next.TriggerHP)}f)",
                    PhaseTrigger.MapEffect => $"TransitionOnMapEffect((byte){next.TriggerMapIndex}, {next.TriggerMapState}u)",
                    _ => $"TransitionOnTargetable({(oidMember.TryGetValue(next.TriggerOID, out var nm) ? $"(uint)OID.{nm}" : $"0x{next.TriggerOID:X}u")})",
                };
                body.Append($"\n            .{call}");
                comment = " // TODO: confirm phase transition (HP %? map effect?)";
            }
            body.Append(';').Append(comment).Append('\n'); // semicolon before the comment, so the statement terminates
        }

        var bodyStr = body.ToString().TrimEnd();
        return $$"""
        sealed class {{className}}States : StateMachineBuilder
        {
            public {{className}}States(ModuleBase module) : base(module)
            {
        {{bodyStr}}
            }
        }
        """;
    }

    private string ModuleClass(string className, GenerationInput input)
    {
        var c = input.Arena.Center;
        var half = input.Arena.HalfExtent;
        var center = $"new WPos({c.X.ToString("0.#", Inv)}f, {c.Z.ToString("0.#", Inv)}f)";
        var bounds = half <= 0.1f
            ? "new ArenaBoundsSquare(20f)"
            : input.Arena.LooksSquare
                ? $"new ArenaBoundsSquare({MathF.Ceiling(half).ToString("0", Inv)}f)"
                : $"new ArenaBoundsCircle({MathF.Ceiling(half).ToString("0", Inv)}f)";

        return $$"""
        [ModuleInfo(CFCID = {{input.CFCID}}u, NameID = 0u, Maturity = ModuleMaturity.WIP, Contributors = "Minerva extractor")]
        public sealed class {{className}}(WorldState ws, Actor primary)
            : ModuleBase(ws, primary, {{center}}, {{bounds}});
        """;
    }

    private string Header(GenerationInput input, int drawn, int special, int review, List<string> todos)
    {
        var b = new StringBuilder();
        b.AppendLine("// <auto-generated by Minerva extractor>");
        if (input.BossOID == 0)
            b.AppendLine("// !! WARNING: no boss identified (open-field content, or the boss never cast). Set OID.Boss and the module's PrimaryActorOID by hand, or this module will never activate.");
        b.AppendLine($"// Duty CFC {input.CFCID} (zone {input.Zone}), boss '{input.BossName}' 0x{input.BossOID:X}, {Math.Max(1, input.Phases.Count)} phase(s).");
        b.AppendLine($"// {input.Actions.Count} actions: {drawn} AOE, {special} classified (spread/stack/bait/raidwide/voidzone), {review} need review.");
        b.AppendLine("// Correlation is heuristic — verify shapes/behaviors and test in-duty before trusting.");
        if (todos.Count > 0)
        {
            b.AppendLine("// TODO:");
            foreach (var td in todos)
                b.AppendLine($"//   - {td}");
        }
        return b.ToString().TrimEnd();
    }

    private string Report(GenerationInput input, int drawn, int special, int voidzones, int review)
    {
        var total = input.Actions.Count;
        var coverage = total > 0 ? (drawn + special) * 100 / total : 0;
        var b = new StringBuilder();
        b.AppendLine($"Extracted module for CFC {input.CFCID} ('{input.BossName}'), {Math.Max(1, input.Phases.Count)} phase(s).");
        b.AppendLine($"Actions: {total}  |  AOE: {drawn}  |  classified: {special}  |  need review: {review}  |  voidzones: {voidzones}");
        b.AppendLine($"Auto-covered: {coverage}% of actions  (the rest are compiling stubs to finish by hand).");
        b.AppendLine($"Objects: {input.Objects.Count}, statuses: {input.Statuses.Count}, tethers: {input.Tethers.Count}, icons: {input.Icons.Count}.");
        return b.ToString();
    }

    private static string F(float v) => v.ToString("0.###", Inv);
}
