namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Extensions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NLua;
    using Properties;
    using Units;
    using Application = System.Windows.Forms.Application;
    using Path = System.IO.Path;

    public class Language
    {
        public Language(string name)
        {
            Name = name;
        }

        public string Name { get; }
        private string? localName;

        [AllowNull]
        public string LocalName
        {
            get => !string.IsNullOrWhiteSpace(localName) ? localName : Name;
            set => localName = value;
        }
    }

    public class LocalizedStringDictionary
    {
        private readonly Dictionary<(string, string), string> dictionary = new();

        public void Clear()
        {
            dictionary.Clear();
        }

        [MaybeNull]
        public string this[string section, string key]
        {
            get => dictionary.GetValueOrDefault((section, key));
            set => dictionary[(section, key)] = value;
        }

        public bool TryGetValue(string section, string key, [MaybeNullWhen(false)] out string value)
        {
            return dictionary.TryGetValue((section, key), out value);
        }
    }

    public abstract class LocalizationInfo
    {
        public abstract string? Interpolate(LocalizedStringDictionary localized);

        public static LocalizationInfo Create(
            string section, string name, string placeholderSection, string placeholderName)
        {
            return new SingleLocalizationInfo(section, name, placeholderSection, placeholderName);
        }

        public static LocalizationInfo Create(
            string section, string name, List<string> placeholders)
        {
            return new MultiLocalizationInfo(section, name, placeholders);
        }

        private sealed class SingleLocalizationInfo : LocalizationInfo
        {
            private readonly string section;
            private readonly string name;
            private readonly string placeholderSection;
            private readonly string placeholderName;

            public SingleLocalizationInfo(
                string section, string name, string placeholderSection, string placeholderName)
            {
                this.section = section;
                this.name = name;
                this.placeholderSection = placeholderSection;
                this.placeholderName = placeholderName;
            }

            public override string? Interpolate(LocalizedStringDictionary localized)
            {
                return localized[section, name]?.Replace(
                    "__1__", localized[placeholderSection, placeholderName]);
            }
        }

        private sealed class MultiLocalizationInfo : LocalizationInfo
        {
            private readonly string section;
            private readonly string name;
            private readonly List<string> placeholders;

            public MultiLocalizationInfo(
                string section, string name, List<string> placeholders)
            {
                this.section = section;
                this.name = name;
                this.placeholders = placeholders;
            }

            public override string? Interpolate(LocalizedStringDictionary localized)
            {
                var str = localized[section, name];
                if (str == null)
                    return null;

                for (int p = 1, i = 0, e = placeholders.Count / 2; i < e; ++p, i += 2) {
                    str = str.Replace(
                        $"__{p}__",
                        localized[placeholders[i],
                            placeholders[i + 1]]);
                }

                return str;
            }
        }
    }

    public interface ILogger
    {
        void Log(string format, params object?[] args);
    }

    public class DebugLogger : ILogger
    {
        public void Log(string format, params object?[] args)
        {
            Debugger.Log(0, null, string.Format(format, args));
        }
    }

    public class DataCache
    {
        private static DataCache current = new();

        public DataCache()
        {
            UnknownIcon = LoadUnknownIcon();
        }

        public static DataCache Current
        {
            get => current;
            private set
            {
                current = value;
                colorCache.Clear();
            }
        }

        private string DataPath => Path.Combine(Settings.Default.FactorioPath, "data");

        private string ModPath => Settings.Default.FactorioModPath;

        private Mod? coreMod;
        public List<Mod> Mods { get; set; } = new();
        public List<Language> Languages { get; } = new();

        public string Difficulty { get; set; } = "normal";
        private const string DefaultLocale = "en";

        public Dictionary<string, Item> Items { get; } = new();
        public Dictionary<string, Recipe> Recipes { get; } = new();
        public Dictionary<string, Assembler> Assemblers { get; } = new();
        public Dictionary<string, Miner> Miners { get; } = new();
        public Dictionary<string, Resource> Resources { get; } = new();
        public Dictionary<string, Module> Modules { get; } = new();
        public Dictionary<string, Beacon> Beacons { get; } = new();
        public Dictionary<string, Inserter> Inserters { get; } = new();

        private const float DefaultRecipeTime = 0.5f;
        private static readonly Dictionary<BitmapSource, Color> colorCache = new();
        public BitmapSource UnknownIcon { get; }

        [AllowNull]
        public ILogger Logger
        {
            get => logger;
            set => logger = value ?? new DebugLogger();
        }

        private LocalizedStringDictionary localeFiles = new();
        private ILogger logger = new DebugLogger();

        public Dictionary<string, Exception> FailedFiles { get; } = new();
        public Dictionary<string, Exception> FailedModRegistrations { get; } = new();

        public Dictionary<string, byte[]> ZipHashes { get; } = new();

        public IEnumerable<Recipe> RecipesSupplying(Item item)
        {
            return Recipes.Values.Where(x => x.Enabled && x.Results.ContainsKey(item));
        }

        public IEnumerable<Recipe> RecipesConsuming(Item item)
        {
            return Recipes.Values.Where(x => x.Enabled && x.Ingredients.ContainsKey(item));
        }

        public static Task Reload()
        {
            return Reload(null);
        }

        public static async Task Reload(List<string>? enabledMods)
        {
            var newData = new DataCache();
            newData.Logger = Current.Logger;
            newData.Difficulty = Current.Difficulty;
            await newData.LoadAllData(enabledMods);
            Current = newData;
        }

        private class FactorioLua : Lua
        {
            private readonly ILogger logger;
            private readonly LuaFunction print;
            private readonly LuaFunction log;
            private readonly LuaFunction tableSize;
            private readonly LuaFunction searcher;
            private readonly LuaFunction loader;
            private Mod? currentMod;
            private Dictionary<string, Mod> mods = new();

            public FactorioLua(ILogger logger)
            {
                this.logger = logger;
                logger.Log("LUA: Initializing new interpreter");

                print = RegisterFunction(
                    "print",
                    this,
                    typeof(FactorioLua).GetMethod(
                        nameof(Print), BindingFlags.Instance | BindingFlags.NonPublic));
                log = RegisterFunction(
                    "log",
                    this,
                    typeof(FactorioLua).GetMethod(
                        nameof(Log), BindingFlags.Instance | BindingFlags.NonPublic));

                tableSize = RegisterFunction(
                    "table_size",
                    typeof(FactorioLua).GetMethod(
                        nameof(TableSize), BindingFlags.Static | BindingFlags.NonPublic));

                // Remove all-in-one loader searcher
                DoString(@"table.remove(package.searchers, 4)");
                // Remove package.cpath searcher
                DoString(@"table.remove(package.searchers, 3)");
                // Remove package.path searcher
                DoString(@"table.remove(package.searchers, 2)");
                // Remove package.preload searcher
                DoString(@"table.remove(package.searchers, 1)");

                searcher = RegisterFunction("__foreman_searcher__", this,
                    GetType().GetMethod(nameof(PackageSearcher), BindingFlags.Instance | BindingFlags.NonPublic));
                loader = RegisterFunction("__foreman_loader__", this,
                    GetType().GetMethod(nameof(PackageLoader), BindingFlags.Instance | BindingFlags.NonPublic));

                DoString(@"
                    local loader_wrapper = function(modname, arg)
                        return __foreman_loader__(modname, arg)
                    end
                    local searcher_wrapper = function(modname)
                        local loader, arg = __foreman_searcher__(modname)
                        if not loader or type(loader) == 'string' then
                            return loader
                        end
                        return loader_wrapper, arg
                    end
                    table.insert(package.searchers, searcher_wrapper)
                ");

                DoString("serpent = require('serpent')");
            }

            public override void Dispose()
            {
                loader.Dispose();
                searcher.Dispose();
                tableSize.Dispose();
                base.Dispose();
            }

            public void SetMods(List<Mod> mods)
            {
                this.mods = mods.ToDictionary(x => x.Name);

                // https://lua-api.factorio.com/latest/Data-Lifecycle.html:
                //   Additionally, a global table named mods exists that contains
                //   a mapping of mod name to mod version for all enabled mods.

                NewTable("mods");
                using var table = GetTable("mods");
                foreach (Mod mod in mods)
                    table[mod.Name] = mod.Version;
            }

            private void Print(params object[] args)
            {
                string fmt = args.Length switch {
                    0 => "\n",
                    1 => "{0}\n",
                    2 => "{0}, {1}\n",
                    3 => "{0}, {1}, {2}\n",
                    4 => "{0}, {1}, {2}, {3}\n",
                    { } n => string.Join(", ", Enumerable.Range(0, n - 1).Select(x => "{" + x + '}')) + '\n',
                };

                logger.Log(fmt, args);
            }

            private void Log(params object[] args)
            {
                string fmt = args.Length switch {
                    0 => "\n",
                    1 => "{0}\n",
                    2 => "{0}, {1}\n",
                    3 => "{0}, {1}, {2}\n",
                    4 => "{0}, {1}, {2}, {3}\n",
                    { } n => string.Join(", ", Enumerable.Range(0, n - 1).Select(x => "{" + x + '}')) + '\n',
                };

                logger.Log(fmt, args);
            }

            private static int TableSize(LuaTable table)
            {
                return table.Keys.Count;
            }

            private object? PackageSearcher(string module, out object? loaderArg)
            {
                loaderArg = null;

                if (module == "serpent") {
                    logger.Log("LUA: Resolving [{0}] '{1}' with: <internal module>", currentMod?.Name, module);
                    return loader;
                }

                if (module.StartsWith("__")) {
                    var match = Regex.Match(module, @"\A__(?<mod>[^\\/\.]+)__[/%.](?<path>.+)\z");
                    if (match.Success) {
                        string modName = match.Groups["mod"].Value;
                        string path = match.Groups["path"].Value;
                        if (mods.TryGetValue(modName, out var explicitMod)) {
                            if (explicitMod.Search(path, out loaderArg)) {
                                logger.Log("LUA: Resolving [{0}] '{1}' with: mod='{2}', entry='{3}'", currentMod?.Name, module, explicitMod.Name, loaderArg);
                                return loader;
                            }
                        }

                        logger.Log("LUA: Resolving [{0}] '{1}' with: NOT FOUND!", currentMod?.Name, module);
                        return null;
                    }
                }

                if (currentMod != null && currentMod.Search(module, out loaderArg)) {
                    logger.Log("LUA: Resolving [{0}] '{1}' with: mod='{2}', entry='{3}'", currentMod.Name, module, currentMod.Name, loaderArg);
                    return loader;
                }

                if (mods.TryGetValue("core", out Mod? coreMod)) {
                    string relPath = module.Replace('.', '/') + ".lua";
                    string path = Path.Combine(coreMod.ModPath, "lualib", relPath);
                    if (File.Exists(path)) {
                        loaderArg = path;
                        logger.Log("LUA: Resolving [{0}] '{1}' with: mod='{2}', entry='{3}'", currentMod?.Name, module, coreMod.Name, loaderArg);
                        return loader;
                    }
                }

                logger.Log("LUA: Resolving [{0}] '{1}' with: NOT FOUND!", currentMod?.Name, module);
                return null;
            }

            private object? PackageLoader(string module, string path)
            {
                if (module == "serpent") {
                    using var stream = GetType().Assembly
                        .GetManifestResourceStream(typeof(Resources), "Serpent.lua");
                    return DoString(stream!.ReadAllText())?[0];
                }

                if (currentMod == null)
                    return null;

                object? result;
                if (Path.IsPathRooted(path) && File.Exists(path)) {
                    //result = DoFile(path)?[0];
                    result = DoString(File.ReadAllText(path))?[0];
                } else {
                    result = currentMod.Loader(this, path);
                }

                return result;
            }

            public void PushMod(Mod mod)
            {
                currentMod = mod;
                mod.Register(this);
            }

            public void PopMod(Mod mod)
            {
                mod.Unregister(this);
                currentMod = null;
            }
        }

        private FactorioLua CreateFactorioLua(ILogger logger)
        {
            var lua = new FactorioLua(logger);

            var asm = GetType().Assembly;
            using (var stream = asm.GetManifestResourceStream(typeof(Resources), "FactorioDefines.lua")) {
                lua.DoString(stream!.ReadAllText());
            }

            //            lua.DoString(@"
            //                local orig_require = require
            //
            //                function relative_require(modname)
            //                  if string.match(modname, '__.+__[/%.]') then
            //                    return orig_require(string.gsub(modname, '__.+__[/%.]', ''))
            //                  end
            //
            //                  local regular_searcher = package.searchers[1]
            //                  local searcher = function(inner)
            //                    if string.match(modname, '(.*)%.') then
            //                      return regular_searcher(string.match(modname, '(.*)%.') .. '.' .. inner)
            //                    end
            //                  end
            //                  table.insert(package.searchers, 1, searcher)
            //                  local result = orig_require(modname)
            //                  table.remove(package.searchers, 1)
            //                  return result
            //                end
            //                _G.require = relative_require
            //");

            //AddLuaPackagePath(lua, Path.Combine(DataPath, "core", "lualib")); // Core lua functions
            //AddLuaPackagePath(lua, Path.Combine(DataPath, "base")); // Base mod

            return lua;
        }

        private async Task LoadAllData(List<string>? enabledMods)
        {
            Clear();
            FindAllMods(enabledMods);

            var orderedMods = Mods.Where(m => m.Enabled).ToList();

            Dictionary<string, Dictionary<string, object>> settingsMap;

            // 1. settings stage
            using (var lua = CreateFactorioLua(Logger)) {
                string basePackagePath = (string)lua["package.path"];

                string dataloaderFile = Path.Combine(DataPath, "core", "lualib", "dataloader.lua");
                try {
                    lua.DoFile(dataloaderFile);
                } catch (Exception ex) {
                    FailedFiles[dataloaderFile] = ex;
                    ErrorLogging.LogLine(
                        $"Error loading dataloader.lua. This file is required to load any values from the prototypes. Message: '{ex.Message}'");
                    return;
                }

                lua.SetMods(orderedMods);

                lua["package.path"] = basePackagePath;
                foreach (Mod mod in orderedMods)
                    mod.Load(lua, "settings.lua");
                foreach (Mod mod in orderedMods)
                    mod.Load(lua, "settings-updates.lua");
                foreach (Mod mod in orderedMods)
                    mod.Load(lua, "settings-final-fixes.lua");

                settingsMap = ExtractSettings(lua);
            }

            using (var lua = CreateFactorioLua(Logger)) {
                string basePackagePath = (string)lua["package.path"];

                string dataloaderFile = Path.Combine(DataPath, "core", "lualib", "dataloader.lua");
                try {
                    lua.DoFile(dataloaderFile);
                } catch (Exception ex) {
                    FailedFiles[dataloaderFile] = ex;
                    ErrorLogging.LogLine(
                        $"Error loading dataloader.lua. This file is required to load any values from the prototypes. Message: '{ex.Message}'");
                    return;
                }

                lua.SetMods(orderedMods);

                lua.DoString(@"
                    --function module(modname,...)
                    --end

                    --require ""util""
                    --util = {}
                    --util.table = {}
                    --util.table.deepcopy = table.deepcopy
                    --util.multiplystripes = multiplystripes
                    --util.by_pixel = by_pixel
                    --util.format_number = format_number
                    --util.increment = increment

                    settings = {}
                    settings.startup = {}
                    setmetatable(settings.startup, {
                        __index = function()
                            return {value = 0}
                        end
                    })
");

                lua.NewTable("settings");
                foreach (var entry in settingsMap) {
                    var key = $"settings.{entry.Key}";
                    lua.NewTable(key);
                    using var settingsForType = lua.GetTable(key);
                    foreach (var subEntry in entry.Value) {
                        using var t = (LuaTable)lua.DoString("return {}")[0];
                        t["value"] = subEntry.Value;
                        settingsForType[subEntry.Key] = t;
                    }
                }

                LoadMods(lua, orderedMods, basePackagePath);

                //------------------------------------------------------------------------------------------
                // Lua files have all been executed, now it's time to extract their data from the lua engine
                //------------------------------------------------------------------------------------------

                InterpretRawData(lua.GetTable("data.raw"));
                LoadAllLanguages();
                await ChangeLocaleAsync(DefaultLocale);
            }

            MarkCyclicRecipes();

            ReportErrors();
        }

        private static Dictionary<string, Dictionary<string, object>> ExtractSettings(FactorioLua lua)
        {
            var map = new Dictionary<string, Dictionary<string, object>>();

            using var data = lua.GetTable("data.raw");
            foreach (LuaTable settingTable in data.Values) {
                foreach (LuaTable setting in settingTable.Values) {
                    string name = setting.String("name");
                    string type = setting.String("setting_type");
                    var value = setting["default_value"];
                    var s = map.GetOrAdd(type, _ => new Dictionary<string, object>());
                    s.Add(name, value);
                }
            }

            return map;
        }

        private void LoadMods(FactorioLua lua, List<Mod> mods, string basePackagePath)
        {
            foreach (string filename in new[] { "data.lua", "data-updates.lua", "data-final-fixes.lua" }) {
                foreach (var mod in mods) {
                    LoadMod(lua, mod, basePackagePath, filename);
                }
            }
        }

        private void LoadMod(FactorioLua lua, Mod mod, string basePackagePath, string filename)
        {
            // Mods use relative paths, but if more than one mod is in package.path at once this can be ambiguous
            lua["package.path"] = basePackagePath;
            try {
                lua.PushMod(mod);
            } catch (Exception ex) {
                FailedModRegistrations[mod.ModPath] = ex;
                return;
            }

            try {
                //Because many mods use the same path to refer to different files, we need to clear the 'loaded' table so Lua doesn't think they're already loaded
                lua.DoString(@"
                            for k, v in pairs(package.loaded) do
                                package.loaded[k] = false
                            end");

                try {
                    mod.Load(lua, filename);
                } catch (Exception ex) {
                    FailedFiles[$"__{mod.Name}__/{filename}"] = ex;
                }
            } finally {
                lua.PopMod(mod);
                lua["package.path"] = basePackagePath;
            }
        }

        private void InterpretRawData(LuaTable rawData)
        {
            var itemTypes = new List<string> {
                "item",
                "fluid",
                "capsule",
                "module",
                "ammo",
                "gun",
                "armor",
                "blueprint",
                "deconstruction-item",
                "mining-tool",
                "repair-tool",
                "tool",
                "item-with-entity-data",
                "rail-planner"
            };
            foreach (string key in itemTypes) {
                if (rawData[key] is LuaTable table) {
                    foreach (KeyValuePair<object, object> entry in table)
                        InterpretLuaItem((string)entry.Key, (LuaTable)entry.Value);
                }
            }

            var interpreters = new ValueTuple<string, Action<string, LuaTable>>[] {
                ("recipe", InterpretLuaRecipe),
                ("assembling-machine", InterpretAssemblingMachine),
                ("furnace", InterpretFurnace),
                ("rocket-silo", InterpretRocketSilo),
                ("mining-drill", InterpretMiner),
                ("resource", InterpretResource),
                ("module", InterpretModule),
                ("beacon", InterpretBeacon),
            };

            foreach (var (key, interpreter) in interpreters) {
                if (rawData[key] is LuaTable table) {
                    foreach (KeyValuePair<object, object> entry in table)
                        interpreter((string)entry.Key, (LuaTable)entry.Value);
                }
            }
        }

        private BitmapSource LoadUnknownIcon()
        {
            var assembly = typeof(DataCache).Assembly;
            using (var stream = assembly.GetManifestResourceStream(typeof(DataCache), "UnknownIcon.png")) {
                if (stream != null)
                    return ImagingExtensions.LoadImage(stream);
            }

            int length = 32;
            var pixels = new uint[length * length];
            for (int i = 0; i < pixels.Length; ++i)
                pixels[i] = 0xFFFFFFFF;
            var icon = BitmapSource.Create(
                length, length, 96, 96, PixelFormats.Pbgra32, null, pixels, 32);
            icon.Freeze();
            return icon;
        }

        private void LoadAllLanguages()
        {
            var localeDirs = Directory.EnumerateDirectories(
                Path.Combine(coreMod!.ModPath, "locale"));

            foreach (string dir in localeDirs) {
                var newLanguage = new Language(Path.GetFileName(dir));
                try {
                    string infoJson = File.ReadAllText(Path.Combine(dir, "info.json"));
                    newLanguage.LocalName = (string)JObject.Parse(infoJson)["language-name"]!;
                } catch {
                    // ignored
                }

                Languages.Add(newLanguage);
            }
        }

        public void Clear()
        {
            Mods.Clear();
            Items.Clear();
            Recipes.Clear();
            Assemblers.Clear();
            Miners.Clear();
            Resources.Clear();
            Modules.Clear();
            Beacons.Clear();
            colorCache.Clear();
            localeFiles.Clear();
            FailedFiles.Clear();
            FailedModRegistrations.Clear();
            Inserters.Clear();
            Languages.Clear();
        }

        private void ReportErrors()
        {
            if (FailedModRegistrations.Any()) {
                ErrorLogging.LogLine("There were errors setting the lua path variable or loader for the following mods:");
                foreach (string dir in FailedModRegistrations.Keys)
                    ErrorLogging.LogLine($"{dir} ({FailedModRegistrations[dir].Message})");
            }

            if (FailedFiles.Any()) {
                ErrorLogging.LogLine("The following files could not be loaded due to errors:");
                foreach (string file in FailedFiles.Keys)
                    ErrorLogging.LogLine($"{file} ({FailedFiles[file].Message})");
            }
        }

        private void AddLuaPackagePath(Lua lua, string dir)
        {
            try {
                string luaCommand =
                    $"package.path = package.path .. ';{dir}{Path.DirectorySeparatorChar}?.lua'";
                luaCommand = luaCommand.Replace("\\", "\\\\");
                lua.DoString(luaCommand);
            } catch (Exception ex) {
                FailedModRegistrations[dir] = ex;
            }
        }

        private void FindAllMods(List<string>? enabledMods) //Vanilla game counts as a mod too.
        {
            if (Directory.Exists(DataPath)) {
                foreach (string dir in Directory.EnumerateDirectories(DataPath))
                    ReadModInfoFile(dir);
            }
            if (Directory.Exists(ModPath)) {
                foreach (string dir in Directory.EnumerateDirectories(ModPath))
                    ReadModInfoFile(dir);
                foreach (string zipFile in Directory.EnumerateFiles(ModPath, "*.zip"))
                    ReadModInfoZip(zipFile);
            }

            coreMod = Mods.First(x => x.Name == "core");

            var enabledModsFromFile = new Dictionary<string, bool>();

            string modListFile = Path.Combine(Settings.Default.FactorioModPath, "mod-list.json");
            if (File.Exists(modListFile)) {
                string json = File.ReadAllText(modListFile);
                dynamic parsedJson = JsonConvert.DeserializeObject(json)!;
                foreach (var mod in parsedJson.mods) {
                    string name = mod.name;
                    bool enabled = (bool)mod.enabled;
                    enabledModsFromFile.Add(name, enabled);
                }
            }

            if (enabledMods != null) {
                foreach (Mod mod in Mods) {
                    mod.Enabled = enabledMods.Contains(mod.Name);
                }
            } else {
                var splitModStrings = new Dictionary<string, string>();
                foreach (string? s in Settings.Default.EnabledMods) {
                    var split = s!.Split('|');
                    splitModStrings.Add(split[0], split[1]);
                }
                foreach (Mod mod in Mods) {
                    if (splitModStrings.ContainsKey(mod.Name)) {
                        mod.Enabled = (splitModStrings[mod.Name] == "True");
                    } else if (enabledModsFromFile.ContainsKey(mod.Name)) {
                        mod.Enabled = enabledModsFromFile[mod.Name];
                    } else {
                        mod.Enabled = true;
                    }
                }
            }

            var modGraph = new DependencyGraph(Mods);
            modGraph.DisableUnsatisfiedMods();
            Mods = modGraph.SortMods();
        }

        private void ReadModInfoFile(string dir)
        {
            var path = Path.Combine(dir, "info.json");
            if (!File.Exists(path))
                return;
            try {
                ReadModInfo(File.ReadAllText(path), dir);
            } catch (Exception) {
                ErrorLogging.LogLine($"The mod at '{dir}' has an invalid info.json file");
            }
        }

        private static string GetTempModPath(string modZipFile)
        {
            var name = Path.GetFileNameWithoutExtension(modZipFile);
            return Path.Combine(Path.GetTempPath(), "ForemanMods", name);
        }

        private void UnzipMod(string modZipFile)
        {
            string fullPath = Path.GetFullPath(modZipFile);
            byte[] hash;
            bool needsExtraction = false;

            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(fullPath))
                hash = md5.ComputeHash(stream);

            if (ZipHashes.ContainsKey(fullPath)) {
                if (!ZipHashes[fullPath].SequenceEqual(hash)) {
                    needsExtraction = true;
                    ZipHashes[fullPath] = hash;
                }
            } else {
                needsExtraction = true;
                ZipHashes.Add(fullPath, hash);
            }

            string outputDir = GetTempModPath(modZipFile);

            if (needsExtraction) {
                try {
                    Directory.Delete(outputDir, true);
                } catch (DirectoryNotFoundException) {
                }
                ZipFile.ExtractToDirectory(modZipFile, outputDir);
            }
        }

        private void ReadModInfoZip(string zipFile)
        {
            using var archive = new ZipArchive(File.OpenRead(zipFile), ZipArchiveMode.Read);
            var infoEntry = archive.GetEntry("info.json") ?? GetEntryIgnoreCaseSlow(archive, "info.json");
            if (infoEntry != null)
                ReadModInfo(infoEntry.ReadAllText(), zipFile);
        }

        private ZipArchiveEntry? GetEntryIgnoreCaseSlow(ZipArchive archive, string name)
        {
            foreach (var entry in archive.Entries) {
                if (entry.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return entry;
            }

            return null;
        }

        private void ReadModInfo(string json, string path)
        {
            var obj = JObject.Parse(json);

            string? name = obj.Value<string>("name");
            string? version = obj.Value<string>("version");

            if (version == null || !Version.TryParse(version, out var parsedVersion))
                parsedVersion = new Version(0, 0, 0, 0);

            var newMod = new Mod(path, File.Exists(path)) {
                Name = name ?? Path.GetFileName(path),
                Version = version ?? "",
                ParsedVersion = parsedVersion,
                Title = obj.Value<string>("title"),
                Author = obj.Value<string>("author")
            };

            JToken? deps = obj["dependencies"];
            if (deps != null) {
                foreach (string? dep in deps.Values<string>().NotNull())
                    newMod.Dependencies.Add(dep);
            }

            ParseModDependencies(newMod);

            var existing = Mods.FindIndex(x => x.Name == newMod.Name);
            if (existing != -1) {
                if (parsedVersion < Mods[existing].ParsedVersion)
                    return;

                Mods[existing] = newMod;
            } else {
                Mods.Add(newMod);
            }
        }

        private void ParseModDependencies(Mod mod)
        {
            if (mod.Name == "base")
                mod.Dependencies.Add("core");

            foreach (string depString in mod.Dependencies) {
                var match = Regex.Match(depString,
                    @"\A(?:(?<prefix>\?|\(\?\)|~|!)\ *)?
                        (?<name>\S.+?)
                        (?:\ +(?<op><|<=|=|>=|>)\ (?<version>\d+(?:\.\d+)*))?\z",
                    RegexOptions.IgnorePatternWhitespace);

                if (!match.Success)
                    continue;

                Version? version = null;
                DependencyType versionType = DependencyType.EqualTo;

                if (match.Groups["version"].Success) {
                    if (!Version.TryParse(match.Groups["version"].Value, out version))
                        version = new Version(0, 0, 0, 0);

                    versionType = match.Groups["op"]?.Value switch {
                        "=" => DependencyType.EqualTo,
                        ">" => DependencyType.GreaterThan,
                        ">=" => DependencyType.GreaterThanOrEqual,
                        "<" => DependencyType.LessThan,
                        "<=" => DependencyType.LessThanOrEqual,
                        _ => DependencyType.EqualTo
                    };
                }

                ModDependencyKind kind = ModDependencyKind.Required;

                if (match.Groups["prefix"].Success) {
                    kind = match.Groups["prefix"].Value switch {
                        "?" => ModDependencyKind.Optional,
                        "(?)" => ModDependencyKind.HiddenOptional,
                        "~" => ModDependencyKind.DoesNotAffectLoaderOrder,
                        "!" => ModDependencyKind.Incompatible,
                        _ => kind
                    };
                }

                var modName = match.Groups["name"].Value;
                var dep = new ModDependency(modName, kind, version, versionType);
                mod.ParsedDependencies.Add(dep);
            }
        }

        public async Task ChangeLocaleAsync(string newLocale)
        {
            localeFiles = await LoadLocaleFilesAsync(newLocale, Mods, FailedFiles);
        }

        private static async Task<LocalizedStringDictionary> LoadLocaleFilesAsync(
            string locale, IEnumerable<Mod> mods, Dictionary<string, Exception>? failedFiles = null)
        {
            var localeFiles = new LocalizedStringDictionary();

            foreach (Mod mod in mods.Where(m => m.Enabled)) {
                var localeDir = Path.Combine("locale", locale);
                foreach (var file in mod.EnumerateFiles(localeDir, "*.cfg")) {
                    await using var stream = file.Open();
                    try {
                        await LoadLocaleFileAsync(stream, localeFiles);
                    } catch (Exception ex) when (failedFiles != null) {
                        failedFiles[file.Name] = ex;
                    }
                }
            }

            return localeFiles;
        }

        private static async Task LoadLocaleFileAsync(
            Stream file, LocalizedStringDictionary newLocaleFiles)
        {
            using var stream = new StreamReader(file);
            string iniSection = "none";

            while (!stream.EndOfStream) {
                string? line = await stream.ReadLineAsync();
                if (line == null)
                    break;

                if (line.StartsWith("[") && line.EndsWith("]")) {
                    iniSection = line.Trim('[', ']');
                    continue;
                }

                string[] split = line.Split('=');
                if (split.Length == 2)
                    newLocaleFiles[iniSection, split[0].Trim()] = split[1].Trim();
            }
        }

        private BitmapSource? LoadModImage(LuaTable values)
        {
            {
                var iconSize = values.Int("icon_size");
                var iconPath = values.StringOrDefault("icon");
                if (iconPath != null)
                    return LoadModImage(iconPath, iconSize);
            }

            var icons = values.TableOrDefault("icons");
            if (icons != null) {
                var baseImagePath = ((LuaTable)icons[1]).String("icon");
                var baseImage = LoadModImage(baseImagePath);
                // FIXME: Handle composite icons
                return baseImage;
            }

            return null;
        }

        private BitmapSource? LoadModImage(string filePath, int? iconSize = null)
        {
            if (TrySplitModPath(filePath, out string? modName, out string? relativePath)) {
                var mod = Mods.FirstOrDefault(x => x.Name == modName);
                return mod?.LoadImage(relativePath, iconSize);
            }

            if (!File.Exists(filePath)) {
                filePath = Path.Combine(Application.StartupPath, filePath);
                if (!File.Exists(filePath))
                    return null;
            }

            try {
                return ImagingExtensions.LoadImage(filePath, iconSize);
            } catch (Exception) {
                return null;
            }
        }

        private static bool TrySplitModPath(
            string filePath, [MaybeNullWhen(false)] out string modName,
            [MaybeNullWhen(false)] out string relativePath)
        {
            modName = null;
            relativePath = null;

            // "__a__/"
            if (filePath.Length < 2 + 1 + 2 + 1)
                return false;

            int idx = filePath.IndexOf('/');
            if (idx == -1 || idx < 5)
                return false;

            if (filePath[0] != '_' || filePath[1] != '_' ||
                filePath[idx - 1] != '_' || filePath[idx - 2] != '_')
                return false;

            modName = filePath.Substring(2, idx - 4);
            relativePath = filePath.Substring(idx + 1);
            return true;
        }

        public static Color IconAverageColor(BitmapSource? icon)
        {
            if (icon == null)
                return Colors.LightGray;

            if (colorCache.TryGetValue(icon, out Color result))
                return result;

            result = icon.ComputeAvgColor();

            // Set alpha to 255, also lighten the colours to make them more pastel-y
            result = Color.FromArgb(
                255,
                (byte)(result.R + (255 - result.R) / 2),
                (byte)(result.G + (255 - result.G) / 2),
                (byte)(result.B + (255 - result.B) / 2));
            colorCache.Add(icon, result);

            return result;
        }

        private void InterpretLuaItem(string? name, LuaTable values)
        {
            if (string.IsNullOrEmpty(name) || Items.ContainsKey(name))
                return;

            var newItem = new Item(name);
            newItem.LocalizedName = GetLocalizationInfo(values);
            newItem.Icon = LoadModImage(values);

            Items.Add(name, newItem);
        }

        private LocalizationInfo? GetLocalizationInfo(LuaTable values)
        {
            var localizedTable = values.TableOrDefault("localised_name");
            if (localizedTable == null || localizedTable.Values.Count != 2)
                return null;

            var name = localizedTable[1] as string;
            var placeholders = localizedTable[2] as LuaTable;
            if (name == null || placeholders == null)
                return null;

            if (!SplitKey(name, out var section, out name))
                return null;

            if (placeholders.Values.Count == 1) {
                if (!SplitKey((string)placeholders[1], out var placeholderSection, out var placeholderName))
                    return null;
                return LocalizationInfo.Create(section, name, placeholderSection, placeholderName);
            }

            var e = new List<string>();
            foreach (string placeholder in placeholders.Values) {
                if (!SplitKey(placeholder, out var placeholderSection, out var placeholderName))
                    return null;

                e.Add(placeholderSection);
                e.Add(placeholderName);
            }

            return LocalizationInfo.Create(section, name, e);
        }

        private bool SplitKey(string key, [MaybeNullWhen(false)] out string section,
            [MaybeNullWhen(false)] out string name)
        {
            int idx = key.IndexOf('.');
            if (idx != -1) {
                section = key.Substring(0, idx);
                name = key.Substring(idx + 1);
                return true;
            }

            section = null;
            name = null;
            return false;
        }

        private Item FindOrCreateUnknownItem(string itemName)
        {
            // This is only if a recipe references an item that isn't in the
            // item prototypes (which shouldn't really happen)
            return Items.GetOrAdd(itemName, x => new Item(x));
        }

        private void InterpretLuaRecipe(string name, LuaTable values)
        {
            try {
                var timeSource = values[Difficulty] == null ? values : values.TableOrDefault(Difficulty);
                if (timeSource == null) {
                    ErrorLogging.LogLine($"Error reading recipe '{name}', unable to locate data table.");
                    return;
                }

                float time = timeSource.FloatOrDefault("energy_required", 0.5f);
                if (time == 0.0f)
                    time = DefaultRecipeTime;

                Dictionary<Item, float> ingredients = ExtractIngredientsFromLuaRecipe(values);
                Dictionary<Item, float> results = ExtractResultsFromLuaRecipe(values);

                var newRecipe = new Recipe(
                    name, time, ingredients, results) {
                    Category = values.StringOrDefault("category", "crafting"),
                    Icon = LoadModImage(values)
                };
                newRecipe.LocalizedName = GetLocalizationInfo(values);

                foreach (Item result in results.Keys)
                    result.Recipes.Add(newRecipe);

                Recipes.Add(newRecipe.Name, newRecipe);
            } catch (MissingPrototypeValueException ex) {
                ErrorLogging.LogLine(
                    $"Error reading value '{ex.Key}' from recipe prototype '{name}'. " +
                    $"Returned error message: '{ex.Message}'");
            }
        }

        private void ReadAssemblerProperties(Assembler assembler, LuaTable values)
        {
            assembler.Icon = LoadModImage(values);
            assembler.MaxIngredients = values.IntOrDefault("ingredient_count", int.MaxValue);
            assembler.ModuleSlots = values.IntOrDefault("module_slots");
            if (assembler.ModuleSlots == 0) {
                var moduleTable = values.TableOrDefault("module_specification");
                if (moduleTable != null)
                    assembler.ModuleSlots = moduleTable.IntOrDefault("module_slots");
            }

            assembler.Speed = values.Float("crafting_speed");
            assembler.EnergyUsage = ParsePower(values.String("energy_usage"));

            LuaTable categories = values.Table("crafting_categories");
            foreach (string category in categories.Values) {
                assembler.Categories.Add(category);
            }

            LuaTable? effects = values.TableOrDefault("allowed_effects");
            if (effects != null) {
                foreach (string effect in effects.Values) {
                    assembler.AllowedEffects.Add(effect);
                }
            }

            foreach (string? s in Settings.Default.EnabledAssemblers) {
                if (s!.Split('|')[0] == assembler.Name) {
                    assembler.Enabled = (s.Split('|')[1] == "True");
                }
            }
        }

        private void InterpretAssemblingMachine(string name, LuaTable values)
        {
            try {
                var newAssembler = new Assembler(name);
                ReadAssemblerProperties(newAssembler, values);

                Assemblers.Add(newAssembler.Name, newAssembler);
            } catch (MissingPrototypeValueException ex) {
                ErrorLogging.LogLine(
                    $"Error reading value '{ex.Key}' from assembler prototype '{name}'. " +
                    $"Returned error message: '{ex.Message}'");
            }
        }

        private static Power ParsePower(string value)
        {
            if (value.EndsWith("GW", StringComparison.OrdinalIgnoreCase))
                return Power.FromGigawatts(double.Parse(value.Substring(0, value.Length - 2)));
            if (value.EndsWith("MW", StringComparison.OrdinalIgnoreCase))
                return Power.FromMegawatts(double.Parse(value.Substring(0, value.Length - 2)));
            if (value.EndsWith("kW", StringComparison.OrdinalIgnoreCase))
                return Power.FromKilowatts(double.Parse(value.Substring(0, value.Length - 2)));
            if (value.EndsWith("W", StringComparison.OrdinalIgnoreCase))
                return new Power(double.Parse(value.Substring(0, value.Length - 1)));

            throw new ArgumentException($"Invalid power value '{value}'");
        }

        private void InterpretFurnace(string name, LuaTable values)
        {
            try {
                var newFurnace = new Assembler(name);
                ReadAssemblerProperties(newFurnace, values);
                newFurnace.MaxIngredients = 1;

                if (newFurnace.Speed == -1f) {
                    //In case we're still on Factorio 0.10
                    newFurnace.Speed = values.Float("smelting_speed");
                }

                if (values["crafting_categories"] == null) {
                    //Another 0.10 compatibility thing.
                    LuaTable? categories = values.TableOrDefault("smelting_categories");
                    if (categories != null) {
                        foreach (string category in categories.Values)
                            newFurnace.Categories.Add(category);
                    }
                }

                Assemblers.Add(newFurnace.Name, newFurnace);
            } catch (MissingPrototypeValueException ex) {
                ErrorLogging.LogLine(
                    $"Error reading value '{ex.Key}' from furnace prototype '{name}'. " +
                    $"Returned error message: '{ex.Message}'");
            }
        }

        private void InterpretRocketSilo(string name, LuaTable values)
        {
            try {
                var newRocketSilo = new Assembler(name);
                ReadAssemblerProperties(newRocketSilo, values);

                Assemblers.Add(newRocketSilo.Name, newRocketSilo);
            } catch (MissingPrototypeValueException ex) {
                ErrorLogging.LogLine(
                    $"Error reading value '{ex.Key}' from rocket silo prototype '{name}'. " +
                    $"Returned error message: '{ex.Message}'");
            }
        }

        private void InterpretMiner(string name, LuaTable values)
        {
            try {
                var newMiner = new Miner(name);

                newMiner.Icon = LoadModImage(values);
                newMiner.MiningPower = 1; //values.Float("mining_power");
                newMiner.Speed = values.Float("mining_speed");
                newMiner.ModuleSlots = values.IntOrDefault("module_slots");
                newMiner.EnergyUsage = ParsePower(values.String("energy_usage"));
                if (newMiner.ModuleSlots == 0) {
                    var moduleTable = values.TableOrDefault("module_specification");
                    if (moduleTable != null)
                        newMiner.ModuleSlots = moduleTable.IntOrDefault("module_slots");
                }

                LuaTable? categories = values.TableOrDefault("resource_categories");
                if (categories != null) {
                    foreach (string category in categories.Values) {
                        newMiner.ResourceCategories.Add(category);
                    }
                }

                foreach (string? s in Settings.Default.EnabledMiners) {
                    if (s!.Split('|')[0] == name) {
                        newMiner.Enabled = (s.Split('|')[1] == "True");
                    }
                }

                Miners.Add(name, newMiner);
            } catch (MissingPrototypeValueException ex) {
                ErrorLogging.LogLine(
                    $"Error reading value '{ex.Key}' from miner prototype '{name}'. " +
                    $"Returned error message: '{ex.Message}'");
            }
        }

        private void InterpretResource(string name, LuaTable values)
        {
            try {
                if (values["minable"] == null) {
                    return; //This means the resource is not usable by miners and is therefore not useful to us
                }

                var category = values.StringOrDefault("category", "basic-solid");
                LuaTable minableTable = values.Table("minable");
                var hardness = 0; //minableTable.Float("hardness");
                var miningTime = minableTable.Float("mining_time");

                string result;
                if (minableTable["result"] != null) {
                    result = minableTable.String("result");
                } else {
                    var results = minableTable["results"] as LuaTable;
                    result = (results?[1] as LuaTable)?["name"] as string ??
                             throw new MissingPrototypeValueException(minableTable, "results");
                }

                var resultItem = FindOrCreateUnknownItem(result);

                var newResource = new Resource(
                    name, category, hardness, miningTime, resultItem);
                Resources.Add(name, newResource);
            } catch (MissingPrototypeValueException ex) {
                ErrorLogging.LogLine(
                    $"Error reading value '{ex.Key}' from resource prototype '{name}'. " +
                    $"Returned error message: '{ex.Message}'");
            }
        }

        private void InterpretModule(string name, LuaTable values)
        {
            try {
                string category = values.String("category");

                float speedBonus = 0f;
                float productivityBonus = 0f;
                float consumptionBonus = 0f;

                LuaTable effectTable = values.Table("effect");

                LuaTable? speed = effectTable.TableOrDefault("speed");
                if (speed != null) {
                    speedBonus = speed.FloatOrDefault("bonus");
                }

                LuaTable? productivity = effectTable.TableOrDefault("productivity");
                if (productivity != null) {
                    productivityBonus = productivity.FloatOrDefault("bonus");
                }

                LuaTable? consumption = effectTable.TableOrDefault("consumption");
                if (consumption != null)
                    consumptionBonus = consumption.FloatOrDefault("bonus");

                var limitations = values.TableOrDefault("limitation");
                List<string>? allowedIn = null;
                if (limitations != null) {
                    allowedIn = limitations.Values.Cast<string>().ToList();
                }

                var newModule = new Module(
                    name, category, speedBonus, productivityBonus,
                    consumptionBonus, allowedIn);

                foreach (string? s in Settings.Default.EnabledModules) {
                    if (s!.Split('|')[0] == name) {
                        newModule.Enabled = (s.Split('|')[1] == "True");
                    }
                }

                Modules.Add(name, newModule);
            } catch (MissingPrototypeValueException ex) {
                ErrorLogging.LogLine(
                    $"Error reading value '{ex.Key}' from module prototype '{name}'. " +
                    $"Returned error message: '{ex.Message}'");
            }
        }

        private void InterpretBeacon(string name, LuaTable values)
        {
            try {
                IEnumerable<string> allowedEffects = Enumerable.Empty<string>();
                if (values["allowed_effects"] is LuaTable effects)
                    allowedEffects = effects.Values.Cast<string>();

                var effectivity = values.FloatOrDefault("distribution_effectivity", 1);

                int moduleSlots;
                if (values["module_specification"] is LuaTable t)
                    moduleSlots = t.IntOrDefault("module_slots");
                else
                    moduleSlots = values.IntOrDefault("module_slots");

                var beacon = new Beacon(
                    name, allowedEffects, effectivity, moduleSlots);
                Beacons.Add(name, beacon);
            } catch (MissingPrototypeValueException ex) {
                ErrorLogging.LogLine(
                    $"Error reading value '{ex.Key}' from module prototype '{name}'. " +
                    $"Returned error message: '{ex.Message}'");
            }
        }

        private void InterpretInserter(string name, LuaTable values)
        {
            try {
                var newInserter = new Inserter(name);
                newInserter.RotationSpeed = values.Float("rotation_speed");
                newInserter.Icon = LoadModImage(values);

                Inserters.Add(name, newInserter);
            } catch (MissingPrototypeValueException ex) {
                ErrorLogging.LogLine(
                    $"Error reading value '{ex.Key}' from inserter prototype '{name}'. " +
                    $"Returned error message: '{ex.Message}'");
            }
        }

        private Dictionary<Item, float> ExtractResultsFromLuaRecipe(LuaTable values)
        {
            var results = new Dictionary<Item, float>();

            LuaTable? source = null;

            if (values[Difficulty] == null)
                source = values;
            else {
                var difficultyTable = values.TableOrDefault(Difficulty);
                if (difficultyTable?["result"] != null || difficultyTable?["results"] != null)
                    source = difficultyTable;
            }

            if (source?["result"] != null) {
                string resultName = source.String("result");
                float resultCount = source.FloatOrDefault("result_count");
                if (resultCount == 0f) {
                    resultCount = 1f;
                }
                results.Add(FindOrCreateUnknownItem(resultName), resultCount);
            } else {
                // If we can't read results, try difficulty/results
                LuaTable? resultsTable = source?.TableOrDefault("results");

                if (resultsTable != null) {
                    foreach (LuaTable resultTable in resultsTable.Values) {
                        Item result = FindOrCreateUnknownItem(
                            (string?)resultTable["name"] ?? (string)resultTable[1]);

                        float amount;
                        if (resultTable["amount"] != null) {
                            amount = resultTable.Float("amount");
                            //Just the average yield. Maybe in the future it should show more information about the probability
                            amount *= resultTable.FloatOrDefault("probability", 1);
                        } else if (resultTable["amount_min"] != null) {
                            float probability = resultTable.FloatOrDefault("probability", 1);
                            float amountMin = resultTable.Float("amount_min");
                            float amountMax = resultTable.Float("amount_max");
                            //Just the average yield. Maybe in the future it should show more information about the probability
                            amount = ((amountMin + amountMax) / 2f) * probability;
                        } else {
                            amount = Convert.ToSingle(resultTable[2]);
                        }

                        if (results.ContainsKey(result)) {
                            results[result] += amount;
                        } else {
                            results.Add(result, amount);
                        }
                    }
                } else {
                    ErrorLogging.LogLine($"Error reading results from {values}.");
                }
            }
            return results;
        }

        private Dictionary<Item, float> ExtractIngredientsFromLuaRecipe(LuaTable values)
        {
            var ingredients = new Dictionary<Item, float>();

            LuaTable ingredientsTable =
                values.TableOrDefault("ingredients") ??
                values.Table(Difficulty).Table("ingredients");

            foreach (LuaTable ingredientTable in ingredientsTable.Values) {
                // Name and amount often have no key in the prototype
                string name = (string)(ingredientTable["name"] ?? ingredientTable[1]);
                float amount = Convert.ToSingle(ingredientTable["amount"] ?? ingredientTable[2]);

                Item ingredient = FindOrCreateUnknownItem(name);
                if (!ingredients.ContainsKey(ingredient))
                    ingredients.Add(ingredient, amount);
                else
                    ingredients[ingredient] += amount;
            }

            return ingredients;
        }

        private void MarkCyclicRecipes()
        {
            var testGraph = new ProductionGraph();
            foreach (Recipe recipe in Recipes.Values)
                RecipeNode.Create(recipe, testGraph);

            testGraph.CreateAllPossibleInputLinks();
            foreach (var scc in testGraph.GetStronglyConnectedComponents(true)) {
                foreach (var node in scc) {
                    ((RecipeNode)node).BaseRecipe.IsCyclic = true;
                }
            }
        }

        public static readonly List<string> LocaleCategories =
            new() { "item-name", "fluid-name", "entity-name", "equipment-name" };

        public string GetLocalizedString(string category, string name)
        {
            if (localeFiles.TryGetValue(category, name, out var localized))
                return localized;
            return name;
        }

        public bool TryGetLocalizedString(
            string category, string name, [MaybeNullWhen(false)] out string localized)
        {
            if (localeFiles.TryGetValue(category, name, out localized))
                return true;
            localized = null;
            return false;
        }

        public string GetLocalizedString(string name)
        {
            foreach (string category in LocaleCategories) {
                if (localeFiles.TryGetValue(category, name, out var localized))
                    return localized;
            }

            return name;
        }

        public string GetLocalizedString(string name, LocalizationInfo? locInfo)
        {
            return locInfo?.Interpolate(localeFiles) ?? GetLocalizedString(name);
        }
    }

    public class MissingPrototypeValueException : Exception
    {
        public LuaTable Table { get; }
        public string Key { get; }

        public MissingPrototypeValueException(LuaTable table, string key, string message = "")
            : base(message)
        {
            Table = table;
            Key = key;
        }
    }

    public static class LuaExtensions
    {
        public static float Float(this LuaTable table, string key)
        {
            if (table[key] == null)
                throw new MissingPrototypeValueException(table, key, "Key is missing");

            try {
                return Convert.ToSingle(table[key]);
            } catch (FormatException) {
                throw new MissingPrototypeValueException(table, key,
                    $"Expected a float, but the value ('{table[key]}') isn't one");
            }
        }

        public static float FloatOrDefault(this LuaTable table, string key, float defaultValue = 0f)
        {
            if (table[key] == null)
                return defaultValue;

            try {
                return Convert.ToSingle(table[key]);
            } catch (FormatException) {
                throw new MissingPrototypeValueException(table, key,
                    $"Expected a float, but the value ('{table[key]}') isn't one");
            }
        }

        public static int IntOrDefault(this LuaTable table, string key, int defaultValue = 0)
        {
            if (table[key] == null)
                return defaultValue;

            try {
                return Convert.ToInt32(table[key]);
            } catch (FormatException) {
                throw new MissingPrototypeValueException(table, key,
                    $"Expected an Int32, but the value ('{table[key]}') isn't one");
            }
        }

        public static int? Int(this LuaTable table, string key)
        {
            if (table[key] == null)
                return null;

            try {
                return Convert.ToInt32(table[key]);
            } catch (FormatException) {
                throw new MissingPrototypeValueException(table, key,
                    $"Expected an Int32, but the value ('{table[key]}') isn't one");
            }
        }

        public static string String(this LuaTable table, string key)
        {
            if (table[key] != null)
                return Convert.ToString(table[key])!;

            throw new MissingPrototypeValueException(table, key, "Key is missing");
        }

        [return: NotNullIfNotNull("defaultValue")]
        public static string? StringOrDefault(
            this LuaTable table, string key, string? defaultValue = null)
        {
            if (table[key] != null)
                return Convert.ToString(table[key]);
            return defaultValue;
        }

        public static LuaTable Table(this LuaTable table, string key)
        {
            if (table[key] != null)
                return (LuaTable)table[key];

            throw new MissingPrototypeValueException(table, key, "Key is missing");
        }

        public static LuaTable? TableOrDefault(
            this LuaTable table, string key, LuaTable? defaultValue = null)
        {
            if (table[key] != null)
                return table[key] as LuaTable;
            return defaultValue;
        }

        public static IEnumerable<KeyValuePair<object, object>> AsEnumerable(this LuaTable table)
        {
            foreach (KeyValuePair<object, object> entry in table)
                yield return entry;
        }
    }
}
