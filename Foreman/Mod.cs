namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Mod
    {
        public string Name { get; set; } = "";
        public string Title { get; set; } = "";
        public string Version { get; set; } = "";
        public Version ParsedVersion { get; set; }
        public string Dir { get; set; } = "";
        public string Description { get; set; } = "";
        public string Author { get; set; } = "";
        public List<string> Dependencies { get; set; } = new List<string>();
        public List<ModDependency> ParsedDependencies { get; set; } = new List<ModDependency>();
        public bool Enabled { get; set; } = true;

        public bool SatisfiesDependency(ModDependency dep)
        {
            if (Name != dep.ModName) {
                return false;
            }
            if (dep.Version != null) {
                if (dep.VersionType == DependencyType.EqualTo
                    && ParsedVersion != dep.Version) {
                    return false;
                }
                if (dep.VersionType == DependencyType.GreaterThan
                    && ParsedVersion <= dep.Version) {
                    return false;
                }
                if (dep.VersionType == DependencyType.GreaterThanOrEqual
                    && ParsedVersion < dep.Version) {
                    return false;
                }
            }
            return true;
        }

        public bool DependsOn(Mod mod, bool ignoreOptional)
        {
            IEnumerable<ModDependency> depList;
            if (ignoreOptional)
                depList = ParsedDependencies.Where(d => !d.Optional);
            else
                depList = ParsedDependencies;

            return depList.Any(mod.SatisfiesDependency);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class ModDependency
    {
        public ModDependency(string modName, Version version, bool optional, DependencyType versionType)
        {
            ModName = modName;
            Version = version;
            Optional = optional;
            VersionType = versionType;
        }

        public string ModName { get; }
        public Version Version { get; }
        public bool Optional { get; }
        public DependencyType VersionType { get; }
    }

    public enum DependencyType
    {
        EqualTo,
        GreaterThan,
        GreaterThanOrEqual
    }
}
