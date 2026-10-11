namespace Minerva;

/// <summary>
/// A character's own taste in landing spots. The solver takes the cheapest safe cell and the cost is mostly the walk,
/// so characters that start together all pick the same cell and move as one block. The user, 2026-10-11: "so we all
/// dont just huddle and move to same spot as our other bots".
/// <para>Each cell is worth some yalms of walking less to a given character, by a fixed hash of the character and the
/// cell: the same character always likes the same cells, different characters like different ones, and nothing needs
/// setting up per character. It only chooses among cells the solver already accepts as safe and reachable.</para>
/// </summary>
public static class LandingSpread
{
    /// <summary>Yalms of walking the cell at <paramref name="p"/> is worth to the character <paramref name="seed"/>:
    /// from 0 up to <paramref name="spread"/>, fixed per character and one-yalm cell.</summary>
    public static float Worth(ulong seed, WPos p, float spread)
    {
        if (spread <= 0f)
            return 0f;
        var h = seed
            ^ (unchecked((ulong)(long)MathF.Floor(p.X)) * 0x9E3779B97F4A7C15ul)
            ^ (unchecked((ulong)(long)MathF.Floor(p.Z)) * 0xC2B2AE3D27D4EB4Ful);
        // a 64-bit finaliser (MurmurHash3's), so neighbouring cells and neighbouring ids come out unrelated
        h ^= h >> 33;
        h *= 0xFF51AFD7ED558CCDul;
        h ^= h >> 33;
        h *= 0xC4CEB9FE1A85EC53ul;
        h ^= h >> 33;
        return spread * ((h >> 40) / (float)(1 << 24));
    }

    /// <summary>
    /// What the cell is worth on this walk: never more than half the walk, so a short hop stays short and standing
    /// still is never traded for a shuffle; and never more than half the distance to spare, so a dodge that is tight
    /// still takes the nearest spot. The spare is what is left of the solver's budget, which the clearance lead has
    /// already shortened: the spread spends walking, never the margin the lead buys. Measured on four recordings,
    /// 2026-10-11, eight characters on one spot: about a yalm apart on average for 0.3y more walking, more where
    /// there is room and time, nothing where the floor leaves one pocket; no spot lost.
    /// </summary>
    public static float Discount(AIHints hints, WPos p, float travel, float spareYalms)
    {
        var worth = Worth(hints.LandingSeed, p, hints.LandingSpread);
        return MathF.Max(MathF.Min(worth, 0.5f * MathF.Min(travel, spareYalms)), 0f);
    }
}
