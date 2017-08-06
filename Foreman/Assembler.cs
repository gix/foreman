namespace Foreman
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Media.Imaging;
    using Units;

    public class MachinePermutation
    {
        public ProductionEntity Assembler { get; }
        public List<Module> Modules { get; }

        public double GetAssemblerRate(float recipeTime, float beaconBonus)
        {
            return GameUtils.GetRate(recipeTime, Assembler.GetSpeed(beaconBonus, Modules));
        }

        internal double GetAssemblerProductivity()
        {
            return Modules
                .Where(x => x != null)
                .Sum(x => x.ProductivityBonus);
        }

        public double GetMinerRate(Resource r, float beaconBonus)
        {
            return GameUtils.GetMiningRate(r, ((Miner)Assembler).MiningPower, Assembler.GetSpeed(beaconBonus, Modules));
        }

        public MachinePermutation(ProductionEntity machine, ICollection<Module> modules)
        {
            Assembler = machine;
            Modules = modules.ToList();
        }
    }

    public abstract class ProductionEntity
    {
        public string Name { get; protected set; }
        public bool Enabled { get; set; }
        public BitmapSource Icon { get; set; }
        public int ModuleSlots { get; set; }
        public float Speed { get; set; }
        public Power EnergyUsage { get; set; }
        private string friendlyName;

        public string FriendlyName
        {
            get => !string.IsNullOrWhiteSpace(friendlyName) ? friendlyName : Name;
            set => friendlyName = value;
        }

        public double GetSpeed(double beaconBonus, IEnumerable<Module> modules = null)
        {
            double finalSpeed = Speed;
            if (modules != null) {
                foreach (Module module in modules.Where(m => m != null))
                    finalSpeed += module.SpeedBonus * Speed;
            }
            finalSpeed += beaconBonus * Speed;

            return finalSpeed;
        }

        public Power GetEnergyConsumption(double beaconBonus, IEnumerable<Module> modules = null)
        {
            Power consumption = EnergyUsage;
            if (modules != null) {
                foreach (Module module in modules.Where(m => m != null))
                    consumption += module.ConsumptionBonus * EnergyUsage;
            }
            consumption += beaconBonus * EnergyUsage;

            return consumption;
        }

        public IEnumerable<MachinePermutation> GetAllPermutations(Recipe recipe)
        {
            yield return new MachinePermutation(this, new List<Module>());

            Module[] currentModules = new Module[ModuleSlots];

            if (ModuleSlots <= 0) {
                yield break;
            }

            var allowedModules = DataCache.Modules.Values
                .Where(m => m.Enabled)
                .Where(m => m.AllowedIn(recipe));

            foreach (Module module in allowedModules) {
                for (int i = 0; i < ModuleSlots; i++) {
                    currentModules[i] = module;
                    yield return new MachinePermutation(this, currentModules);
                }
            }
        }
    }

    public class Assembler : ProductionEntity
    {
        public List<string> Categories { get; }
        public int MaxIngredients { get; set; }
        public List<string> AllowedEffects { get; }

        public Assembler(string name)
        {
            Enabled = true;
            Name = name;
            Categories = new List<string>();
            AllowedEffects = new List<string>();
        }

        public override string ToString()
        {
            return string.Format("Assembler: {0}", Name);
        }

        public double GetRate(float recipeTime, double beaconBonus, IEnumerable<Module> modules = null)
        {
            return GameUtils.GetRate(recipeTime, GetSpeed(beaconBonus, modules));
        }
    }

    public class Module
    {
        public BitmapSource Icon => DataCache.Items[Name].Icon; // For each module there should be a corresponding item with the icon already loaded.

        public bool Enabled { get; set; }
        public float SpeedBonus { get; }
        public float ProductivityBonus { get; }
        public float ConsumptionBonus { get; }
        public string Name { get; }
        private string friendlyName;
        private readonly List<string> allowedIn;

        public string FriendlyName
        {
            get => !string.IsNullOrWhiteSpace(friendlyName) ? friendlyName : Name;
            set => friendlyName = value;
        }


        public Module(
            string name, float speedBonus, float productivityBonus,
            float consumptionBonus, List<string> allowedIn)
        {
            SpeedBonus = speedBonus;
            ProductivityBonus = productivityBonus;
            ConsumptionBonus = consumptionBonus;
            Name = name;
            Enabled = true;
            this.allowedIn = allowedIn;
        }

        public bool AllowedIn(Recipe recipe)
        {
            // TODO: Remove recipe == null case, it's just there as scaffolding.
            if (allowedIn == null || recipe == null)
                return true;

            return allowedIn.Contains(recipe.Name);
        }
    }
}
