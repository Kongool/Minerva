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

/// <summary>
/// Casts this module has looked at and decided need no zone drawn — a telegraph that precedes the real
/// hit, a wind-up animation, a visual twin of something already covered.
/// <para>Exists so "not handled" and "handled by deciding it does not matter" stop looking the same. The
/// replay validator treats an uncovered <i>helper</i> cast as a probable missed mechanic, which is a good
/// rule: helpers only ever cast mechanics. But a fight with a couple of helper-cast telegraphs then fails
/// validation forever, and a check that always fails is a check nobody reads. Declaring them moves them
/// into the "watched, draws nothing" column, where raidwides and gazes already live.</para>
/// <para><paramref name="why"/> is not used at runtime. It is there because the next person to read this
/// needs to know it was a decision rather than an oversight.</para>
/// </summary>
public class IgnoredCasts(ModuleBase module, uint[] aids, string why) : CastCounterMulti(module, aids)
{
    public readonly string Why = why;
}
