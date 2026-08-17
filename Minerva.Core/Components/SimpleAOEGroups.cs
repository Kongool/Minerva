using System.Runtime.InteropServices;

namespace Minerva.Components;

/// <summary>
/// Like <see cref="SimpleAOEs"/> but watches several actions that all share one shape — the single most
/// common AOE component in ported modules. Add one line listing the action ids:
/// <c>sealed class Rings(ModuleBase m) : Components.SimpleAOEGroups(m, [(uint)AID.RingA, (uint)AID.RingB], 6f);</c>
/// When <paramref name="expectedNumCasters"/> instances have started, the list is sorted by activation
/// so staggered groups resolve in order. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class SimpleAOEGroups(ModuleBase module, uint[] aids, AOEShape shape, int maxCasts = int.MaxValue, int expectedNumCasters = 99, double riskyWithSecondsLeft = default)
    : SimpleAOEs(module, default, shape, maxCasts, riskyWithSecondsLeft)
{
    public SimpleAOEGroups(ModuleBase module, uint[] aids, float radius, int maxCasts = int.MaxValue, int expectedNumCasters = 99, double riskyWithSecondsLeft = default)
        : this(module, aids, new AOEShapeCircle(radius), maxCasts, expectedNumCasters, riskyWithSecondsLeft) { }

    protected readonly uint[] AIDs = aids;
    protected readonly int ExpectedNumCasters = expectedNumCasters;

    protected bool Watches(uint id)
    {
        for (var i = 0; i < this.AIDs.Length; ++i)
            if (id == this.AIDs[i])
                return true;
        return false;
    }

    protected static void SortByActivation(List<AOEInstance> casters)
        => casters.Sort(static (a, b) => a.Activation.CompareTo(b.Activation));

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (!this.Watches(cast.Action.ID))
            return;
        var origin = cast.LocXZ != default ? cast.LocXZ : caster.Position;
        this.Casters.Add(new AOEInstance(this.Shape, origin, cast.Rotation, this.Module.CastFinishAt(cast), actorID: caster.InstanceID));
        if (this.Casters.Count >= this.ExpectedNumCasters)
            SortByActivation(this.Casters);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        // actorID is unique to an active cast, so no need to re-check the action id
        this.Casters.RemoveAll(c => c.ActorID == caster.InstanceID);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (this.Watches(spell.Action.ID))
            ++this.NumCasts;
    }
}

/// <summary>
/// A <see cref="SimpleAOEGroups"/> that only treats the earliest activation-window of casts as active:
/// everything within <paramref name="timeWindowInSeconds"/> of the first pending instance. Useful when a
/// group's cast count varies. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class SimpleAOEGroupsByTimewindow(ModuleBase module, uint[] aids, AOEShape shape, double timeWindowInSeconds = 1d, int expectedNumCasters = 99, double riskyWithSecondsLeft = default)
    : SimpleAOEGroups(module, aids, shape, int.MaxValue, expectedNumCasters, riskyWithSecondsLeft)
{
    public SimpleAOEGroupsByTimewindow(ModuleBase module, uint[] aids, float radius, double timeWindowInSeconds = 1d, int expectedNumCasters = 99, double riskyWithSecondsLeft = default)
        : this(module, aids, new AOEShapeCircle(radius), timeWindowInSeconds, expectedNumCasters, riskyWithSecondsLeft) { }

    protected readonly double TimeWindowInSeconds = timeWindowInSeconds;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = this.Casters.Count;
        if (count == 0)
            return [];

        var aoes = CollectionsMarshal.AsSpan(this.Casters);
        var deadline = aoes[0].Activation.AddSeconds(this.TimeWindowInSeconds);
        var index = 0;
        while (index < count && aoes[index].Activation < deadline)
            ++index;

        if (this.RiskyWithSecondsLeft != default)
        {
            var now = this.World.CurrentTime;
            for (var i = 0; i < index; ++i)
                aoes[i].Risky = aoes[i].Activation.AddSeconds(-this.RiskyWithSecondsLeft) <= now;
        }
        return aoes[..index];
    }
}

/// <summary>
/// A "charge at location" AOE: a rectangle from the caster to the cast's target location, resolving at
/// the end of the cast. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class ChargeAOEs(ModuleBase module, uint aid, float halfWidth, int maxCasts = int.MaxValue, double riskyWithSecondsLeft = default, float extraLengthFront = default)
    : SimpleAOEs(module, aid, new AOEShapeCircle(default), maxCasts, riskyWithSecondsLeft)
{
    public readonly float HalfWidth = halfWidth;
    public readonly float ExtraLengthFront = extraLengthFront;

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID != this.WatchedAction)
            return;
        var dir = cast.LocXZ - caster.Position;
        var shape = new AOEShapeRect(dir.Length() + this.ExtraLengthFront, this.HalfWidth);
        this.Casters.Add(new AOEInstance(shape, caster.Position, Angle.FromDirection(dir), this.Module.CastFinishAt(cast), actorID: caster.InstanceID));
    }
}

/// <summary>A <see cref="ChargeAOEs"/> that watches several actions sharing one half-width.</summary>
public class SimpleChargeAOEGroups(ModuleBase module, uint[] aids, float halfWidth, int maxCasts = int.MaxValue, int expectedNumCasters = 99, double riskyWithSecondsLeft = default, float extraLengthFront = default)
    : SimpleAOEGroups(module, aids, 0f, maxCasts, expectedNumCasters, riskyWithSecondsLeft)
{
    private readonly float HalfWidth = halfWidth;
    private readonly float ExtraLengthFront = extraLengthFront;

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (!this.Watches(cast.Action.ID))
            return;
        var dir = cast.LocXZ - caster.Position;
        var shape = new AOEShapeRect(dir.Length() + this.ExtraLengthFront, this.HalfWidth);
        this.Casters.Add(new AOEInstance(shape, caster.Position, Angle.FromDirection(dir), this.Module.CastFinishAt(cast), actorID: caster.InstanceID));
        if (this.Casters.Count >= this.ExpectedNumCasters)
            SortByActivation(this.Casters);
    }
}
