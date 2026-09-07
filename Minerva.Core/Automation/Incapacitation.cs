namespace Minerva;

/// <summary>
/// Statuses that take the character's legs away: while one is on, no dodge can move anybody, and a hit
/// taken during it is not the dodge arriving late.
/// <para>Eureka Orthos, 2026-09-06: a Mirrorknight's Double Hex Eye stunned the whole party, Knowing Gleam
/// landed four seconds later on a character that could not move, and the report blamed clearance lead.
/// The dodge was steering the whole time and the mover said it was driving, because from the movement
/// layer's point of view it was — the game simply ignores input while stunned.</para>
/// <para>Ids are only what has been seen in a recording or is named by a ported module's status enum;
/// an id that turns out to be wrong costs nothing (the status never appears), while a missing one only
/// leaves today's behaviour. Add as they are met.</para>
/// </summary>
public static class Incapacitation
{
    /// <summary>Status ids that stop movement, each with the word for the state it puts you in, so a
    /// report reads "you were stunned when it landed" rather than naming the status row.</summary>
    private static readonly (uint ID, string State)[] Blockers =
    [
        (149u, "stunned"),       // seen: Orthos Mirrorknight, Double Hex Eye, 2026-09-06
        (3u, "asleep"),
        (13u, "bound"),
        (2518u, "bound"),        // named Bind by three ported status enums
        (610u, "petrified"),     // named Petrification by several ported enums (Echidna, Ferdiad)
        (3007u, "petrified"),    // seen: Eye to Eye, 2026-09-05
        (1958u, "terrified"),
    ];

    /// <summary>The state this actor is stuck in ("stunned", "bound", ...), or null when it can move.</summary>
    public static string? Blocking(Actor? actor)
    {
        if (actor == null)
            return null;
        for (var i = 0; i < Blockers.Length; ++i)
            if (actor.FindStatus(Blockers[i].ID) != null)
                return Blockers[i].State;
        return null;
    }
}
