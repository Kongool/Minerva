namespace Minerva.Components;

/// <summary>
/// Counts resolved casts of a watched action. Base for many mechanic components (the whole AOE family
/// derives from it) and useful on its own to drive state-machine transitions off a cast count.
/// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class CastCounter(ModuleBase module, uint aid) : ModuleComponent(module)
{
    public readonly uint WatchedAction = aid;
    public int NumCasts;

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == this.WatchedAction)
            ++this.NumCasts;
    }
}

/// <summary>Like <see cref="CastCounter"/> but counts any of several actions.</summary>
public class CastCounterMulti(ModuleBase module, uint[] aids) : ModuleComponent(module)
{
    public readonly uint[] WatchedActions = aids;
    public int NumCasts;

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        var id = spell.Action.ID;
        for (var i = 0; i < this.WatchedActions.Length; ++i)
        {
            if (id == this.WatchedActions[i])
            {
                ++this.NumCasts;
                return;
            }
        }
    }
}
