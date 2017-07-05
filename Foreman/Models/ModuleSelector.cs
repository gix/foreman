namespace Foreman
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using Newtonsoft.Json.Linq;

    public abstract class ModuleSelector : ISerializable
    {
        public abstract void GetObjectData(SerializationInfo info, StreamingContext context);
        public abstract string Name { get; }

        public static ModuleSelector Fastest => new ModuleSelectorFastest();

        public static ModuleSelector None => new ModuleSelectorNone();

        public static ModuleSelector Productive => new ModuleSelectorProductivity();

        public static ModuleSelector Load(JToken token)
        {
            ModuleSelector filter = Fastest;

            if (token["ModuleFilterType"] != null) {
                switch ((string)token["ModuleFilterType"]) {
                    case "Best":
                        filter = Fastest;
                        break;
                    case "None":
                        filter = None;
                        break;
                    case "Most Productive":
                        filter = Productive;
                        break;
                    case "Specific":
                        if (token["Module"] != null) {
                            var moduleKey = (string)token["Module"];
                            if (DataCache.Modules.ContainsKey(moduleKey)) {
                                filter = new ModuleSpecificFilter(DataCache.Modules[moduleKey]);
                            }
                        }
                        break;
                }
            }

            return filter;
        }

        protected abstract IEnumerable<Module> AvailableModules();

        public IEnumerable<Module> For(Recipe recipe, int moduleSlots)
        {
            var modules = AvailableModules()
                .Where(m => m.Enabled)
                .Where(m => m.AllowedIn(recipe))
                .Take(1);

            return Enumerable.Repeat(modules, moduleSlots)
                .SelectMany(x => x);
        }

        public static ModuleSelector Specific(Module module)
        {
            return new ModuleSpecificFilter(module);
        }

        private class ModuleSpecificFilter : ModuleSelector
        {
            public Module Module { get; }

            public ModuleSpecificFilter(Module module)
            {
                Module = module;
            }

            public override string Name => Module.Name;

            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("ModuleFilterType", "Specific");
                info.AddValue("Module", Module.Name);
            }

            protected override IEnumerable<Module> AvailableModules()
            {
                return Enumerable.Repeat(Module, 1);
            }
        }

        private class ModuleSelectorFastest : ModuleSelector
        {
            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("ModuleFilterType", "Best");
            }

            public override string Name => "Fastest";

            protected override IEnumerable<Module> AvailableModules()
            {
                return DataCache.Modules.Values
                    .OrderBy(m => -m.SpeedBonus);
            }
        }

        private class ModuleSelectorProductivity : ModuleSelector
        {
            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("ModuleFilterType", "Most Productive");
            }

            public override string Name => "Most Productive";

            protected override IEnumerable<Module> AvailableModules()
            {
                return DataCache.Modules.Values
                    .OrderBy(m => -m.ProductivityBonus);
            }
        }

        private class ModuleSelectorNone : ModuleSelector
        {
            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("ModuleFilterType", "None");
            }

            public override string Name => "None";

            protected override IEnumerable<Module> AvailableModules()
            {
                return Enumerable.Empty<Module>();
            }
        }
    }
}
