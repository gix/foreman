namespace Foreman
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Runtime.Serialization;
    using Extensions;
    using Newtonsoft.Json.Linq;

    [Serializable]
    public abstract class ModuleSelector : ISerializable
    {
        public abstract void GetObjectData(SerializationInfo info, StreamingContext context);
        public abstract string Name { get; }

        public static DefaultModuleSelector Default { get; } = new();
        public static ModuleSelector None { get; } = new ModuleSelectorNone();
        public static ModuleSelector Fastest { get; } = new ModuleSelectorFastest();
        public static ModuleSelector Productive { get; } = new ModuleSelectorProductivity();
        public static ModuleSelector Efficient { get; } = new ModuleSelectorEfficiency();

        public static ModuleSelector FromName(string name)
        {
            return name switch {
                "None" => None,
                "Fastest" => Fastest,
                "Most Productive" => Productive,
                "Most Efficient" => Efficient,
                _ => None,
            };
        }

        public static ModuleSelector Load(JToken token)
        {
            ModuleSelector filter = Fastest;

            var filterType = token["ModuleFilterType"]?.Value<string>();
            switch (filterType) {
                case "Default":
                    filter = Default;
                    break;
                case "None":
                    filter = None;
                    break;
                case "Fastest":
                    filter = Fastest;
                    break;
                case "Most Productive":
                    filter = Productive;
                    break;
                case "Most Efficient":
                    filter = Efficient;
                    break;
                case "Specific":
                    var moduleKey = token["Module"]?.Value<string>();
                    if (moduleKey != null && DataCache.Current.Modules.ContainsKey(moduleKey))
                        filter = new ModuleSpecificFilter(DataCache.Current.Modules[moduleKey]);
                    break;
                case "Custom":
                    var modulesToken = token["Modules"];
                    if (modulesToken != null) {
                        var moduleKeys = modulesToken.Values<string>();
                        filter = new ModuleSet(
                            moduleKeys
                            .Select(x => x != null ? DataCache.Current.Modules.GetValueOrDefault(x) : null));
                    }
                    break;
            }

            return filter;
        }

        protected abstract IEnumerable<Module> AvailableModules();

        public virtual IEnumerable<Module> For(Assembler assembler, Recipe recipe, int moduleSlots)
        {
            var modules = AvailableModules()
                .Where(m => m.Enabled)
                .Where(m => m.AllowedIn(assembler, recipe))
                .Take(1);

            return Enumerable.Repeat(modules, moduleSlots)
                .SelectMany(x => x);
        }

        public virtual IEnumerable<Module> For(Miner miner, Resource resource, int moduleSlots)
        {
            var modules = AvailableModules()
                .Where(m => m.Enabled)
                .Where(m => m.AllowedIn(miner, resource))
                .Take(1);

            return Enumerable.Repeat(modules, moduleSlots)
                .SelectMany(x => x);
        }

        public abstract ModuleSelector Clone();

        public static ModuleSelector Specific(Module module)
        {
            return new ModuleSpecificFilter(module);
        }

        [Serializable]
        private sealed class ModuleSpecificFilter : ModuleSelector
        {
            public Module Module { get; }

            public ModuleSpecificFilter(Module module)
            {
                Module = module;
            }

            public override string Name => Module.FriendlyName;

            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("ModuleFilterType", "Specific");
                info.AddValue("Module", Module.Name);
            }

            protected override IEnumerable<Module> AvailableModules()
            {
                return Enumerable.Repeat(Module, 1);
            }

            public override ModuleSelector Clone()
            {
                return new ModuleSpecificFilter(Module);
            }
        }

        [Serializable]
        public sealed class DefaultModuleSelector : ModuleSelector
        {
            private ModuleSelector? strategy;

            [AllowNull]
            public ModuleSelector Strategy
            {
                get => strategy ?? None;
                set => strategy = value;
            }

            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("ModuleFilterType", "Default");
            }

            public override string Name => "Default";

            protected override IEnumerable<Module> AvailableModules()
            {
                return Strategy.AvailableModules();
            }

            public override ModuleSelector Clone()
            {
                return this;
            }
        }

        [Serializable]
        private sealed class ModuleSelectorNone : ModuleSelector
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

            public override ModuleSelector Clone()
            {
                return this;
            }
        }

        [Serializable]
        private class ModuleSelectorFastest : ModuleSelector
        {
            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("ModuleFilterType", "Fastest");
            }

            public override string Name => "Fastest";

            protected override IEnumerable<Module> AvailableModules()
            {
                return DataCache.Current.Modules.Values.OrderBy(m => -m.SpeedBonus);
            }

            public override ModuleSelector Clone()
            {
                return this;
            }
        }

        [Serializable]
        private class ModuleSelectorProductivity : ModuleSelector
        {
            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("ModuleFilterType", "Most Productive");
            }

            public override string Name => "Most Productive";

            protected override IEnumerable<Module> AvailableModules()
            {
                return DataCache.Current.Modules.Values.OrderBy(m => -m.ProductivityBonus);
            }

            public override ModuleSelector Clone()
            {
                return this;
            }
        }

        [Serializable]
        private class ModuleSelectorEfficiency : ModuleSelector
        {
            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("ModuleFilterType", "Most Efficient");
            }

            public override string Name => "Most Efficient";

            protected override IEnumerable<Module> AvailableModules()
            {
                return DataCache.Current.Modules.Values.OrderBy(m => m.ConsumptionBonus);
            }

            public override ModuleSelector Clone()
            {
                return this;
            }
        }
    }

    [Serializable]
    public class ModuleSet : ModuleSelector, IReadOnlyList<Module?>
    {
        private readonly List<Module?> modules;

        public ModuleSet()
        {
            modules = new List<Module?>();
        }

        public ModuleSet(IEnumerable<Module?> modules)
        {
            this.modules = new List<Module?>(modules);
        }

        public override string Name => "Custom";

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ModuleFilterType", "Custom");
            info.AddValue("Modules", modules.Select(x => x?.Name).ToArray());
        }

        public override ModuleSelector Clone()
        {
            return new ModuleSet(modules);
        }

        protected override IEnumerable<Module> AvailableModules()
        {
            return Enumerable.Empty<Module>();
        }

        public override IEnumerable<Module> For(Assembler assembler, Recipe recipe, int moduleSlots)
        {
            return modules.NotNull().Where(x => x.AllowedIn(assembler, recipe)).Take(moduleSlots);
        }

        public override IEnumerable<Module> For(Miner miner, Resource resource, int moduleSlots)
        {
            return modules.NotNull().Where(x => x.AllowedIn(miner, resource)).Take(moduleSlots);
        }

        public int Count => modules.Count;

        public Module? this[int index]
        {
            get => index >= 0 && index < Count ? modules[index] : null;
            set
            {
                if (index >= Count)
                    Resize(index + 1);
                modules[index] = value;
            }
        }

        public void Resize(int size)
        {
            if (size > modules.Count)
                modules.AddRange(
                    Enumerable.Repeat<Module?>(null, size - modules.Count));
            else if (size < modules.Count)
                modules.RemoveRange(size, modules.Count - size);
        }

        public IEnumerator<Module> GetEnumerator()
        {
            return modules.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)modules).GetEnumerator();
        }
    }
}
