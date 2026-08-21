namespace Minerva.Components;

/// <summary>
/// A bait whose victims are chosen by proximity rather than by tether or marker: the N nearest (or
/// farthest) eligible players get the AOE, so who is baiting changes as people move. Supports
/// stack-flavoured baits, where the same shape must be shared rather than avoided. Ported from
/// BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class GenericBaitProximity(ModuleBase module, bool alwaysDrawOtherBaits = true, bool onlyShowOutlines = false) : CastCounter(module, default)
{
    public struct Bait(WPos source, AOEShape shape, DateTime activation = default, int numTargets = 1, bool nearest = true, bool stack = false, int minStack = 1, int maxStack = 1, bool centerAtTarget = false, bool tankbuster = false, uint caster = default, Role role = Role.None, BitMask forbidden = default, Angle? customRotation = null, WDir offset = default)
    {
        public WPos Position = source;
        public AOEShape Shape = shape;
        public DateTime Activation = activation;
        public bool FromNearest = nearest;
        public int NumTargets = numTargets;
        public bool IsStack = stack;
        public int MinStack = minStack;
        public int MaxStack = maxStack;
        public Role SpecifiedRole = role;
        public BitMask ForbiddenPlayers = forbidden; // players who should not be picked
        public bool CenterAtTarget = centerAtTarget;
        public WDir Offset = offset;
        public bool IsTankbuster = tankbuster;
        public uint CasterID = caster;
        public Angle? CustomRotation = customRotation;

        public Bait(Actor source, AOEShape shape, DateTime activation = default, int numTargets = 1, bool nearest = true, bool stack = false, int minStack = 1, int maxStack = 1, bool centerAtTarget = false, bool tankbuster = false, uint caster = default, Role role = Role.None, BitMask forbidden = default, Angle? customRotation = null, WDir offset = default)
            : this(source.Position, shape, activation, numTargets, nearest, stack, minStack, maxStack, centerAtTarget, tankbuster, caster, role, forbidden, customRotation, offset) { }
    }

    public readonly bool AlwaysDrawOtherBaits = alwaysDrawOtherBaits;
    public bool OnlyShowOutlines = onlyShowOutlines;
    public bool AllowDeadTargets;
    public bool EnableHints = true;
    public bool IgnoreOtherBaits;
    public List<Bait> CurrentBaits = [];

    public const string BaitAwayHint = "Bait away from raid!";
    public const string BaitAOEHint = "GTFO from baited AOE!";

    /// <summary>Baits whose caster is still alive and which have at least one live target.</summary>
    public List<Bait> ActiveBaits
    {
        get
        {
            var active = new List<Bait>(this.CurrentBaits.Count);
            foreach (var bait in this.CurrentBaits)
            {
                if (bait.CasterID != default)
                {
                    var caster = this.Module.Enemies(bait.CasterID);
                    if (caster.Count == 0 || caster[0].IsDeadOrDestroyed)
                        continue;
                }
                foreach (var t in this.GetTargets(bait))
                {
                    if (this.AllowDeadTargets || !t.IsDead)
                    {
                        active.Add(bait);
                        break;
                    }
                }
            }
            return active;
        }
    }

    /// <summary>
    /// The players this bait currently lands on: eligible by role and not forbidden, sorted by distance
    /// from the bait's origin, taking the nearest (or farthest) <c>NumTargets</c>.
    /// </summary>
    public List<Actor> GetTargets(Bait bait)
    {
        var candidates = new List<(Actor actor, float distSq)>();
        foreach (var (slot, actor) in this.World.Party.WithSlot(this.AllowDeadTargets))
        {
            if (bait.ForbiddenPlayers[slot])
                continue;
            if (bait.SpecifiedRole != Role.None && actor.Role != bait.SpecifiedRole)
                continue;
            candidates.Add((actor, (actor.Position - bait.Position).LengthSq()));
        }

        candidates.Sort((a, b) => bait.FromNearest ? a.distSq.CompareTo(b.distSq) : b.distSq.CompareTo(a.distSq));
        var take = Math.Min(bait.NumTargets, candidates.Count);
        var result = new List<Actor>(take);
        for (var i = 0; i < take; ++i)
            result.Add(candidates[i].actor);
        return result;
    }

    public WPos BaitOrigin(in Bait bait, Actor target) => (bait.CenterAtTarget ? target.Position : bait.Position) + bait.Offset;
    public Angle BaitRotation(in Bait bait, Actor target) => bait.CustomRotation ?? Angle.FromDirection(target.Position - bait.Position);
    public bool IsClippedBy(Actor pc, in Bait bait, Actor target) => bait.Shape.Check(pc.Position, this.BaitOrigin(in bait, target), this.BaitRotation(in bait, target));
    public bool IsBaitTarget(in Bait bait, Actor target) => this.GetTargets(bait).Exists(t => t.InstanceID == target.InstanceID);

    /// <summary>Party members other than the bait's own target standing inside its shape.</summary>
    public List<Actor> PlayersClippedBy(in Bait bait, Actor target)
    {
        var origin = this.BaitOrigin(in bait, target);
        var rotation = this.BaitRotation(in bait, target);
        var result = new List<Actor>();
        foreach (var actor in this.World.Party.WithoutSlot())
            if (actor.InstanceID != target.InstanceID && bait.Shape.Check(actor.Position, origin, rotation))
                result.Add(actor);
        return result;
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (!this.EnableHints)
            return;

        foreach (var b in this.ActiveBaits)
        {
            if (this.IsBaitTarget(in b, actor))
            {
                var clipped = this.PlayersClippedBy(in b, actor).Count;
                if (b.IsStack)
                {
                    var inStack = clipped + 1; // count the baiter
                    if (inStack < b.MinStack)
                        hints.Add("Not enough in stack!");
                    else if (inStack > b.MaxStack)
                        hints.Add("Too many in stack!");
                    else
                        hints.Add("Stack!", false);
                    return;
                }
                if (clipped != 0)
                {
                    hints.Add(BaitAwayHint);
                    return;
                }
            }
            else if (!this.IgnoreOtherBaits)
            {
                foreach (var target in this.GetTargets(b))
                {
                    if (target != actor && this.IsClippedBy(actor, in b, target))
                    {
                        hints.Add(b.IsStack ? "Stack!" : BaitAOEHint);
                        return;
                    }
                }
            }
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        foreach (var b in this.ActiveBaits)
        {
            if (b.IsStack || this.IsBaitTarget(in b, actor))
                continue; // stacks are approached, and your own bait can't be dodged
            foreach (var target in this.GetTargets(b))
                if (target != actor)
                    hints.AddForbiddenZone(b.Shape, this.BaitOrigin(in b, target), this.BaitRotation(in b, target), b.Activation);
        }
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        foreach (var b in this.ActiveBaits)
        {
            foreach (var target in this.GetTargets(b))
            {
                var isOwn = target == pc;
                if (!isOwn && !this.AlwaysDrawOtherBaits && !this.IsClippedBy(pc, in b, target))
                    continue;
                var origin = this.BaitOrigin(in b, target);
                var rotation = this.BaitRotation(in b, target);
                if (isOwn || this.OnlyShowOutlines)
                    this.Arena.OutlineShape(b.Shape, origin, rotation, b.IsStack ? Colors.Safe : Colors.Danger);
                else
                    this.Arena.ZoneShape(b.Shape, origin, rotation, b.IsStack ? Colors.Safe : Colors.AOE);
            }
        }
    }
}
