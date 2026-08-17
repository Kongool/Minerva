using System.Runtime.InteropServices;

namespace Minerva.Components;

/// <summary>
/// Generic "soak the tower" mechanic: a set of zones that need a specific number of players standing in
/// them. Warns the local player to soak a tower or get out of a forbidden one, and draws each tower
/// (green when it still needs soakers). Ported from BossmodReborn's GenericTowers (BSD-3; see
/// THIRD-PARTY-NOTICES.txt), simplified for Minerva (no ShapeDistance / predicted-damage AI weighting).
/// </summary>
public class GenericTowers(ModuleBase module, uint aid = default, bool prioritizeInsufficient = false) : CastCounter(module, aid)
{
    public struct Tower
    {
        public Tower(WPos position, float radius, int minSoakers = 1, int maxSoakers = 1, BitMask forbiddenSoakers = default, DateTime activation = default, ulong actorID = default)
            : this(position, new AOEShapeCircle(radius), minSoakers, maxSoakers, forbiddenSoakers, activation, default, actorID) { }

        public Tower(WPos position, AOEShape shape, int minSoakers = 1, int maxSoakers = 1, BitMask forbiddenSoakers = default, DateTime activation = default, Angle rotation = default, ulong actorID = default)
        {
            this.Position = position;
            this.Shape = shape;
            this.MinSoakers = minSoakers;
            this.MaxSoakers = maxSoakers;
            this.ForbiddenSoakers = forbiddenSoakers;
            this.Activation = activation;
            this.Rotation = rotation;
            this.ActorID = actorID;
        }

        public WPos Position;
        public Angle Rotation;
        public AOEShape Shape;
        public int MinSoakers;
        public int MaxSoakers;
        public BitMask ForbiddenSoakers;
        public DateTime Activation;
        public ulong ActorID;

        public readonly bool IsInside(WPos pos) => this.Shape.Check(pos, this.Position, this.Rotation);
        public readonly bool IsInside(Actor actor) => this.IsInside(actor.Position);

        /// <summary>How many (non-forbidden) party members currently stand in this tower.</summary>
        public readonly int NumInside(ModuleBase module)
        {
            var count = 0;
            for (var i = 0; i < PartyState.MaxSlots; ++i)
            {
                if (this.ForbiddenSoakers[i])
                    continue;
                var a = module.World.Party.Actor(i);
                if (a != null && this.IsInside(a))
                    ++count;
            }
            return count;
        }

        public readonly bool CorrectAmountInside(ModuleBase module) => this.NumInside(module) is var c && c >= this.MinSoakers && c <= this.MaxSoakers;
        public readonly bool InsufficientAmountInside(ModuleBase module) => this.NumInside(module) < this.MaxSoakers;
    }

    public List<Tower> Towers = [];
    public readonly bool PrioritizeInsufficient = prioritizeInsufficient;

    public virtual ReadOnlySpan<Tower> ActiveTowers(int slot, Actor actor) => CollectionsMarshal.AsSpan(this.Towers);

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var towers = this.ActiveTowers(slot, actor);
        if (towers.Length == 0)
            return;

        foreach (ref readonly var t in towers)
        {
            if (t.ForbiddenSoakers[slot] && t.IsInside(actor))
            {
                hints.Add("GTFO from tower!");
                return;
            }
        }

        // are we standing in a tower we're allowed to soak?
        foreach (ref readonly var t in towers)
        {
            if (!t.ForbiddenSoakers[slot] && t.IsInside(actor))
            {
                var count = t.NumInside(this.Module);
                if (count < t.MinSoakers)
                    hints.Add("Too few soakers in the tower!");
                else if (count > t.MaxSoakers)
                    hints.Add("Too many soakers in the tower!");
                else
                    hints.Add("Soak the tower!", false);
                return;
            }
        }

        // not in any tower — is there one still short of soakers we could take?
        foreach (ref readonly var t in towers)
        {
            if (!t.ForbiddenSoakers[slot] && t.InsufficientAmountInside(this.Module))
            {
                hints.Add("Soak the tower!");
                return;
            }
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        var towers = this.ActiveTowers(pcSlot, pc);
        foreach (ref readonly var t in towers)
        {
            if (t.ForbiddenSoakers[pcSlot])
            {
                this.Arena.OutlineShape(t.Shape, t.Position, t.Rotation, Colors.Danger, 2f);
                continue;
            }
            var numInside = t.NumInside(this.Module);
            var safe = numInside < t.MaxSoakers || (t.IsInside(pc) && numInside <= t.MaxSoakers);
            this.Arena.OutlineShape(t.Shape, t.Position, t.Rotation, safe ? Colors.Safe : Colors.Danger, 2f);
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var towers = this.ActiveTowers(slot, actor);
        foreach (ref readonly var t in towers)
            if (t.ForbiddenSoakers[slot])
                hints.AddForbiddenZone(t.Shape, t.Position, t.Rotation, t.Activation);
    }
}

/// <summary>
/// Towers driven by a cast: each cast of <c>WatchedAction</c> at its target location spawns a circular
/// tower needing <paramref name="minSoakers"/>–<paramref name="maxSoakers"/> players. One line per
/// mechanic. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class CastTowers(ModuleBase module, uint aid, float radius, int minSoakers = 1, int maxSoakers = 1) : GenericTowers(module, aid)
{
    public readonly float Radius = radius;
    public readonly int MinSoakers = minSoakers;
    public readonly int MaxSoakers = maxSoakers;

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction)
            this.Towers.Add(new Tower(cast.LocXZ, this.Radius, this.MinSoakers, this.MaxSoakers, activation: this.Module.CastFinishAt(cast), actorID: caster.InstanceID));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID != this.WatchedAction)
            return;
        for (var i = 0; i < this.Towers.Count; ++i)
        {
            if (this.Towers[i].ActorID == caster.InstanceID)
            {
                this.Towers.RemoveAt(i);
                return;
            }
        }
    }
}
