namespace Minerva.Generation;

/// <summary>
/// Resolves action/object ids to human names from game data, so the generator can emit readable
/// enum members (<c>PunutiyPress = 36492</c>) instead of numeric placeholders. Like
/// <see cref="IShapeResolver"/>, this is the seam between game-data lookups (plugin) and the
/// game-free generator (core). Names are sanitized to valid C# identifiers by the generator.
/// </summary>
public interface INameResolver
{
    /// <summary>Display name for an action id, or null if unknown.</summary>
    string? ActionName(uint actionId);

    /// <summary>Display name for a BNpc/object id (OID), or null if unknown.</summary>
    string? ObjectName(uint oid);
}

/// <summary>Fallback resolver that knows no names — the generator falls back to <c>_id</c> members.</summary>
public sealed class NullNameResolver : INameResolver
{
    public string? ActionName(uint actionId) => null;
    public string? ObjectName(uint oid) => null;
}
