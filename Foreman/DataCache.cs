﻿namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Extensions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NLua;
    using Properties;
    using Units;

    public class Language
    {
        public Language(string name)
        {
            Name = name;
        }

        public string Name { get; }
        private string localName;

        public string LocalName
        {
            get => !string.IsNullOrWhiteSpace(localName) ? localName : Name;
            set => localName = value;
        }
    }

    public class LocalizedStringDictionary
    {
        private readonly Dictionary<(string, string), string> dictionary =
            new Dictionary<(string, string), string>();

        public void Clear()
        {
            dictionary.Clear();
        }

        public string this[string section, string key]
        {
            get => dictionary.GetValueOrDefault((section, key));
            set => dictionary[(section, key)] = value;
        }

        public bool TryGetValue(string section, string key, out string value)
        {
            return dictionary.TryGetValue((section, key), out value);
        }
    }

    public abstract class LocalizationInfo
    {
        public abstract string Interpolate(LocalizedStringDictionary localized);

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

            public override string Interpolate(LocalizedStringDictionary localized)
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

            public override string Interpolate(LocalizedStringDictionary localized)
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

    public class DataCache
    {
        private static DataCache current = new DataCache();

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

        private Mod coreMod;
        public List<Mod> Mods { get; set; } = new List<Mod>();
        public List<Language> Languages { get; } = new List<Language>();

        public string Difficulty { get; set; } = "normal";
        private const string DefaultLocale = "en";

        public Dictionary<string, Item> Items { get; } = new Dictionary<string, Item>();
        public Dictionary<string, Recipe> Recipes { get; } = new Dictionary<string, Recipe>();
        public Dictionary<string, Assembler> Assemblers { get; } = new Dictionary<string, Assembler>();
        public Dictionary<string, Miner> Miners { get; } = new Dictionary<string, Miner>();
        public Dictionary<string, Resource> Resources { get; } = new Dictionary<string, Resource>();
        public Dictionary<string, Module> Modules { get; } = new Dictionary<string, Module>();
        public Dictionary<string, Inserter> Inserters { get; } = new Dictionary<string, Inserter>();

        private const float defaultRecipeTime = 0.5f;
        private static readonly Dictionary<BitmapSource, Color> colorCache =
            new Dictionary<BitmapSource, Color>();
        public BitmapSource UnknownIcon;

        private LocalizedStringDictionary localeFiles =
            new LocalizedStringDictionary();

        public Dictionary<string, Exception> FailedFiles { get; } = new Dictionary<string, Exception>();
        public Dictionary<string, Exception> FailedPathDirectories { get; } = new Dictionary<string, Exception>();

        public Dictionary<string, byte[]> ZipHashes { get; } = new Dictionary<string, byte[]>();

        public static Task Reload()
        {
            return Reload(null);
        }

        public static async Task Reload(List<string> enabledMods)
        {
            var newData = new DataCache();
            newData.Difficulty = Current.Difficulty;
            await newData.LoadAllData(enabledMods);
            Current = newData;
        }

        private async Task LoadAllData(List<string> enabledMods)
        {
            Clear();

            using (var lua = new Lua()) {
                FindAllMods(enabledMods);

                AddLuaPackagePath(lua, Path.Combine(DataPath, "core", "lualib")); //Core lua functions
                string basePackagePath = lua["package.path"] as string;

                string dataloaderFile = Path.Combine(DataPath, "core", "lualib", "dataloader.lua");
                try {
                    lua.DoFile(dataloaderFile);
                } catch (Exception ex) {
                    FailedFiles[dataloaderFile] = ex;
                    ErrorLogging.LogLine(
                        $"Error loading dataloader.lua. This file is required to load any values from the prototypes. Message: '{ex.Message}'");
                    return;
                }

                lua.DoString(@"
                    function module(modname,...)
                    end

                    require ""util""
                    util = {}
                    util.table = {}
                    util.table.deepcopy = table.deepcopy
                    util.multiplystripes = multiplystripes
                    util.by_pixel = by_pixel
                    util.format_number = format_number
                    util.increment = increment

                    function log(...)
                    end

                    defines = {}
                    defines.difficulty_settings = {}
                    defines.difficulty_settings.recipe_difficulty = {}
                    defines.difficulty_settings.technology_difficulty = {}
                    defines.difficulty_settings.recipe_difficulty.normal = 1
                    defines.difficulty_settings.technology_difficulty.normal = 1
                    defines.direction = {}
                    defines.direction.north = 1
                    defines.direction.east = 2
                    defines.direction.south = 3
                    defines.direction.west = 4
");

                foreach (Mod mod in Mods.Where(m => m.Enabled)) {
                    foreach (string filename in new[] { "data.lua", "data-updates.lua", "data-final-fixes.lua" }) {
                        //Mods use relative paths, but if more than one mod is in package.path at once this can be ambiguous
                        lua["package.path"] = basePackagePath;
                        AddLuaPackagePath(lua, mod.Dir);

                        //Because many mods use the same path to refer to different files, we need to clear the 'loaded' table so Lua doesn't think they're already loaded
                        lua.DoString(@"
                            for k, v in pairs(package.loaded) do
                                package.loaded[k] = false
                            end");

                        string dataFile = Path.Combine(mod.Dir, filename);
                        if (File.Exists(dataFile)) {
                            try {
                                lua.DoFile(dataFile);
                            } catch (Exception ex) {
                                FailedFiles[dataFile] = ex;
                            }
                        }
                    }
                }

                //------------------------------------------------------------------------------------------
                // Lua files have all been executed, now it's time to extract their data from the lua engine
                //------------------------------------------------------------------------------------------

                foreach (string type in new List<string> {
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
                    "tool"
                }) {
                    InterpretItems(lua, type);
                }

                var rawData = lua.GetTable("data.raw");

                if (rawData["recipe"] is LuaTable recipeTable) {
                    foreach (KeyValuePair<object, object> entry in recipeTable)
                        InterpretLuaRecipe(entry.Key as string, entry.Value as LuaTable);
                }

                if (rawData["assembling-machine"] is LuaTable assemblerTable) {
                    foreach (KeyValuePair<object, object> entry in assemblerTable)
                        InterpretAssemblingMachine(entry.Key as string, entry.Value as LuaTable);
                }

                if (rawData["furnace"] is LuaTable furnaceTable) {
                    foreach (KeyValuePair<object, object> entry in furnaceTable)
                        InterpretFurnace(entry.Key as string, entry.Value as LuaTable);
                }

                if (rawData["rocket-silo"] is LuaTable rocketSiloTable) {
                    foreach (KeyValuePair<object, object> entry in rocketSiloTable)
                        InterpretRocketSilo(entry.Key as string, entry.Value as LuaTable);
                }

                if (rawData["mining-drill"] is LuaTable minerTable) {
                    foreach (KeyValuePair<object, object> entry in minerTable)
                        InterpretMiner(entry.Key as string, entry.Value as LuaTable);
                }

                if (rawData["resource"] is LuaTable resourceTable) {
                    foreach (KeyValuePair<object, object> entry in resourceTable)
                        InterpretResource(entry.Key as string, entry.Value as LuaTable);
                }

                if (rawData["module"] is LuaTable moduleTable) {
                    foreach (KeyValuePair<object, object> entry in moduleTable)
                        InterpretModule(entry.Key as string, entry.Value as LuaTable);
                }

                UnknownIcon = LoadUnknownIcon();

                LoadAllLanguages();
                await ChangeLocaleAsync(DefaultLocale);
            }

            MarkCyclicRecipes();

            ReportErrors();
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
                Path.Combine(coreMod.Dir, "locale"));

            foreach (string dir in localeDirs) {
                var newLanguage = new Language(Path.GetFileName(dir));
                try {
                    string infoJson = File.ReadAllText(Path.Combine(dir, "info.json"));
                    newLanguage.LocalName = (string)JObject.Parse(infoJson)["language-name"];
                } catch {
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
            colorCache.Clear();
            localeFiles.Clear();
            FailedFiles.Clear();
            FailedPathDirectories.Clear();
            Inserters.Clear();
            Languages.Clear();
        }

        private void ReportErrors()
        {
            if (FailedPathDirectories.Any()) {
                ErrorLogging.LogLine("There were errors setting the lua path variable for the following directories:");
                foreach (string dir in FailedPathDirectories.Keys)
                    ErrorLogging.LogLine($"{dir} ({FailedPathDirectories[dir].Message})");
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
                string luaCommand = string.Format("package.path = package.path .. ';{0}{1}?.lua'", dir,
                    Path.DirectorySeparatorChar);
                luaCommand = luaCommand.Replace("\\", "\\\\");
                lua.DoString(luaCommand);
            } catch (Exception ex) {
                FailedPathDirectories[dir] = ex;
            }
        }

        private void FindAllMods(List<string> enabledMods) //Vanilla game counts as a mod too.
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

            coreMod = Mods.FirstOrDefault(x => x.Name == "core");

            var enabledModsFromFile = new Dictionary<string, bool>();

            string modListFile = Path.Combine(Settings.Default.FactorioModPath, "mod-list.json");
            if (File.Exists(modListFile)) {
                string json = File.ReadAllText(modListFile);
                dynamic parsedJson = JsonConvert.DeserializeObject(json);
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
                foreach (string s in Settings.Default.EnabledMods) {
                    var split = s.Split('|');
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

            string outputDir = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(modZipFile));

            if (needsExtraction) {
                using (ZipStorer zip = ZipStorer.Open(modZipFile, FileAccess.Read)) {
                    foreach (var fileEntry in zip.ReadCentralDir()) {
                        zip.ExtractFile(fileEntry, Path.Combine(outputDir, fileEntry.FilenameInZip));
                    }
                }
            }
        }

        private void ReadModInfoZip(string zipFile)
        {
            UnzipMod(zipFile);

            var path = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(zipFile));
            string file = Directory.EnumerateFiles(
                path, "info.json", SearchOption.AllDirectories).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(file))
                return;

            ReadModInfo(File.ReadAllText(file), Path.GetDirectoryName(file));
        }

        private void ReadModInfo(string json, string path)
        {
            Mod newMod = JsonConvert.DeserializeObject<Mod>(json);
            newMod.Dir = path;

            Version parsedVersion;
            if (!Version.TryParse(newMod.Version, out parsedVersion))
                parsedVersion = new Version(0, 0, 0, 0);

            newMod.ParsedVersion = parsedVersion;
            ParseModDependencies(newMod);

            Mods.Add(newMod);
        }

        private void ParseModDependencies(Mod mod)
        {
            if (mod.Name == "base")
                mod.Dependencies.Add("core");

            foreach (string depString in mod.Dependencies) {
                int token = 0;

                string[] split = depString.Split(' ');

                bool optional = false;
                if (split[token] == "?") {
                    optional = true;
                    token++;
                }

                string modName = split[token];
                token++;

                Version version = null;
                DependencyType versionType = DependencyType.EqualTo;
                if (split.Length == token + 2) {
                    switch (split[token]) {
                        case "=":
                            versionType = DependencyType.EqualTo;
                            break;
                        case ">":
                            versionType = DependencyType.GreaterThan;
                            break;
                        case ">=":
                            versionType = DependencyType.GreaterThanOrEqual;
                            break;
                    }
                    token++;

                    if (!Version.TryParse(split[token], out version)) {
                        version = new Version(0, 0, 0, 0);
                    }
                    token++;
                }

                mod.ParsedDependencies.Add(new ModDependency(modName, version, optional, versionType));
            }
        }

        private void InterpretItems(Lua lua, string typeName)
        {
            if (lua.GetTable("data.raw")[typeName] is LuaTable itemTable) {
                foreach (KeyValuePair<object, object> entry in itemTable)
                    InterpretLuaItem(entry.Key as string, entry.Value as LuaTable);
            }
        }

        public async Task ChangeLocaleAsync(string newLocale)
        {
            localeFiles = await LoadLocaleFilesAsync(newLocale, Mods, FailedFiles);
        }

        private static async Task<LocalizedStringDictionary> LoadLocaleFilesAsync(
            string locale, IEnumerable<Mod> mods, Dictionary<string, Exception> failedFiles = null)
        {
            var localeFiles = new LocalizedStringDictionary();

            foreach (Mod mod in mods.Where(m => m.Enabled)) {
                var localDir = Path.Combine("locale", locale);
                foreach (string file in mod.EnumerateFiles(localDir, "*.cfg")) {
                    try {
                        await LoadLocaleFileAsync(file, localeFiles);
                    } catch (Exception ex) when (failedFiles != null) {
                        failedFiles[file] = ex;
                    }
                }
            }

            return localeFiles;
        }

        private static async Task LoadLocaleFileAsync(
            string file, LocalizedStringDictionary newLocaleFiles)
        {
            using (var stream = new StreamReader(file)) {
                string iniSection = "none";

                while (!stream.EndOfStream) {
                    string line = await stream.ReadLineAsync();
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
        }

        private BitmapSource LoadModImage(string filePath)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            string fullPath = "";
            if (File.Exists(fileName)) {
                fullPath = fileName;
            } else if (File.Exists(Application.StartupPath + "\\" + fileName)) {
                fullPath = Application.StartupPath + "\\" + fileName;
            } else {
                string[] splitPath = fileName.Split('/');
                splitPath[0] = splitPath[0].Trim('_');

                if (Mods.Any(m => m.Name == splitPath[0])) {
                    fullPath = Mods.First(m => m.Name == splitPath[0]).Dir;
                }

                if (!string.IsNullOrEmpty(fullPath)) {
                    for (int i = 1;
                         i < splitPath.Length;
                         i++) //Skip the first split section because it's the mod name, not a directory
                    {
                        fullPath = Path.Combine(fullPath, splitPath[i]);
                    }
                }
            }

            try {
                using (var stream = File.OpenRead(fullPath))
                    return LoadImage(stream);
            } catch (Exception) {
                return null;
            }
        }

        private static BitmapSource LoadImage(Stream source)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = source;
            image.EndInit();
            image.Freeze();
            return image;
        }

        public static Color IconAverageColor(BitmapSource icon)
        {
            if (icon == null)
                return Colors.LightGray;

            Color result;
            if (colorCache.TryGetValue(icon, out result))
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

        private void InterpretLuaItem(string name, LuaTable values)
        {
            if (string.IsNullOrEmpty(name))
                return;

            Item newItem = new Item(name);
            var fileName = values.StringOrDefault("icon");
            if (fileName == null) {
                var icons = values.TableOrDefault("icons");
                // TODO: Figure out how to either composite multiple icons
                var first = (LuaTable)icons?[1];
                if (first != null)
                    fileName = first.StringOrDefault("icon");
            }

            newItem.Icon = LoadImage(fileName);
            newItem.LocalizedName = GetLocalizationInfo(values);

            if (!Items.ContainsKey(name)) {
                Items.Add(name, newItem);
            }
        }

        private LocalizationInfo GetLocalizationInfo(LuaTable values)
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

        private bool SplitKey(string key, out string section, out string name)
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

                Dictionary<Item, float> ingredients = ExtractIngredientsFromLuaRecipe(values);
                Dictionary<Item, float> results = ExtractResultsFromLuaRecipe(values);

                if (name == null)
                    name = results.ElementAt(0).Key.Name;
                Recipe newRecipe = new Recipe(name, time == 0.0f ? defaultRecipeTime : time, ingredients, results);

                newRecipe.Category = values.StringOrDefault("category", "crafting");

                string iconFile = values.StringOrDefault("icon");
                if (iconFile != null)
                    newRecipe.Icon = LoadImage(iconFile);

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
            assembler.Icon = LoadImage(values.StringOrDefault("icon"));
            assembler.MaxIngredients = values.IntOrDefault("ingredient_count");
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

            LuaTable effects = values.TableOrDefault("allowed_effects");
            if (effects != null) {
                foreach (string effect in effects.Values) {
                    assembler.AllowedEffects.Add(effect);
                }
            }

            foreach (string s in Settings.Default.EnabledAssemblers) {
                if (s.Split('|')[0] == assembler.Name) {
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
            if (value.EndsWith("kW"))
                return Power.FromKilowatts(double.Parse(value.Substring(0, value.Length - 2)));
            if (value.EndsWith("W"))
                return new Power(double.Parse(value.Substring(0, value.Length - 1)));

            throw new ArgumentException($"Invalid power value '{value}'");
        }

        private void InterpretFurnace(string name, LuaTable values)
        {
            try {
                var newFurnace = new Assembler(name);
                ReadAssemblerProperties(newFurnace, values);

                if (newFurnace.Speed == -1f) {
                    //In case we're still on Factorio 0.10
                    newFurnace.Speed = values.Float("smelting_speed");
                }

                if (values["crafting_categories"] == null) {
                    //Another 0.10 compatibility thing.
                    LuaTable categories = values.Table("smelting_categories");
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

                newMiner.Icon = LoadImage(values.StringOrDefault("icon"));
                newMiner.MiningPower = values.Float("mining_power");
                newMiner.Speed = values.Float("mining_speed");
                newMiner.ModuleSlots = values.IntOrDefault("module_slots");
                if (newMiner.ModuleSlots == 0) {
                    var moduleTable = values.TableOrDefault("module_specification");
                    if (moduleTable != null)
                        newMiner.ModuleSlots = moduleTable.IntOrDefault("module_slots");
                }

                LuaTable categories = values.Table("resource_categories");
                if (categories != null) {
                    foreach (string category in categories.Values) {
                        newMiner.ResourceCategories.Add(category);
                    }
                }

                foreach (string s in Settings.Default.EnabledMiners) {
                    if (s.Split('|')[0] == name) {
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

                var newResource = new Resource(name);
                newResource.Category = values.StringOrDefault("category", "basic-solid");
                LuaTable minableTable = values.TableOrDefault("minable");
                newResource.Hardness = minableTable.Float("hardness");
                newResource.Time = minableTable.Float("mining_time");

                if (minableTable["result"] != null) {
                    newResource.Result = minableTable.String("result");
                } else {
                    newResource.Result = ((minableTable["results"] as LuaTable)?[1] as LuaTable)?["name"] as string;
                    if (newResource.Result == null) {
                        throw new MissingPrototypeValueException(minableTable, "results");
                    }
                }

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
                float speedBonus = 0f;
                float productivityBonus = 0f;
                float consumptionBonus = 0f;

                LuaTable effectTable = values.Table("effect");

                LuaTable speed = effectTable.TableOrDefault("speed");
                if (speed != null) {
                    speedBonus = speed.FloatOrDefault("bonus");
                }

                LuaTable productivity = effectTable.TableOrDefault("productivity");
                if (productivity != null) {
                    productivityBonus = productivity.FloatOrDefault("bonus");
                }

                LuaTable consumption = effectTable.TableOrDefault("consumption");
                if (consumption != null)
                    consumptionBonus = consumption.FloatOrDefault("bonus");

                var limitations = values.TableOrDefault("limitation");
                List<string> allowedIn = null;
                if (limitations != null) {
                    allowedIn = new List<string>();
                    foreach (var recipe in limitations.Values) {
                        allowedIn.Add((string)recipe);
                    }
                }

                var newModule = new Module(
                    name, speedBonus, productivityBonus, consumptionBonus, allowedIn);

                foreach (string s in Settings.Default.EnabledModules) {
                    if (s.Split('|')[0] == name) {
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

        private void InterpretInserter(string name, LuaTable values)
        {
            try {
                float rotationSpeed = values.Float("rotation_speed");
                Inserter newInserter = new Inserter(name);
                newInserter.RotationSpeed = rotationSpeed;
                newInserter.Icon = LoadImage(values.StringOrDefault("icon"));

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

            LuaTable source = null;

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
                LuaTable resultsTable = source.TableOrDefault("results");

                if (resultsTable != null) {
                    foreach (LuaTable resultTable in resultsTable.Values) {
                        Item result = FindOrCreateUnknownItem(
                            (string)resultTable["name"] ?? (string)resultTable[1]);

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
                string name = (ingredientTable["name"] ?? ingredientTable[1]) as string;
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
            new List<string> { "item-name", "fluid-name", "entity-name", "equipment-name" };

        public string GetLocalizedString(string category, string name)
        {
            if (localeFiles.TryGetValue(category, name, out var localized))
                return localized;
            return name;
        }

        public bool TryGetLocalizedString(string category, string name, out string localized)
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

        public string GetLocalizedString(string name, LocalizationInfo locInfo)
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

        public static string String(this LuaTable table, string key)
        {
            if (table[key] != null)
                return Convert.ToString(table[key]);

            throw new MissingPrototypeValueException(table, key, "Key is missing");
        }

        public static string StringOrDefault(
            this LuaTable table, string key, string defaultValue = null)
        {
            if (table[key] != null)
                return Convert.ToString(table[key]);
            return defaultValue;
        }

        public static LuaTable Table(this LuaTable table, string key)
        {
            if (table[key] != null)
                return table[key] as LuaTable;

            throw new MissingPrototypeValueException(table, key, "Key is missing");
        }

        public static LuaTable TableOrDefault(
            this LuaTable table, string key, LuaTable defaultValue = null)
        {
            if (table[key] != null)
                return table[key] as LuaTable;
            return defaultValue;
        }
    }
}
