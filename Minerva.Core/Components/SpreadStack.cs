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
        // keep away from spread markers other players carry, and from forbidden stacks
        foreach (var s in this.ActiveSpreads)
            if (s.Target != actor)
                hints.AddForbiddenZone(new AOEShapeCircle(s.Radius + this.ExtraAISpreadThreshold), s.Target.Position, default, s.Activation);
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
