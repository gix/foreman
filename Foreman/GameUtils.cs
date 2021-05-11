namespace Foreman
{
    using System;

    public static class GameUtils
    {
        public static double RoundToNearestTick(double value)
        {
            return value;
            // This seems to give values different from in-game statistics
            return Math.Ceiling(value * 60d) / 60d;
        }

        public static double GetRate(float recipeTime, double speed)
        {
            // Machines have to wait for a new tick before starting a new item, so round up to the nearest tick
            double craftingTime = RoundToNearestTick(recipeTime / speed);

            return 1d / craftingTime;
        }

        public static double GetMiningRate(Resource resource, double miningPower, double speed)
        {
            // According to https://wiki.factorio.com/Mining
            double timeForOneItem = resource.MiningTime / ((miningPower - resource.Hardness) * speed);

            // Round up to the nearest tick, since mining can't start until the start of a new tick
            timeForOneItem = RoundToNearestTick(timeForOneItem);

            return 1d / timeForOneItem;
        }
    }
}
