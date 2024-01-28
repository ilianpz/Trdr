using BenchmarkDotNet.Attributes;

namespace Trdr.Benchmarks;

public class NumericExtensionsBenchmarks
{
    private readonly List<long> _values = new();

    public NumericExtensionsBenchmarks()
    {
        for (int i = 0; i < 10_000_000; ++i)
        {
            _values.Add(Random.Shared.NextInt64());
        }
    }

    [Benchmark]
    public void Get_digits()
    {
        10000000L.GetDigits();
        1000000L.GetDigits();
        100000L.GetDigits();
        10000L.GetDigits();
        1000L.GetDigits();
        100L.GetDigits();
        10L.GetDigits();
        1L.GetDigits();
    }

    [Benchmark]
    public void Get_digits_from_rnd()
    {
        for (int i = 0; i < _values.Count; ++i)
        {
            _values[i].GetDigits();
        }
    }

    [Benchmark]
    public void Switch_get_digits()
    {
        Switch_GetDigits(10000000L);
        Switch_GetDigits(1000000L);
        Switch_GetDigits(100000L);
        Switch_GetDigits(10000L);
        Switch_GetDigits(1000L);
        Switch_GetDigits(100L);
        Switch_GetDigits(10L);
        Switch_GetDigits(1L);
    }

    [Benchmark]
    public void Switch_get_digits_from_rnd()
    {
        for (int i = 0; i < _values.Count; ++i)
        {
            Switch_GetDigits(_values[i]);
        }
    }

    // ReSharper disable once UnusedMethodReturnValue.Local
    private static int Switch_GetDigits(long n)
    {
        if (n >= 0)
        {
            return n switch
            {
                < 10L => 1,
                < 100L => 2,
                < 1000L => 3,
                < 10000L => 4,
                < 100000L => 5,
                < 1000000L => 6,
                < 10000000L => 7,
                < 100000000L => 8,
                < 1000000000L => 9,
                < 10000000000L => 10,
                < 100000000000L => 11,
                < 1000000000000L => 12,
                < 10000000000000L => 13,
                < 100000000000000L => 14,
                < 1000000000000000L => 15,
                < 10000000000000000L => 16,
                < 100000000000000000L => 17,
                < 1000000000000000000L => 18,
                _ => 19
            };
        }

        return n switch
        {
            > -10L => 2,
            > -100L => 3,
            > -1000L => 4,
            > -10000L => 5,
            > -100000L => 6,
            > -1000000L => 7,
            > -10000000L => 8,
            > -100000000L => 9,
            > -1000000000L => 10,
            > -10000000000L => 11,
            > -100000000000L => 12,
            > -1000000000000L => 13,
            > -10000000000000L => 14,
            > -100000000000000L => 15,
            > -1000000000000000L => 16,
            > -10000000000000000L => 17,
            > -100000000000000000L => 18,
            > -1000000000000000000L => 19,
            _ => 20
        };
    }
}