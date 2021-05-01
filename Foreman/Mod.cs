namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Windows.Media.Imaging;
    using Extensions;
    using NLua;

    public class Mod : IDisposable
    {
        private bool registered;
        private bool loaderRegistered;
        private Lua currLua;
        private ZipArchive archive;

        public Mod(string modPath, bool isZippedMod)
        {
            ModPath = modPath;
            IsZippedMod = isZippedMod;
        }

        public string ModPath { get; }
        public bool IsZippedMod { get; }

        public string Name { get; set; } = "";
        public string Title { get; set; } = "";
        public string Version { get; set; } = "";
        public Version ParsedVersion { get; set; }
        public string Description { get; set; } = "";
        public string Author { get; set; } = "";
        public List<string> Dependencies { get; set; } = new();
        public List<ModDependency> ParsedDependencies { get; set; } = new();
        public bool Enabled { get; set; } = true;

        private ZipArchive Archive =>
            archive ??= new ZipArchive(File.OpenRead(ModPath));

        public bool SatisfiesDependency(ModDependency dep)
        {
            if (Name != dep.ModName)
                return false;

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

        public void Dispose()
        {
            archive.Dispose();
        }

        public readonly struct ModFile
        {
            private readonly ZipArchiveEntry entry;
            private readonly string path;

            public ModFile(string name, string path)
            {
                Name = name;
                this.path = path;
                entry = null;
            }

            public ModFile(ZipArchiveEntry entry)
            {
                Name = entry.FullName;
                this.entry = entry;
                path = null;
            }

            public string Name { get; }

            public Stream Open()
            {
                if (entry == null)
                    return File.OpenRead(path);
                return entry.Open();
            }

            public string ReadAllText()
            {
                if (entry == null)
                    return File.ReadAllText(Name);

                using var reader = new StreamReader(entry.Open());
                return reader.ReadToEnd();
            }
        }

        public IEnumerable<ModFile> EnumerateFiles(string relativePath, string searchPattern)
        {
            if (!IsZippedMod) {
                var topDir = Path.Combine(ModPath, relativePath);
                foreach (var file in Directory.GetFiles(topDir, searchPattern))
                    yield return new ModFile(file, file);

                yield break;
            }

            Regex regex = ConvertPatternToRegex(searchPattern);
            foreach (var entry in Archive.Entries) {
                if (entry.FullName == relativePath && regex.IsMatch(entry.Name))
                    yield return new ModFile(entry);
            }
        }

        private static Regex ConvertPatternToRegex(string pattern)
        {
            pattern = pattern.Replace(".", "\\.").Replace("*", ".*");
            return new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public void Load(Lua lua, string filePath)
        {
            if (!IsZippedMod) {
                string dataFile = Path.Combine(ModPath, filePath);
                if (File.Exists(dataFile))
                    lua.DoFile(dataFile);
            } else {
                var entry = Archive.GetEntry(MakeEntryName(filePath));
                if (entry != null)
                    lua.DoString(entry.ReadAllText());
            }
        }

        public Stream OpenFile(string filePath)
        {
            if (IsZippedMod)
                return TryGetArchiveEntry(filePath)?.Open();

            string fullPath = Path.Combine(ModPath, filePath);
            if (File.Exists(fullPath))
                return File.OpenRead(fullPath);

            return null;
        }

        private ZipArchiveEntry TryGetArchiveEntry(string filePath)
        {
            return Archive.GetEntry(MakeEntryName(filePath));
        }

        private string MakeEntryName(string filePath)
        {
            return $"{Name}_{Version}/{filePath}";
        }

        public BitmapSource LoadImage(string filePath, int? iconSize = null)
        {
            using Stream input = OpenFile(filePath);
            if (input == null)
                return null;
            return ImagingExtensions.LoadImage(input, iconSize);
        }

        public void Register(Lua lua)
        {
            if (registered)
                return;

            if (!IsZippedMod)
                AddPackagePath(lua);
            else
                AddPackageSearcher(lua);

            currLua = lua;
            registered = true;
        }

        public void Unregister(Lua lua)
        {
            if (!registered)
                return;

            if (!IsZippedMod)
                RemovePackagePath(lua);
            else
                RemovePackageSearcher(lua);

            currLua = null;
            registered = false;
        }

        private string CreatePackagePathFragment()
        {
            return $";{ModPath}{Path.DirectorySeparatorChar}?.lua";
        }

        private void AddPackagePath(Lua lua)
        {
            var path = (string)lua["package.path"];
            lua["package.path"] = path + CreatePackagePathFragment();
        }

        private void RemovePackagePath(Lua lua)
        {
            var path = (string)lua["package.path"];
            lua["package.path"] = path.Replace(CreatePackagePathFragment(), string.Empty);
        }

        private string ReqPath => $"foreman.mods.req.{Name}";
        private string SearcherPath => $"foreman.mods.req.{Name}.search";

        private void AddPackageSearcher(Lua lua)
        {
            if (!loaderRegistered) {
                lua.DoString($@"
                    if not foreman then foreman = {{}} end
                    if not foreman.mods then foreman.mods = {{}} end
                    if not foreman.mods.searchers then foreman.mods.req = {{}} end
                    if not foreman.mods.searchers then foreman.mods.req.{Name} = {{}} end
                ");

                lua.RegisterFunction(ReqPath + ".search_impl", this,
                    GetType().GetMethod(nameof(PackageSearch), BindingFlags.Instance | BindingFlags.NonPublic));
                lua.RegisterFunction(ReqPath + ".load_impl", this,
                    GetType().GetMethod(nameof(PackageLoader), BindingFlags.Instance | BindingFlags.NonPublic));

                lua.DoString($@"
                    {ReqPath}.load = function(mod, arg)
                        {ReqPath}.load_impl(mod, arg)
                    end
                    {ReqPath}.search = function(mod)
                        local loader, arg = {ReqPath}.search_impl(mod)
                        if type(loader) == 'string' then
                            return loader
                        end
                        return {ReqPath}.load, arg
                    end
                ");
                loaderRegistered = true;
            }

            lua.DoString($"table.insert(package.searchers, {SearcherPath})");
        }

        private void RemovePackageSearcher(Lua lua)
        {
            lua.DoString($@"
              for k, v in pairs(package.searchers) do
                if v == {SearcherPath} then
                  table.remove(package.searchers, k)
                  break
                end
              end
            ");
        }

        private object PackageSearch(string modulePath, out object loaderArg)
        {
            var entryName = MakeEntryName(modulePath.Replace('.', '/') + ".lua");
            var entry = Archive.GetEntry(entryName);
            if (entry == null) {
                loaderArg = null;
                return $"module '{modulePath}' not found";
            }

            loaderArg = entryName;
            return null;
        }

        private void PackageLoader(string name, string entryName)
        {
            var entry = Archive.GetEntry(entryName);
            currLua.DoString(entry.ReadAllText());
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
