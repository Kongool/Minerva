using System.Runtime.InteropServices;

namespace Minerva.Components;

/// <summary>
/// Generic stack/spread mechanic: some players must spread out (keep a radius clear), others must pile
/// into a stack. Subclasses populate <see cref="Stacks"/> / <see cref="Spreads"/>; this base draws the
/// circles, advises the local player (spread / stack / get out), and forbids standing in other players'
/// spreads for the auto-dodge engine. Ported from BossmodReborn's GenericStackSpread (BSD-3; see
/// THIRD-PARTY-NOTICES.txt), adapted to Minerva's PartyState (no ShapeDistance / grid quantization).
/// </summary>
public abstract class GenericStackSpread(ModuleBase module, bool raidwideOnResolve = true, bool includeDeadTargets = false) : ModuleComponent(module)
{
    public struct Stack(Actor target, float radius, int minSize = 2, int maxSize = int.MaxValue, DateTime activation = default, BitMask forbiddenPlayers = default)
    {
        public Actor Target = target;
        public float Radius = radius;
        public int MinSize = minSize;
        public int MaxSize = maxSize;
        public DateTime Activation = activation;
        public BitMask ForbiddenPlayers = forbiddenPlayers; // party members barred from this stack

        public readonly bool IsInside(WPos pos) => pos.InCircle(this.Target.Position, this.Radius);
        public readonly bool IsInside(Actor actor) => this.IsInside(actor.Position);

        public readonly int NumInside(ModuleBase module)
        {
            var count = 0;
            for (var i = 0; i < PartyState.MaxSlots; ++i)
            {
                if (this.ForbiddenPlayers[i])
                    continue;
                var a = module.World.Party.Actor(i);
                if (a != null && this.IsInside(a))
                    ++count;
            }
            return count;
        }
    }

    public struct Spread(Actor target, float radius, DateTime activation = default)
    {
        public Actor Target = target;
        public float Radius = radius;
        public DateTime Activation = activation;
    }

    public readonly bool RaidwideOnResolve = raidwideOnResolve;
    public readonly bool IncludeDeadTargets = includeDeadTargets;
    public int ExtraAISpreadThreshold = 1;
    public readonly List<Stack> Stacks = [];
    public List<Spread> Spreads = [];
    public const string StackHint = "Stack!";

    public bool Active => this.Stacks.Count + this.Spreads.Count > 0;

    public List<Stack> ActiveStacks => this.IncludeDeadTargets ? this.Stacks : this.Stacks.FindAll(s => !s.Target.IsDead);
    public List<Spread> ActiveSpreads => this.IncludeDeadTargets ? this.Spreads : this.Spreads.FindAll(s => !s.Target.IsDead);

    public bool IsStackTarget(Actor? actor) => this.Stacks.Exists(s => s.Target == actor);
    public bool IsSpreadTarget(Actor? actor) => this.Spreads.Exists(s => s.Target == actor);

