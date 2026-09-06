namespace Minerva;

using System.Runtime.InteropServices;

/// <summary>
/// Span access over a <see cref="List{T}"/>, under the name BossmodReborn modules use.
/// </summary>
public static class ListExtensions
{
    /// <summary>
    /// The list's backing store as a span, without copying. Ported modules iterate their AOE and tower
    /// lists this way in code that runs every frame for every party member.
    /// <para>The span is invalidated by anything that changes the list's capacity, so treat it as valid only
    /// for the loop it was taken for.</para>
    /// </summary>
    public static Span<T> AsSpan<T>(this List<T> list) => CollectionsMarshal.AsSpan(list);

    /// <summary>
    /// The element at <paramref name="index"/>, or <c>default</c> when the index is out of range. Modules
    /// index per-slot arrays with a slot that can be -1 (the player is not in the party, or was not found),
    /// and want "no state" rather than a crash mid-frame.
    /// </summary>
    public static T? BoundSafeAt<T>(this T[] array, int index)
        => index >= 0 && index < array.Length ? array[index] : default;

    public static T? BoundSafeAt<T>(this List<T> list, int index)
        => index >= 0 && index < list.Count ? list[index] : default;
}

/// <summary>
/// Array helpers under the names BossmodReborn modules use.
/// </summary>
public static class ArrayExtensions
{
    /// <summary>The array reversed, as a new array. Modules use it to mirror a fixed sequence of directions
    /// for the other half of the party, so the original has to survive.</summary>
    public static T[] ReverseArray<T>(this T[] source)
    {
        var copy = (T[])source.Clone();
        Array.Reverse(copy);
        return copy;
    }
}

/// <summary>
/// Vector helpers under the names BossmodReborn modules use.
/// </summary>
public static class ListHelpers
{
    /// <summary>Add the value unless it is null. Modules collect actors from lookups that may miss.</summary>
    public static void AddIfNonNull<T>(this List<T> list, T? value) where T : class
    {
        if (value != null)
            list.Add(value);
    }
}

public static class VectorExtensions
{
    /// <summary>The position half of an actor's <c>PosRot</c>, dropping the rotation in W. Modules pass it
    /// as a ground-target position for an action.</summary>
    public static System.Numerics.Vector3 XYZ(this System.Numerics.Vector4 v) => new(v.X, v.Y, v.Z);
}
