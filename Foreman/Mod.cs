namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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
        private Lua? currLua;
        private ZipArchive? archive;

        public Mod(string modPath, bool isZippedMod)
        {
            ModPath = modPath;
            IsZippedMod = isZippedMod;
        }

        public string ModPath { get; }
        public bool IsZippedMod { get; }

        public string Name { get; init; } = "";
        public string Version { get; init; } = "";
        public Version ParsedVersion { get; init; } = new();
        public string? Title { get; init; }
        public string? Author { get; init; }

        public List<string> Dependencies { get; } = new();
        public List<ModDependency> ParsedDependencies { get; } = new();

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
            archive?.Dispose();
        }

        public readonly struct ModFile
        {
            private readonly ZipArchiveEntry? entry;
            private readonly string? path;

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
                    return File.OpenRead(path!);
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

            Regex regex = ConvertPatternToRegex(relativePath, searchPattern);
            foreach (var entry in Archive.Entries) {
                if (regex.IsMatch(entry.FullName))
                    yield return new ModFile(entry);
            }
        }

        private static Regex ConvertPatternToRegex(string relativePath, string searchPattern)
        {
            searchPattern = searchPattern.Replace(".", "\\.").Replace("*", ".*");
            string pattern = relativePath.Replace('\\', '/') + '/' + searchPattern;
            return new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public bool Load(Lua lua, string filePath)
        {
            if (!IsZippedMod) {
                string dataFile = Path.Combine(ModPath, filePath);
                if (File.Exists(dataFile)) {
                    lua.DoFile(dataFile);
                    return true;
                }
            } else {
                var entry = Archive.GetEntry(MakeEntryName(filePath));
                if (entry != null) {
                    var code = entry.ReadAllText();
                    lua.DoString(code);
                    return true;
                }
            }

            return false;
        }

        public object? Loader(Lua lua, string entryName)
        {
            if (!IsZippedMod) {
                string dataFile = Path.Combine(ModPath, entryName);
                if (File.Exists(dataFile))
                    return lua.DoFile(dataFile)?[0];
            } else {
                var entry = Archive.GetEntry(entryName);
                if (entry != null) {
                    var code = entry.ReadAllText();
                    return lua.DoString(code)?[0];
                }
            }

            return null;
        }

        public Stream? OpenFile(string filePath)
        {
            if (IsZippedMod)
                return TryGetArchiveEntry(filePath)?.Open();

            string fullPath = Path.Combine(ModPath, filePath);
            if (File.Exists(fullPath))
                return File.OpenRead(fullPath);

            return null;
        }

        private ZipArchiveEntry? TryGetArchiveEntry(string filePath)
        {
            return Archive.GetEntry(MakeEntryName(filePath));
        }

        private string MakeEntryName(string filePath)
        {
            return $"{Name}_{Version}/{filePath}";
        }

        public BitmapSource? LoadImage(string filePath, int? iconSize = null)
        {
            using Stream? input = OpenFile(filePath);
            if (input == null)
                return null;
            return ImagingExtensions.LoadImage(input, iconSize);
        }

        public void Register(Lua lua)
        {
            if (registered)
                return;

            //if (!IsZippedMod)
            //    AddPackagePath(lua);
            //else
            //    AddPackageSearcher(lua);

            currLua = lua;
            registered = true;
        }

        public void Unregister(Lua lua)
        {
            if (!registered)
                return;

            //if (!IsZippedMod)
            //    RemovePackagePath(lua);
            //else
            //    RemovePackageSearcher(lua);

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
        private string SearcherPath => $"{ReqPath}.search";
        private LuaFunction? searcher;
        private LuaFunction? loader;

        private void AddPackageSearcher(Lua lua)
        {
            if (!loaderRegistered) {
                lua.DoString($@"
                    if not foreman then foreman = {{}} end
                    if not foreman.mods then foreman.mods = {{}} end
                    if not foreman.mods.req then foreman.mods.req = {{}} end
                    if not foreman.mods.req.{Name} then foreman.mods.req.{Name} = {{}} end
                ");

                searcher = lua.RegisterFunction(ReqPath + ".search_impl", this,
                    GetType().GetMethod(nameof(ZipPackageSearch), BindingFlags.Instance | BindingFlags.NonPublic));
                loader = lua.RegisterFunction(ReqPath + ".load_impl", this,
                    GetType().GetMethod(nameof(ZipPackageLoader), BindingFlags.Instance | BindingFlags.NonPublic));

                lua.DoString($@"
                    {ReqPath}.load = function(mod, arg)
                        return {ReqPath}.load_impl(mod, arg)
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
            loader?.Dispose();
            loader = null;
            searcher?.Dispose();
            searcher = null;

            lua.DoString($@"
              foreman.mods.req.{Name} = nil

              for k, v in ipairs(package.searchers) do
                if v == {SearcherPath} then
                  table.remove(package.searchers, k)
                  break
                end
              end
            ");
        }

        public bool Search(string modulePath, out object? loaderArg)
        {
            loaderArg = null;
            if (IsZippedMod) {
                string relPath = modulePath.Replace('.', '/') + ".lua";

                var entryName = MakeEntryName(relPath);
                var entry = Archive.GetEntry(entryName);
                if (entry == null)
                    return false;

                loaderArg = entryName;
            } else {
                string relPath = modulePath.Replace('.', Path.DirectorySeparatorChar) + ".lua";

                string fullPath = Path.Combine(ModPath, relPath);
                if (!File.Exists(fullPath))
                    return false;

                loaderArg = fullPath;
            }

            return true;
        }

        private object? ZipPackageSearch(string modulePath, out object? loaderArg)
        {
            var entryName = MakeEntryName(modulePath.Replace('.', '/') + ".lua");
            var entry = Archive.GetEntry(entryName);
            if (entry == null) {
                loaderArg = null;
                return $"module '{modulePath}' not found";
            }

            loaderArg = entryName;
            return loader;
        }

        private object? ZipPackageLoader(string name, string entryName)
        {
            var entry = Archive.GetEntry(entryName);
            if (entry == null)
                return null;
            var code = entry.ReadAllText();
            var result = currLua!.DoString(code);
            return result?[0];
        }
    }

    public class ModDependency
    {
        public ModDependency(
            string modName, ModDependencyKind kind, Version? version, DependencyType versionType)
        {
            ModName = modName;
            Kind = kind;
            Version = version;
            VersionType = versionType;
        }

        public string ModName { get; }
        public ModDependencyKind Kind { get; }
        public Version? Version { get; }
        public bool Optional => Kind == ModDependencyKind.Optional || Kind == ModDependencyKind.HiddenOptional;
        public DependencyType VersionType { get; }
    }

    public enum ModDependencyKind
    {
        Required,
        Optional,
        HiddenOptional,
        DoesNotAffectLoaderOrder,
        Incompatible,
    }

    public enum DependencyType
    {
        EqualTo,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual
    }
}