    // party members (excluding one actor) standing within a circle
    private int CountOthersInCircle(Actor? exclude, WPos center, float radius)
    {
        var count = 0;
        for (var i = 0; i < PartyState.MaxSlots; ++i)
        {
            var a = this.World.Party.Actor(i);
            if (a != null && a != exclude && a.Position.InCircle(center, radius))
                ++count;
        }
        return count;
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var spreads = this.ActiveSpreads;
        var stacks = this.ActiveStacks;

        // primary advice: what should *this* player be doing?
        var handled = false;
        foreach (var s in spreads)
        {
            if (s.Target == actor)
            {
                hints.Add("Spread!", this.CountOthersInCircle(actor, actor.Position, s.Radius) > 0);
                handled = true;
                break;
            }
        }
        if (!handled && this.IsStackTarget(actor))
        {
            hints.Add(StackHint, false);
            handled = true;
        }
        if (!handled)
        {
            var participating = 0;
            var unsatisfied = 0;
            foreach (var s in stacks)
            {
                if (s.ForbiddenPlayers[slot])
                    continue;
                if (s.IsInside(actor))
                {
                    ++participating;
                    continue;
                }
                if (1 + this.CountOthersInCircle(s.Target, s.Target.Position, s.Radius) < s.MinSize)
                    ++unsatisfied;
            }
            if (participating > 1)
                hints.Add(StackHint);
            else if (participating == 1)
                hints.Add(StackHint, false);
            else if (unsatisfied > 0)
                hints.Add(StackHint);
        }

        // secondary advice: get out of a spread / forbidden stack you shouldn't be in
        foreach (var s in spreads)
        {
            if (s.Target != actor && s.Target.Position.InCircle(actor.Position, s.Radius))
            {
                hints.Add("GTFO from spreads!");
                return;
            }
        }
        foreach (var s in stacks)
        {
            if (s.Target != actor && s.ForbiddenPlayers[slot] && s.IsInside(actor))
            {
                hints.Add("GTFO from forbidden stacks!");
                return;
            }
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var s in this.ActiveSpreads)
            this.Arena.AddCircle(s.Target.Position, s.Radius, Colors.Danger, 2f);
        foreach (var s in this.ActiveStacks)
            this.Arena.AddCircle(s.Target.Position, s.Radius, Colors.Safe, 2f);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        // Spreads cut both ways. Staying clear of everyone else's marker is only half of it: while *you* carry
        // one, your own circle is the danger to anybody standing near you, so keep off unmarked teammates too.
        // Other spread targets are exempt — they are already being pushed away from you by the same rule, and
        // forbidding each other's ground as well would leave two markers with nowhere legal to stand.
        foreach (var s in this.ActiveSpreads)
        {
            if (s.Target != actor)
            {
                hints.AddForbiddenZone(new AOEShapeCircle(s.Radius + this.ExtraAISpreadThreshold), s.Target.Position, default, s.Activation);
                continue;
            }

            foreach (var (_, mate) in this.Raid.WithSlot(this.IncludeDeadTargets))
            {
                if (mate == actor || this.IsSpreadTarget(mate))
                    continue;
                hints.AddForbiddenZone(new AOEShapeCircle(s.Radius + this.ExtraAISpreadThreshold), mate.Position, default, s.Activation);
            }
        }
        foreach (var s in this.ActiveStacks)
            if (s.Target != actor && s.ForbiddenPlayers[slot])
                hints.AddForbiddenZone(new AOEShapeCircle(s.Radius), s.Target.Position, default, s.Activation);
    }
}

/// <summary>
/// A <see cref="GenericStackSpread"/> where all stacks share one radius and all spreads another — the
/// common case. Subclasses add stacks/spreads by target with <see cref="AddStack"/> / <see cref="AddSpread"/>
/// (e.g. from an icon or status). Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public abstract class UniformStackSpread(ModuleBase module, float stackRadius, float spreadRadius, int minStackSize = 2, int maxStackSize = int.MaxValue, bool raidwideOnResolve = true, bool includeDeadTargets = false)
    : GenericStackSpread(module, raidwideOnResolve, includeDeadTargets)
{
    public float StackRadius = stackRadius;
    public float SpreadRadius = spreadRadius;
    public int MinStackSize = minStackSize;
    public int MaxStackSize = maxStackSize;

    public void AddStack(Actor target, DateTime activation = default, BitMask forbiddenPlayers = default)
        => this.Stacks.Add(new Stack(target, this.StackRadius, this.MinStackSize, this.MaxStackSize, activation, forbiddenPlayers));
    public void AddStacks(IEnumerable<Actor> targets, DateTime activation = default)
    {
        foreach (var target in targets)
            this.AddStack(target, activation);
    }
    public void AddSpread(Actor target, DateTime activation = default) => this.Spreads.Add(new Spread(target, this.SpreadRadius, activation));
    public void AddSpreads(IEnumerable<Actor> targets, DateTime activation = default)
    {
        foreach (var target in targets)
            this.AddSpread(target, activation);
    }
}

