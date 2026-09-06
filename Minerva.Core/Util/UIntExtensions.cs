// Small integer helpers, ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
//
// Forbidden Folios is a maths mechanic: its pages test whether your acting level is prime, or divisible by
// the number shown. The module needs these to answer the same question the fight asks.

namespace Minerva;

[SkipLocalsInit]
public static class UIntExtensions
{
    public static bool IsPrime(this uint number)
    {
        if (number <= 1u)
        {
            return false;
        }

        if (number == 2u)
        {
            return true;
        }

        if ((number & 1u) == 0u)
        {
            return false;
        }

        var limit = (uint)Math.Sqrt(number);

        for (var i = 3u; i <= limit; i += 2u)
        {
            if (number % i == 0u)
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsDivisible(this uint dividend, uint divisor) => dividend % divisor == 0f;
}
