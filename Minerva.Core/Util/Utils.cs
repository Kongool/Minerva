namespace Minerva;

/// <summary>
/// Small helpers BossmodReborn modules reach for constantly, kept under the name they use so ported files
/// need no edits (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public static class Utils
{
    /// <summary>An array of <paramref name="count"/> copies of <paramref name="value"/>.</summary>
    public static T[] MakeArray<T>(int count, T value)
    {
        var res = new T[count];
        Array.Fill(res, value);
        return res;
    }

    /// <summary>
    /// An array of <paramref name="count"/> items, each produced by its own call to
    /// <paramref name="make"/> — unlike <see cref="MakeArray"/>, which shares one value. The distinction
    /// matters for reference types: eight party slots sharing a single mutable object is a bug that only
    /// shows up once two slots disagree.
    /// </summary>
    public static T[] GenArray<T>(int count, Func<T> make)
    {
        var res = new T[count];
        for (var i = 0; i < count; ++i)
            res[i] = make();
        return res;
    }

    public static void Swap<T>(ref T a, ref T b) => (a, b) = (b, a);

    /// <summary>Whether two values are within <paramref name="epsilon"/> of each other.</summary>
    public static bool AlmostEqual(float a, float b, float epsilon) => MathF.Abs(a - b) <= epsilon;

    public static bool AlmostEqual(double a, double b, double epsilon) => Math.Abs(a - b) <= epsilon;

    /// <summary>
    /// Rotate a list in place so the element at <paramref name="startIndex"/> becomes first.
    /// <para>Three reversals rather than a copy: modules call this on party lists while assigning
    /// clockwise spots, and it runs per frame. Same implementation BossmodReborn uses.</para>
    /// </summary>
    public static void RotateList<T>(List<T> list, int startIndex)
    {
        var count = list.Count;
        if (count <= 1 || startIndex == 0 || startIndex % count == 0)
            return;

        startIndex %= count;
        Reverse(list, 0, startIndex - 1);
        Reverse(list, startIndex, count - 1);
        Reverse(list, 0, count - 1);

        static void Reverse(List<T> list, int left, int right)
        {
            var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(list);
            while (left < right)
            {
                (span[left], span[right]) = (span[right], span[left]);
                ++left;
                --right;
            }
        }
    }
}