/// <summary>
/// Stack/spread whose targets are selected by watched casts: a <c>stackAID</c> cast marks a stack on its
/// target, a <c>spreadAID</c> cast marks a spread. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class CastStackSpread(ModuleBase module, uint stackAID, uint spreadAID, float stackRadius, float spreadRadius, int minStackSize = 2, int maxStackSize = int.MaxValue, bool alwaysShowSpreads = false)
    : UniformStackSpread(module, stackRadius, spreadRadius, minStackSize, maxStackSize, alwaysShowSpreads)
{
    public readonly uint StackAction = stackAID;
    public readonly uint SpreadAction = spreadAID;
    public int NumFinishedStacks;
    public int NumFinishedSpreads;

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        var id = cast.Action.ID;
        if (id == this.StackAction && this.World.Actors.Find(cast.TargetID) is { } stackTarget)
            this.AddStack(stackTarget, this.Module.CastFinishAt(cast));
        else if (id == this.SpreadAction && this.World.Actors.Find(cast.TargetID) is { } spreadTarget)
            this.AddSpread(spreadTarget, this.Module.CastFinishAt(cast));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        var id = cast.Action.ID;
        if (id == this.StackAction)
        {
            for (var i = 0; i < this.Stacks.Count; ++i)
            {
                if (this.Stacks[i].Target.InstanceID == cast.TargetID)
                {
                    ++this.NumFinishedStacks;
                    this.Stacks.RemoveAt(i);
                    return;
                }
            }
        }
        else if (id == this.SpreadAction)
        {
            for (var i = 0; i < this.Spreads.Count; ++i)
            {
                if (this.Spreads[i].Target.InstanceID == cast.TargetID)
                {
                    ++this.NumFinishedSpreads;
                    this.Spreads.RemoveAt(i);
                    return;
                }
            }
        }
    }
}

/// <summary>Spread from the targets of a specific cast — one line: <c>: Components.SpreadFromCastTargets(module, aid, radius)</c>.</summary>
public class SpreadFromCastTargets(ModuleBase module, uint aid, float radius) : CastStackSpread(module, default, aid, default, radius);

/// <summary>Stack on the targets of a specific cast — one line: <c>: Components.StackWithCastTargets(module, aid, radius)</c>.</summary>
public class StackWithCastTargets(ModuleBase module, uint aid, float radius, int minStackSize = 2, int maxStackSize = int.MaxValue) : CastStackSpread(module, aid, default, radius, default, minStackSize, maxStackSize);

/// <summary>
/// Stack/spread selected by an overhead icon and resolved by a cast event. Ported from BossmodReborn
/// (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class IconStackSpread(ModuleBase module, uint stackIcon, uint spreadIcon, uint stackAID, uint spreadAID, float stackRadius, float spreadRadius, double activationDelay, int minStackSize = 2, int maxStackSize = int.MaxValue, int maxCasts = 1)
    : UniformStackSpread(module, stackRadius, spreadRadius, minStackSize, maxStackSize)
{
    public readonly uint StackIcon = stackIcon;
    public readonly uint SpreadIcon = spreadIcon;
    public readonly uint StackAction = stackAID;
    public readonly uint SpreadAction = spreadAID;
    public readonly double ActivationDelay = activationDelay;
    public int NumFinishedStacks;
    public int NumFinishedSpreads;
    public readonly int MaxCasts = maxCasts; // for stacks whose final action hits multiple times
    public int CastCounter;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == this.StackIcon)
            this.AddStack(actor, this.World.FutureTime(this.ActivationDelay));
        else if (iconID == this.SpreadIcon)
            this.AddSpread(actor, this.World.FutureTime(this.ActivationDelay));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        var aid = spell.Action.ID;
        if (aid == this.StackAction && this.StackAction != default)
        {
            if (++this.CastCounter >= this.MaxCasts)
            {
                this.CastCounter = 0;
                if (!this.RemoveByTarget(this.Stacks, spell.MainTargetID) && this.Stacks.Count != 0)
                    this.Stacks.RemoveAt(0); // self-targeted fallback
                ++this.NumFinishedStacks;
            }
        }
        else if (aid == this.SpreadAction && this.SpreadAction != default)
        {
            if (!this.RemoveByTarget(this.Spreads, spell.MainTargetID) && this.Spreads.Count != 0)
                this.Spreads.RemoveAt(0);
            ++this.NumFinishedSpreads;
        }
    }

    private bool RemoveByTarget(List<Stack> list, ulong targetID)
    {
        for (var i = 0; i < list.Count; ++i)
            if (list[i].Target.InstanceID == targetID) { list.RemoveAt(i); return true; }
        return false;
    }

    private bool RemoveByTarget(List<Spread> list, ulong targetID)
    {
        for (var i = 0; i < list.Count; ++i)
            if (list[i].Target.InstanceID == targetID) { list.RemoveAt(i); return true; }
        return false;
    }

    public override void Update()
    {
        for (var i = this.Stacks.Count - 1; i >= 0; --i)
            if (this.Stacks[i].Target.IsDead) this.Stacks.RemoveAt(i);
        for (var i = this.Spreads.Count - 1; i >= 0; --i)
            if (this.Spreads[i].Target.IsDead) this.Spreads.RemoveAt(i);
    }
}

