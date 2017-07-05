namespace Foreman
{
    using System.Collections.Generic;
    using System.Linq;

    public class Resource
    {
        public string Name { get; }
        public string Category { get; set; }
        public float Hardness { get; set; }
        public float Time { get; set; }
        public string Result { get; set; }

        public Resource(string name)
        {
            Name = name;
        }
    }

    public class Miner : ProductionEntity
    {
        public List<string> ResourceCategories { get; }
        public float MiningPower { get; set; }

        public Miner(string name)
        {
            Name = name;
            ResourceCategories = new List<string>();
            Enabled = true;
        }

        public float GetRate(Resource resource, IEnumerable<Module> modules)
        {
            double finalSpeed = Speed;
            foreach (Module module in modules.Where(m => m != null)) {
                finalSpeed += module.SpeedBonus * Speed;
            }

            // According to https://wiki.factorio.com/Mining
            double timeForOneItem = resource.Time / ((MiningPower - resource.Hardness) * finalSpeed);

            // Round up to the nearest tick, since mining can't start until the start of a new tick
            timeForOneItem = GameUtils.RoundToNearestTick(timeForOneItem);

            return (float)(1d / timeForOneItem);
        }
    }
}
