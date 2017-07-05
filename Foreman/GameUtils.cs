namespace Foreman
{
    using System;

    public static class GameUtils
    {
        public static double RoundToNearestTick(double value)
        {
            return Math.Ceiling(value * 60d) / 60d;
        }
    }
}