/// <summary>Spread on players who receive a specific icon. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).</summary>
public class SpreadFromIcon(ModuleBase module, uint icon, uint aid, float radius, double activationDelay) : IconStackSpread(module, default, icon, default, aid, default, radius, activationDelay)
{
    // convenience overload matching Minerva's extractor emission (module, icon, radius): no resolving cast, default delay
    public SpreadFromIcon(ModuleBase module, uint icon, float radius, double activationDelay = 5d) : this(module, icon, default, radius, activationDelay) { }
}

/// <summary>Stack on players who receive a specific icon. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).</summary>
public class StackWithIcon(ModuleBase module, uint icon, uint aid, float radius, double activationDelay, int minStackSize = 2, int maxStackSize = int.MaxValue, int maxCasts = 1)
    : IconStackSpread(module, icon, default, aid, default, radius, default, activationDelay, minStackSize, maxStackSize, maxCasts)
{
    // convenience overload matching Minerva's extractor emission (module, icon, radius)
    public StackWithIcon(ModuleBase module, uint icon, float radius, double activationDelay = 5d) : this(module, icon, default, radius, activationDelay) { }
}

/// <summary>
/// A donut centred on each marked player — the danger is the ring, so the party solves it by stacking
/// tightly on top of the targets (everyone inside everyone else's inner circle). Regular stack
/// components don't fit because the AOE is self-targeted. Ported from BossmodReborn (BSD-3; see
/// THIRD-PARTY-NOTICES.txt).
/// </summary>
public class DonutStack(ModuleBase module, uint aid, uint icon, float innerRadius, float outerRadius, double activationDelay, int minStackSize = 2, int maxStackSize = int.MaxValue)
    : UniformStackSpread(module, innerRadius / 3f, default, minStackSize, maxStackSize)
{
    public readonly AOEShapeDonut Donut = new(innerRadius, outerRadius);
    public readonly double ActivationDelay = activationDelay;
    public readonly uint Icon = icon;
    public readonly uint Aid = aid;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == this.Icon)
            this.AddStack(actor, this.World.FutureTime(this.ActivationDelay));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent cast)
    {
        if (cast.Action.ID != this.Aid)
            return;
        var t = cast.MainTargetID;
        for (var i = 0; i < this.Stacks.Count; ++i)
        {
            if (this.Stacks[i].Target.InstanceID == t)
            {
                this.Stacks.RemoveAt(i);
                return;
            }
        }
        this.Stacks.Clear(); // no match: the donut was self-targeted rather than player-targeted, so clear all
    }

    public override void Update()
    {
        for (var i = this.Stacks.Count - 1; i >= 0; --i)
            if (this.Stacks[i].Target.IsDead)
                this.Stacks.RemoveAt(i);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (this.Stacks.Count == 0)
            return;
        // stay inside every *other* target's inner circle at once — the intersection is the safe huddle
        var forbidden = new List<ShapeDistance>(this.Stacks.Count);
        var radius = this.Donut.InnerRadius * 0.25f;
        foreach (var s in this.Stacks)
            if (s.Target != actor)
                forbidden.Add(new SDInvertedCircle(s.Target.Position, radius));
        if (forbidden.Count != 0)
            hints.AddForbiddenZone(new SDIntersection([.. forbidden]), this.Stacks[0].Activation);
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        foreach (var s in this.Stacks)
            this.Arena.ZoneShape(this.Donut, s.Target.Position, default, Colors.AOE);
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc) { }
}

/// <summary>
/// Stack/spread driven by statuses that resolve on expiry rather than by casts or icons. Ported from
/// BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class StatusStackSpread(ModuleBase module, uint stackSid, uint spreadSid, float stackRadius, float spreadRadius, int minStackSize = 2, int maxStackSize = int.MaxValue, bool raidwideOnResolve = true, bool includeDeadTargets = false)
    : UniformStackSpread(module, stackRadius, spreadRadius, minStackSize, maxStackSize, raidwideOnResolve, includeDeadTargets)
{
    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == stackSid)
            this.AddStack(actor, status.ExpireAt);
        else if (status.ID == spreadSid)
            this.AddSpread(actor, status.ExpireAt);
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == stackSid)
            this.Stacks.Clear();
        else if (status.ID == spreadSid)
            this.Spreads.Clear();
    }
}
