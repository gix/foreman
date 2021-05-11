namespace Foreman
{
    using System.Collections.Generic;

    public class Resource
    {
        public Resource(
            string name, string category, float hardness, float miningTime,
            Item result)
        {
            Name = name;
            Category = category;
            Hardness = hardness;
            MiningTime = miningTime;
            Result = result;
        }

        public string Name { get; }
        public string Category { get; }
        public float Hardness { get; }
        public float MiningTime { get; }
        public Item Result { get; }

        public override string ToString()
        {
            return Name;
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

        public double GetRate(Resource resource, double beaconBonus, IEnumerable<Module> modules = null)
        {
            return GameUtils.GetMiningRate(resource, MiningPower, GetSpeed(beaconBonus, modules));
        }
    }
}
