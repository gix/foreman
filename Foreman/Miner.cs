namespace Foreman
{
    using System.Collections.Generic;

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
    }
}
