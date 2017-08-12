namespace Foreman
{
    using System.Collections.Generic;
    using System.Linq;
    using Extensions;

    public class Beacon : Entity
    {
        private readonly HashSet<string> allowedEffects;

        public Beacon(
            string name, IEnumerable<string> allowedEffects,
            double distributionEffectivity, int moduleSlots)
            : base(name)
        {
            this.allowedEffects = allowedEffects.ToSet();
            DistributionEffectivity = distributionEffectivity;
            ModuleSlots = moduleSlots;
        }

        public IReadOnlyCollection<string> AllowedEffects => allowedEffects;
        public double DistributionEffectivity { get; }
        public int ModuleSlots { get; }

        public bool Allows(Module module)
        {
            if (module.SpeedBonus != 0 && !AllowedEffects.Contains("speed"))
                return false;
            if (module.ProductivityBonus != 0 && !AllowedEffects.Contains("productivity"))
                return false;
            if (module.ConsumptionBonus != 0 && !AllowedEffects.Contains("consumption"))
                return false;

            return allowedEffects.IsSupersetOf(module.EnumerateEffects());
        }
    }
}
