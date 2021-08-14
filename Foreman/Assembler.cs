namespace Foreman
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Media.Imaging;
    using Foreman.Extensions;
    using Units;

    public class MachinePermutation
    {
        public MachinePermutation(ProductionEntity machine, IEnumerable<Module> modules)
        {
            Assembler = machine;
            Modules = modules.ToList();
        }

        public ProductionEntity Assembler { get; }
        public IReadOnlyList<Module> Modules { get; }

        internal double GetAssemblerProductivity()
        {
            return Modules.Sum(x => x.ProductivityBonus);
        }
    }

    public abstract class ProductionEntity
    {
        protected ProductionEntity(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public bool Enabled { get; set; }
        public BitmapSource? Icon { get; set; }
        public int ModuleSlots { get; set; }
        public float Speed { get; set; }
        public Power EnergyUsage { get; set; }

        public string FriendlyName => DataCache.Current.GetLocalizedString(Name);

        public double GetSpeed(double beaconBonus, IEnumerable<Module?>? modules = null)
        {
            double finalSpeed = Speed;
            if (modules != null) {
                foreach (Module module in modules.NotNull())
                    finalSpeed += module.SpeedBonus * Speed;
            }
            finalSpeed += beaconBonus * Speed;

            return finalSpeed;
        }

        public Power GetEnergyConsumption(double beaconBonus, IEnumerable<Module?>? modules = null)
        {
            Power consumption = EnergyUsage;
            if (modules != null) {
                foreach (Module module in modules.NotNull())
                    consumption += module.ConsumptionBonus * EnergyUsage;
            }
            consumption += beaconBonus * EnergyUsage;

            return consumption;
        }

        public IEnumerable<MachinePermutation> GetAllPermutations(Recipe recipe)
        {
            yield return new MachinePermutation(this, new List<Module>());

            var currentModules = new Module[ModuleSlots];

            if (ModuleSlots <= 0) {
                yield break;
            }

            var allowedModules = DataCache.Current.Modules.Values
                .Where(m => m.Enabled)
                .Where(m => m.AllowedIn((Assembler)this, recipe));

            foreach (Module module in allowedModules) {
                for (int i = 0; i < ModuleSlots; ++i) {
                    currentModules[i] = module;
                    yield return new MachinePermutation(this, currentModules);
                }
            }
        }

        public IEnumerable<MachinePermutation> GetAllPermutations(Resource resource)
        {
            yield return new MachinePermutation(this, new List<Module>());

            var currentModules = new Module[ModuleSlots];

            if (ModuleSlots <= 0) {
                yield break;
            }

            var allowedModules = DataCache.Current.Modules.Values
                .Where(m => m.Enabled)
                .Where(m => m.AllowedIn((Miner)this, resource));

            foreach (Module module in allowedModules) {
                for (int i = 0; i < ModuleSlots; ++i) {
                    currentModules[i] = module;
                    yield return new MachinePermutation(this, currentModules);
                }
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class Assembler : ProductionEntity
    {
        public List<string> Categories { get; }
        public int MaxIngredients { get; set; }
        public List<string> AllowedEffects { get; }

        public Assembler(string name)
            : base(name)
        {
            Enabled = true;
            Categories = new List<string>();
            AllowedEffects = new List<string>();
        }

        public override string ToString()
        {
            return $"Assembler: {Name}";
        }

        public double GetRate(float recipeTime, double beaconBonus, IEnumerable<Module>? modules = null)
        {
            return GameUtils.GetRate(recipeTime, GetSpeed(beaconBonus, modules));
        }
    }

    public class Module
    {
        public BitmapSource Icon => DataCache.Current.Items[Name].Icon; // For each module there should be a corresponding item with the icon already loaded.

        public bool Enabled { get; set; }
        public string Category { get; set; }
        public float SpeedBonus { get; }
        public float ProductivityBonus { get; }
        public float ConsumptionBonus { get; }
        public string Name { get; }
        private readonly List<string>? allowedIn;

        public string FriendlyName => DataCache.Current.GetLocalizedString("item-name", Name);

        public Module(
            string name, string category, float speedBonus, float productivityBonus,
            float consumptionBonus, List<string>? allowedIn)
        {
            SpeedBonus = speedBonus;
            ProductivityBonus = productivityBonus;
            ConsumptionBonus = consumptionBonus;
            Name = name;
            Category = category;
            Enabled = true;
            this.allowedIn = allowedIn;
        }

        public bool AllowedIn(Assembler? assembler, Recipe recipe)
        {
            if (assembler != null && !EnumerateEffects().All(x => assembler.AllowedEffects.Contains(x)))
                return false;
            return allowedIn == null || allowedIn.Contains(recipe.Name);
        }

        public bool AllowedIn(Miner? miner, Resource? resource)
        {
            if (miner != null && resource != null && !miner.ResourceCategories.Contains(resource.Category))
                return false;
            return true;
        }

        public IEnumerable<string> EnumerateEffects()
        {
            if (SpeedBonus != 0)
                yield return "speed";
            if (ProductivityBonus != 0)
                yield return "productivity";
            if (ConsumptionBonus != 0)
                yield return "consumption";
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
