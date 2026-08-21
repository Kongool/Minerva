namespace Minerva;

/// <summary>
/// Learns roughly where the walkable ground is, from the only evidence that cannot be wrong: somebody
/// stood there.
/// <para>With no boss module there is nothing to declare an arena, so the dodge has to guess. Both
/// BossmodReborn and Minerva have guessed the same way — a fixed box centred on the player (BMR: a 60x60
/// square snapped to a 5-unit lattice, <c>AIHintsBuilder.cs</c>; Minerva: a 30 yalm circle). That box
/// follows you to the edge of a platform and keeps extending past it, so the far side of a wide AOE looks
/// like clear ground and the character is steered into the void. It is the reported failure exactly:
/// small AOEs dodge fine, an edge-hugging one walks you off.</para>
/// <para>BMR's answer is 428 obstacle bitmaps baked offline through vnavmesh, keyed on zone and duty. They
/// work well where they exist and do nothing at all where they do not, which is every unscripted fight.
/// This is the answer that needs no data shipped ahead of time: watch where the party and the enemies
/// actually stand, and keep the search inside that.</para>
/// <para>It gives an <i>extent</i>, not a shape. The box between two places people stood is not
/// necessarily floor — an arena with a hole in the middle would be described as solid. Holes are the
/// floor probe's job; this only stops the search running off the outside.</para>
/// </summary>
public sealed class ArenaFootprint
{
    /// <summary>Below this the samples describe a spot rather than a field, and clamping to them would
    /// leave nowhere to dodge to. Matches the replay analyser's own floor for the same reason.</summary>
    public const float MinUsableHalfExtent = 8f;

    /// <summary>Grid sanity: past this the cell count costs more than the accuracy is worth.</summary>
    public const float MaxHalfExtent = 40f;

    /// <summary>Allowance past the outermost sample. People do stand on the very edge, so this is small —
    /// the floor probe is what makes a wrong guess here survivable.</summary>
    public const float Margin = 2f;

    /// <summary>Samples this far above or below the player are a different storey, not this arena.</summary>
    public const float SameFloorTolerance = 8f;

    /// <summary>Enough distinct observations that one mispositioned actor cannot define the arena.</summary>
    public const int MinSamples = 24;

    private float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
    private int samples;
    private ushort zone;

    public int Samples => this.samples;

    /// <summary>Forget everything on a zone change — the previous arena says nothing about this one.</summary>
    public void EnterZone(ushort newZone)
    {
        if (newZone == this.zone)
            return;
        this.zone = newZone;
        this.Reset();
    }

    public void Reset()
    {
        this.minX = this.minZ = float.MaxValue;
        this.maxX = this.maxZ = float.MinValue;
        this.samples = 0;
    }

    /// <summary>Record one place something stood. <paramref name="dy"/> is its height above the player.</summary>
    public void Observe(WPos p, float dy = 0f)
    {
        if (MathF.Abs(dy) > SameFloorTolerance)
            return;

        this.minX = MathF.Min(this.minX, p.X);
        this.maxX = MathF.Max(this.maxX, p.X);
        this.minZ = MathF.Min(this.minZ, p.Z);
        this.maxZ = MathF.Max(this.maxZ, p.Z);
        ++this.samples;
    }

    /// <summary>
    /// The arena as observed so far, or false while the evidence is too thin to beat a blind guess.
    /// <para>Centred on the box rather than on the player. That is the part that matters: a player-centred
    /// window slides off the platform with you, an arena-centred one does not.</para>
    /// </summary>
    public bool TryEstimate(out WPos center, out ArenaBounds bounds)
    {
        center = default;
        bounds = null!;
        if (this.samples < MinSamples)
            return false;

        var halfX = ((this.maxX - this.minX) * 0.5f) + Margin;
        var halfZ = ((this.maxZ - this.minZ) * 0.5f) + Margin;
        if (MathF.Max(halfX, halfZ) < MinUsableHalfExtent)
            return false;

        center = new WPos((this.minX + this.maxX) * 0.5f, (this.minZ + this.maxZ) * 0.5f);
        bounds = new ArenaBoundsRect(MathF.Min(halfX, MaxHalfExtent), MathF.Min(halfZ, MaxHalfExtent));
        return true;
    }
}
