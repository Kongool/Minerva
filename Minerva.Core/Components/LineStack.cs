namespace Minerva.Components;

/// <summary>
/// Line stack: a long rectangle aimed from its source through a marked player, which the party must line
/// up inside to share. The marker arrives either as a telegraph cast (<paramref name="aidMarker"/>), an
/// overhead icon (<paramref name="iconID"/>), or — when neither is given — the resolve cast itself, and
/// the bait clears when <paramref name="aidResolve"/> lands. The rectangle follows live positions, so it
/// reads correctly as the marked player moves. Ported from BossmodReborn's LineStack (BSD-3; see
/// THIRD-PARTY-NOTICES.txt).
/// </summary>
public class LineStack(ModuleBase module, uint aidMarker, uint aidResolve, double activationDelay = 5.1d, float range = 50f, float halfWidth = 4f, int minStackSize = 4, int maxStackSize = int.MaxValue, int maxCasts = 1, bool markerIsFinalTarget = true, uint iconID = default) : GenericBaitStack(module)
{
    /// <summary>Icon-driven overload: the marker is an overhead icon rather than a telegraph cast.</summary>
    public LineStack(ModuleBase module, uint iconID, uint aidResolve, double activationDelay = 5.1d, float range = 50f, float halfWidth = 4f, int minStackSize = 4, int maxStackSize = int.MaxValue, int maxCasts = 1, bool markerIsFinalTarget = true)
        : this(module, default, aidResolve, activationDelay, range, halfWidth, minStackSize, maxStackSize, maxCasts, markerIsFinalTarget, iconID) { }

    public readonly uint AidMarker = aidMarker;
    public readonly uint AidResolve = aidResolve;
    public readonly double ActionDelay = activationDelay;
    public readonly float Range = range;
    public readonly float HalfWidth = halfWidth;
    // TODO (matches BMR): min/max stack size are recorded but not yet enforced in the hints.
    public readonly int MinStackSize = minStackSize;
    public readonly int MaxStackSize = maxStackSize;
    public readonly int MaxCasts = maxCasts; // stacks whose resolve AID hits several times
    public readonly bool MarkerIsFinalTarget = markerIsFinalTarget; // rarely the marked player is not the final target
    public readonly uint IconId = iconID;

    private readonly AOEShape rect = new AOEShapeRect(range, halfWidth);
    private int castCounter;

    /// <summary>True when the marker is a separate telegraph (cast or icon) rather than the resolve cast.</summary>
    private bool HasSeparateMarker => this.AidMarker != default || this.IconId != default;

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (!this.HasSeparateMarker)
            return;

        var id = spell.Action.ID;
        if (id == this.AidMarker && this.World.Actors.Find(spell.MainTargetID) is { } target)
        {
            this.CurrentBaits.Add(new(caster, target, this.rect, this.World.FutureTime(this.ActionDelay), maxCasts: this.MaxCasts));
        }
        else if (id == this.AidResolve)
        {
            this.Resolve(spell.MainTargetID);
        }
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (this.IconId != default && iconID == this.IconId && this.World.Actors.Find(targetID) is { } target)
            this.CurrentBaits.Add(new(actor, target, this.rect, this.World.FutureTime(this.ActionDelay), maxCasts: this.MaxCasts));
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        // without a separate marker the resolve cast *is* the telegraph: bait for its duration
        if (!this.HasSeparateMarker && cast.Action.ID == this.AidResolve && this.World.Actors.Find(cast.TargetID) is { } target)
            this.CurrentBaits.Add(new(caster, target, this.rect, this.Module.CastFinishAt(cast), maxCasts: this.MaxCasts));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (!this.HasSeparateMarker && cast.Action.ID == this.AidResolve)
            this.Resolve(cast.TargetID);
    }

    /// <summary>
    /// Clear the bait that just resolved. When the marked player is the final target, the bait is matched
    /// by target id (and a lone bait is re-pointed if the game picked someone else); otherwise the resolve
    /// casts are simply counted off the front of the queue.
    /// </summary>
    private void Resolve(ulong targetID)
    {
        if (!this.MarkerIsFinalTarget)
        {
            if (++this.castCounter >= this.MaxCasts && this.CurrentBaits.Count != 0)
            {
                this.CurrentBaits.RemoveAt(0);
                this.castCounter -= this.MaxCasts;
                ++this.NumCasts;
            }
            return;
        }

        // a single outstanding bait that resolved onto someone else means the marker moved — follow it
        if (this.CurrentBaits.Count == 1 && this.CurrentBaits[0].Target.InstanceID != targetID && this.World.Actors.Find(targetID) is { } actual)
            this.CurrentBaits.Ref(0).Target = actual;

        for (var i = 0; i < this.CurrentBaits.Count; ++i)
        {
            ref var b = ref this.CurrentBaits.Ref(i);
            if (b.Target.InstanceID == targetID && --b.MaxCasts == 0)
            {
                this.CurrentBaits.RemoveAt(i);
                ++this.NumCasts;
                return;
            }
        }
    }
}
