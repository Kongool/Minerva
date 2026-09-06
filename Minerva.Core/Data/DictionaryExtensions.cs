namespace Minerva;

/// <summary>
/// Small dictionary helpers used by ported components.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// The value for <paramref name="key"/>, creating and storing a fresh one if it is not there yet.
    ///
    /// <para>Components that track something per player — a march's pending moves, a stack of mechanics
    /// aimed at someone — reach for this constantly, because the alternative at every call site is a
    /// TryGetValue, a construction and an insert. <typeparamref name="TValue"/> needs a parameterless
    /// constructor, which every one of those state records has.</para>
    /// </summary>
    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
        where TKey : notnull
        where TValue : new()
    {
        if (!dict.TryGetValue(key, out var value))
            dict[key] = value = new TValue();
        return value;
    }

    /// <summary>As <see cref="GetOrAdd{TKey,TValue}(Dictionary{TKey,TValue},TKey)"/>, with the caller
    /// supplying how to build a missing value.</summary>
    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> factory)
        where TKey : notnull
    {
        if (!dict.TryGetValue(key, out var value))
            dict[key] = value = factory(key);
        return value;
    }
}
