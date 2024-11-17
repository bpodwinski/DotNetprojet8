namespace GpsUtil.Helpers;

internal static class ThreadLocalRandom
{
    public static double NextDouble(double minValue, double maxValue)
    {
        if (minValue >= maxValue)
            throw new ArgumentException("minValue must be less than maxValue.");

        return Random.Shared.NextDouble() * (maxValue - minValue) + minValue;
    }

    public static int Next(int minValue, int maxValue)
    {
        if (minValue >= maxValue)
            throw new ArgumentException("minValue must be less than maxValue.");

        return Random.Shared.Next(minValue, maxValue);
    }
}
