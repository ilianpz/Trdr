namespace Trdr;

public static class NumericExtensions
{
    public static int GetDigits(this long n)
    {
        if (n == 0)
            return 1;

        return (n > 0 ? 1 : 2) + (int)Math.Log10(Math.Abs((double)n));
    }
}