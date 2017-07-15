namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Windows.Forms;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Extensions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NLua;
    using Properties;

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

    public static class DataCache
    {
        private class MissingPrototypeValueException : Exception
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

        private static string DataPath => Path.Combine(Settings.Default.FactorioPath, "data");

        private static string ModPath => Settings.Default.FactorioModPath;

        public static List<Mod> Mods { get; set; } = new List<Mod>();
        public static List<Language> Languages { get; } = new List<Language>();

        public static string Difficulty = "normal";

        public static Dictionary<string, Item> Items { get; } = new Dictionary<string, Item>();
        public static Dictionary<string, Recipe> Recipes { get; } = new Dictionary<string, Recipe>();
        public static Dictionary<string, Assembler> Assemblers { get; } = new Dictionary<string, Assembler>();
        public static Dictionary<string, Miner> Miners { get; } = new Dictionary<string, Miner>();
        public static Dictionary<string, Resource> Resources { get; } = new Dictionary<string, Resource>();
        public static Dictionary<string, Module> Modules { get; } = new Dictionary<string, Module>();
        public static Dictionary<string, Inserter> Inserters { get; } = new Dictionary<string, Inserter>();

        private const float defaultRecipeTime = 0.5f;
        private static readonly Dictionary<BitmapSource, Color> colourCache = new Dictionary<BitmapSource, Color>();
        public static BitmapSource UnknownIcon;

        public static Dictionary<string, Dictionary<string, string>> LocaleFiles { get; } =
            new Dictionary<string, Dictionary<string, string>>();

        public static Dictionary<string, Exception> FailedFiles { get; } = new Dictionary<string, Exception>();
        public static Dictionary<string, Exception> FailedPathDirectories { get; } = new Dictionary<string, Exception>();

        public static Dictionary<string, byte[]> ZipHashes { get; } = new Dictionary<string, byte[]>();

        public static void LoadAllData(List<string> enabledMods)
        {
            Clear();

            using (Lua lua = new Lua()) {
                FindAllMods(enabledMods);

                AddLuaPackagePath(lua, Path.Combine(DataPath, "core", "lualib")); //Core lua functions
                string basePackagePath = lua["package.path"] as string;

                string dataloaderFile = Path.Combine(DataPath, "core", "lualib", "dataloader.lua");
                try {
                    lua.DoFile(dataloaderFile);
                } catch (Exception e) {
                    FailedFiles[dataloaderFile] = e;
                    ErrorLogging.LogLine(string.Format(
                        "Error loading dataloader.lua. This file is required to load any values from the prototypes. Message: '{0}'",
                        e.Message));
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

                foreach (string filename in new[] { "data.lua", "data-updates.lua", "data-final-fixes.lua" }) {
                    foreach (Mod mod in Mods.Where(m => m.Enabled)) {
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
                            } catch (Exception e) {
                                FailedFiles[dataFile] = e;
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

                LuaTable recipeTable = lua.GetTable("data.raw")["recipe"] as LuaTable;
                if (recipeTable != null) {
                    var recipeEnumerator = recipeTable.GetEnumerator();
                    while (recipeEnumerator.MoveNext()) {
                        InterpretLuaRecipe(recipeEnumerator.Key as string, recipeEnumerator.Value as LuaTable);
                    }
                }

                LuaTable assemblerTable = lua.GetTable("data.raw")["assembling-machine"] as LuaTable;
                if (assemblerTable != null) {
                    var assemblerEnumerator = assemblerTable.GetEnumerator();
                    while (assemblerEnumerator.MoveNext()) {
                        InterpretAssemblingMachine(assemblerEnumerator.Key as string,
                            assemblerEnumerator.Value as LuaTable);
                    }
                }

                LuaTable furnaceTable = lua.GetTable("data.raw")["furnace"] as LuaTable;
                if (furnaceTable != null) {
                    var furnaceEnumerator = furnaceTable.GetEnumerator();
                    while (furnaceEnumerator.MoveNext()) {
                        InterpretFurnace(furnaceEnumerator.Key as string, furnaceEnumerator.Value as LuaTable);
                    }
                }

                LuaTable minerTable = lua.GetTable("data.raw")["mining-drill"] as LuaTable;
                if (minerTable != null) {
                    var minerEnumerator = minerTable.GetEnumerator();
                    while (minerEnumerator.MoveNext()) {
                        InterpretMiner(minerEnumerator.Key as string, minerEnumerator.Value as LuaTable);
                    }
                }

                LuaTable resourceTable = lua.GetTable("data.raw")["resource"] as LuaTable;
                if (resourceTable != null) {
                    var resourceEnumerator = resourceTable.GetEnumerator();
                    while (resourceEnumerator.MoveNext()) {
                        InterpretResource(resourceEnumerator.Key as string, resourceEnumerator.Value as LuaTable);
                    }
                }

                LuaTable moduleTable = lua.GetTable("data.raw")["module"] as LuaTable;
                if (moduleTable != null) {
                    foreach (string moduleName in moduleTable.Keys) {
                        InterpretModule(moduleName, moduleTable[moduleName] as LuaTable);
                    }
                }

                UnknownIcon = LoadImage("UnknownIcon.png");
                if (UnknownIcon == null) {
                    var pixels = new uint[32 * 32];
                    for (int i = 0; i < pixels.Length; ++i)
                        pixels[i] = 0xFFFFFFFF;
                    UnknownIcon = BitmapSource.Create(
                        32, 32, 96, 96, PixelFormats.Pbgra32, null, pixels, 32);
                    UnknownIcon.Freeze();
                }

                LoadAllLanguages();
                LoadLocaleFiles();
            }

            MarkCyclicRecipes();

            ReportErrors();
        }

        private static void LoadAllLanguages()
        {
            var dirList = Directory.EnumerateDirectories(Path.Combine(Mods.First(m => m.Name == "core").Dir, "locale"));

            foreach (string dir in dirList) {
                var newLanguage = new Language(Path.GetFileName(dir));
                try {
                    string infoJson = File.ReadAllText(Path.Combine(dir, "info.json"));
                    newLanguage.LocalName = (string)JObject.Parse(infoJson)["language-name"];
                } catch {
                }
                Languages.Add(newLanguage);
            }
        }

        public static void Clear()
        {
            Mods.Clear();
            Items.Clear();
            Recipes.Clear();
            Assemblers.Clear();
            Miners.Clear();
            Resources.Clear();
            Modules.Clear();
            colourCache.Clear();
            LocaleFiles.Clear();
            FailedFiles.Clear();
            FailedPathDirectories.Clear();
            Inserters.Clear();
            Languages.Clear();
        }

        private static float ReadLuaFloat(LuaTable table, string key, bool canBeMissing = false,
            float defaultValue = 0f)
        {
            if (table[key] == null) {
                if (canBeMissing) {
                    return defaultValue;
                }
                throw new MissingPrototypeValueException(table, key, "Key is missing");
            }

            try {
                return Convert.ToSingle(table[key]);
            } catch (FormatException) {
                throw new MissingPrototypeValueException(table, key,
                    string.Format("Expected a float, but the value ('{0}') isn't one", table[key]));
            }
        }

        private static int ReadLuaInt(LuaTable table, string key, bool canBeMissing = false, int defaultValue = 0)
        {
            if (table[key] == null) {
                if (canBeMissing) {
                    return defaultValue;
                }
                throw new MissingPrototypeValueException(table, key, "Key is missing");
            }

            try {
                return Convert.ToInt32(table[key]);
            } catch (FormatException) {
                throw new MissingPrototypeValueException(table, key,
                    string.Format("Expected an Int32, but the value ('{0}') isn't one", table[key]));
            }
        }

        private static string ReadLuaString(LuaTable table, string key, bool canBeMissing = false,
            string defaultValue = null)
        {
            if (table[key] == null) {
                if (canBeMissing) {
                    return defaultValue;
                }
                throw new MissingPrototypeValueException(table, key, "Key is missing");
            }

            return Convert.ToString(table[key]);
        }

        private static LuaTable ReadLuaLuaTable(LuaTable table, string key, bool canBeMissing = false)
        {
            if (table[key] == null) {
                if (canBeMissing) {
                    return null;
                }
                throw new MissingPrototypeValueException(table, key, "Key is missing");
            }

            try {
                return table[key] as LuaTable;
            } catch (Exception) {
                throw new MissingPrototypeValueException(table, key, "Could not convert key to LuaTable");
            }
        }

        private static void ReportErrors()
        {
            if (FailedPathDirectories.Any()) {
                ErrorLogging.LogLine("There were errors setting the lua path variable for the following directories:");
                foreach (string dir in FailedPathDirectories.Keys) {
                    ErrorLogging.LogLine(string.Format("{0} ({1})", dir, FailedPathDirectories[dir].Message));
                }
            }

            if (FailedFiles.Any()) {
                ErrorLogging.LogLine("The following files could not be loaded due to errors:");
                foreach (string file in FailedFiles.Keys) {
                    ErrorLogging.LogLine(string.Format("{0} ({1})", file, FailedFiles[file].Message));
                }
            }
        }

        private static void AddLuaPackagePath(Lua lua, string dir)
        {
            try {
                string luaCommand = string.Format("package.path = package.path .. ';{0}{1}?.lua'", dir,
                    Path.DirectorySeparatorChar);
                luaCommand = luaCommand.Replace("\\", "\\\\");
                lua.DoString(luaCommand);
            } catch (Exception e) {
                FailedPathDirectories[dir] = e;
            }
        }

        private static IEnumerable<string> GetAllLuaFiles()
        {
            if (Directory.Exists(ModPath)) {
                foreach (string file in Directory.GetFiles(DataPath, "*.lua", SearchOption.AllDirectories)) {
                    yield return file;
                }
            }
            if (Directory.Exists(ModPath)) {
                foreach (string file in Directory.GetFiles(ModPath, "*.lua", SearchOption.AllDirectories)) {
                    yield return file;
                }
            }
        }

        private static void FindAllMods(List<string> enabledMods) //Vanilla game counts as a mod too.
        {
            if (Directory.Exists(DataPath)) {
                foreach (string dir in Directory.EnumerateDirectories(DataPath)) {
                    ReadModInfoFile(dir);
                }
            }
            if (Directory.Exists(Settings.Default.FactorioModPath)) {
                foreach (string dir in Directory.EnumerateDirectories(Settings.Default.FactorioModPath)) {
                    ReadModInfoFile(dir);
                }
                foreach (string zipFile in Directory.EnumerateFiles(Settings.Default.FactorioModPath,
                    "*.zip")) {
                    ReadModInfoZip(zipFile);
                }
            }

            Dictionary<string, bool> enabledModsFromFile = new Dictionary<string, bool>();

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
                Dictionary<string, string> splitModStrings = new Dictionary<string, string>();
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

            DependencyGraph modGraph = new DependencyGraph(Mods);
            modGraph.DisableUnsatisfiedMods();
            Mods = modGraph.SortMods();
        }

        private static void ReadModInfoFile(string dir)
        {
            try {
                if (!File.Exists(Path.Combine(dir, "info.json"))) {
                    return;
                }
                string json = File.ReadAllText(Path.Combine(dir, "info.json"));
                ReadModInfo(json, dir);
            } catch (Exception) {
                ErrorLogging.LogLine(string.Format("The mod at '{0}' has an invalid info.json file", dir));
            }
        }

        private static void UnzipMod(string modZipFile)
        {
            string fullPath = Path.GetFullPath(modZipFile);
            byte[] hash;
            bool needsExtraction = false;

            using (var md5 = MD5.Create()) {
                using (var stream = File.OpenRead(fullPath)) {
                    hash = md5.ComputeHash(stream);
                }
            }

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

        private static void ReadModInfoZip(string zipFile)
        {
            UnzipMod(zipFile);

            string file = Directory
                .EnumerateFiles(Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(zipFile)),
                    "info.json", SearchOption.AllDirectories).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(file)) {
                return;
            }
            ReadModInfo(File.ReadAllText(file), Path.GetDirectoryName(file));
        }

        private static void ReadModInfo(string json, string dir)
        {
            Mod newMod = JsonConvert.DeserializeObject<Mod>(json);
            newMod.Dir = dir;

            Version parsedVersion;
            if (!Version.TryParse(newMod.Version, out parsedVersion))
                parsedVersion = new Version(0, 0, 0, 0);

            newMod.ParsedVersion = parsedVersion;
            ParseModDependencies(newMod);

            Mods.Add(newMod);
        }

        private static void ParseModDependencies(Mod mod)
        {
            if (mod.Name == "base") {
                mod.Dependencies.Add("core");
            }

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

        private static void InterpretItems(Lua lua, string typeName)
        {
            LuaTable itemTable = lua.GetTable("data.raw")[typeName] as LuaTable;

            var table = lua.GetTable("data.raw")["solar-panel"] as LuaTable;

            if (itemTable != null) {
                var enumerator = itemTable.GetEnumerator();
                while (enumerator.MoveNext()) {
                    InterpretLuaItem(enumerator.Key as string, enumerator.Value as LuaTable);
                }
            }
        }

        public static void LoadLocaleFiles(string locale = "en")
        {
            foreach (Mod mod in Mods.Where(m => m.Enabled)) {
                string localeDir = Path.Combine(mod.Dir, "locale", locale);
                if (Directory.Exists(localeDir)) {
                    foreach (string file in Directory.GetFiles(localeDir, "*.cfg")) {
                        try {
                            using (StreamReader fStream = new StreamReader(file)) {
                                string currentIniSection = "none";

                                while (!fStream.EndOfStream) {
                                    string line = fStream.ReadLine();
                                    if (line.StartsWith("[") && line.EndsWith("]")) {
                                        currentIniSection = line.Trim('[', ']');
                                    } else {
                                        if (!LocaleFiles.ContainsKey(currentIniSection)) {
                                            LocaleFiles.Add(currentIniSection, new Dictionary<string, string>());
                                        }
                                        string[] split = line.Split('=');
                                        if (split.Length == 2) {
                                            LocaleFiles[currentIniSection][split[0].Trim()] = split[1].Trim();
                                        }
                                    }
                                }
                            }
                        } catch (Exception e) {
                            FailedFiles[file] = e;
                        }
                    }
                }
            }
        }

        private static BitmapSource LoadImage(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) {
                return null;
            }

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

        public static Color IconAverageColour(BitmapSource icon)
        {
            if (icon == null) {
                return Colors.LightGray;
            }

            Color result;
            if (colourCache.ContainsKey(icon)) {
                result = colourCache[icon];
            } else {
                result = icon.ComputeAvgColor();

                // Set alpha to 255, also lighten the colours to make them more pastel-y
                result = Color.FromArgb(
                    255,
                    (byte)(result.R + (255 - result.R) / 2),
                    (byte)(result.G + (255 - result.G) / 2),
                    (byte)(result.B + (255 - result.B) / 2));
                colourCache.Add(icon, result);
            }

            return result;
        }

        private static void InterpretLuaItem(string name, LuaTable values)
        {
            if (string.IsNullOrEmpty(name)) {
                return;
            }
            Item newItem = new Item(name);
            var fileName = ReadLuaString(values, "icon", true);
            if (fileName == null) {
                var icons = ReadLuaLuaTable(values, "icons", true);
                // TODO: Figure out how to either composite multiple icons
                var first = (LuaTable)icons?[1];
                if (first != null)
                    fileName = ReadLuaString(first, "icon", true);
            }

            newItem.Icon = LoadImage(fileName);

            if (!Items.ContainsKey(name)) {
                Items.Add(name, newItem);
            }
        }

        //This is only if a recipe references an item that isn't in the item prototypes (which shouldn't really happen)
        private static Item FindOrCreateUnknownItem(string itemName)
        {
            Item newItem;
            if (!Items.ContainsKey(itemName)) {
                Items.Add(itemName, newItem = new Item(itemName));
            } else {
                newItem = Items[itemName];
            }
            return newItem;
        }

        private static void InterpretLuaRecipe(string name, LuaTable values)
        {
            try {
                var timeSource = values[Difficulty] == null ? values : ReadLuaLuaTable(values, Difficulty, true);
                if (timeSource == null) {
                    ErrorLogging.LogLine($"Error reading recipe '{name}', unable to locate data table.");
                    return;
                }

                float time = ReadLuaFloat(timeSource, "energy_required", true, 0.5f);

                Dictionary<Item, float> ingredients = ExtractIngredientsFromLuaRecipe(values);
                Dictionary<Item, float> results = ExtractResultsFromLuaRecipe(values);

                if (name == null)
                    name = results.ElementAt(0).Key.Name;
                Recipe newRecipe = new Recipe(name, time == 0.0f ? defaultRecipeTime : time, ingredients, results);

                newRecipe.Category = ReadLuaString(values, "category", true, "crafting");

                string iconFile = ReadLuaString(values, "icon", true);
                if (iconFile != null) {
                    BitmapSource icon = LoadImage(iconFile);
                    newRecipe.Icon = icon;
                }

                foreach (Item result in results.Keys) {
                    result.Recipes.Add(newRecipe);
                }

                Recipes.Add(newRecipe.Name, newRecipe);
            } catch (MissingPrototypeValueException e) {
                ErrorLogging.LogLine(string.Format(
                    "Error reading value '{0}' from recipe prototype '{1}'. Returned error message: '{2}'", e.Key, name,
                    e.Message));
            }
        }

        private static void InterpretAssemblingMachine(string name, LuaTable values)
        {
            try {
                Assembler newAssembler = new Assembler(name);

                newAssembler.Icon = LoadImage(ReadLuaString(values, "icon", true));
                newAssembler.MaxIngredients = ReadLuaInt(values, "ingredient_count");
                newAssembler.ModuleSlots = ReadLuaInt(values, "module_slots", true, 0);
                if (newAssembler.ModuleSlots == 0) {
                    var moduleTable = ReadLuaLuaTable(values, "module_specification", true);
                    if (moduleTable != null) {
                        newAssembler.ModuleSlots = ReadLuaInt(moduleTable, "module_slots", true, 0);
                    }
                }
                newAssembler.Speed = ReadLuaFloat(values, "crafting_speed");

                LuaTable effects = ReadLuaLuaTable(values, "allowed_effects", true);
                if (effects != null) {
                    foreach (string effect in effects.Values) {
                        newAssembler.AllowedEffects.Add(effect);
                    }
                }
                LuaTable categories = ReadLuaLuaTable(values, "crafting_categories");
                foreach (string category in categories.Values) {
                    newAssembler.Categories.Add(category);
                }

                foreach (string s in Settings.Default.EnabledAssemblers) {
                    if (s.Split('|')[0] == name) {
                        newAssembler.Enabled = (s.Split('|')[1] == "True");
                    }
                }

                Assemblers.Add(newAssembler.Name, newAssembler);
            } catch (MissingPrototypeValueException e) {
                ErrorLogging.LogLine(string.Format(
                    "Error reading value '{0}' from assembler prototype '{1}'. Returned error message: '{2}'", e.Key,
                    name, e.Message));
            }
        }

        private static void InterpretFurnace(string name, LuaTable values)
        {
            try {
                Assembler newFurnace = new Assembler(name);

                newFurnace.Icon = LoadImage(ReadLuaString(values, "icon", true));
                newFurnace.MaxIngredients = 1;
                newFurnace.ModuleSlots = ReadLuaInt(values, "module_slots", true, 0);
                if (newFurnace.ModuleSlots == 0) {
                    var moduleTable = ReadLuaLuaTable(values, "module_specification", true);
                    if (moduleTable != null) {
                        newFurnace.ModuleSlots = ReadLuaInt(moduleTable, "module_slots", true, 0);
                    }
                }
                newFurnace.Speed = ReadLuaFloat(values, "crafting_speed", true, -1f);
                if (newFurnace.Speed == -1f) {
                    //In case we're still on Factorio 0.10
                    newFurnace.Speed = ReadLuaFloat(values, "smelting_speed");
                }

                LuaTable categories = ReadLuaLuaTable(values, "crafting_categories", true);
                if (categories == null) {
                    //Another 0.10 compatibility thing.
                    categories = ReadLuaLuaTable(values, "smelting_categories");
                }
                foreach (string category in categories.Values) {
                    newFurnace.Categories.Add(category);
                }

                foreach (string s in Settings.Default.EnabledAssemblers) {
                    if (s.Split('|')[0] == name) {
                        newFurnace.Enabled = (s.Split('|')[1] == "True");
                    }
                }

                Assemblers.Add(newFurnace.Name, newFurnace);
            } catch (MissingPrototypeValueException e) {
                ErrorLogging.LogLine(string.Format(
                    "Error reading value '{0}' from furnace prototype '{1}'. Returned error message: '{2}'", e.Key,
                    name, e.Message));
            }
        }

        private static void InterpretMiner(string name, LuaTable values)
        {
            try {
                Miner newMiner = new Miner(name);

                newMiner.Icon = LoadImage(ReadLuaString(values, "icon", true));
                newMiner.MiningPower = ReadLuaFloat(values, "mining_power");
                newMiner.Speed = ReadLuaFloat(values, "mining_speed");
                newMiner.ModuleSlots = ReadLuaInt(values, "module_slots", true, 0);
                if (newMiner.ModuleSlots == 0) {
                    var moduleTable = ReadLuaLuaTable(values, "module_specification", true);
                    if (moduleTable != null) {
                        newMiner.ModuleSlots = ReadLuaInt(moduleTable, "module_slots", true, 0);
                    }
                }

                LuaTable categories = ReadLuaLuaTable(values, "resource_categories");
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
            } catch (MissingPrototypeValueException e) {
                ErrorLogging.LogLine(string.Format(
                    "Error reading value '{0}' from miner prototype '{1}'. Returned error message: '{2}'", e.Key, name,
                    e.Message));
            }
        }

        private static void InterpretResource(string name, LuaTable values)
        {
            try {
                if (values["minable"] == null) {
                    return; //This means the resource is not usable by miners and is therefore not useful to us
                }
                Resource newResource = new Resource(name);
                newResource.Category = ReadLuaString(values, "category", true, "basic-solid");
                LuaTable minableTable = ReadLuaLuaTable(values, "minable", true);
                newResource.Hardness = ReadLuaFloat(minableTable, "hardness");
                newResource.Time = ReadLuaFloat(minableTable, "mining_time");

                if (minableTable["result"] != null) {
                    newResource.Result = ReadLuaString(minableTable, "result");
                } else {
                    try {
                        newResource.Result = ((minableTable["results"] as LuaTable)[1] as LuaTable)["name"] as string;
                    } catch (Exception e) {
                        throw new MissingPrototypeValueException(minableTable, "results", e.Message);
                    }
                }

                Resources.Add(name, newResource);
            } catch (MissingPrototypeValueException e) {
                ErrorLogging.LogLine(string.Format(
                    "Error reading value '{0}' from resource prototype '{1}'. Returned error message: '{2}'", e.Key,
                    name, e.Message));
            }
        }

        private static void InterpretModule(string name, LuaTable values)
        {
            try {
                float speedBonus = 0f;
                float productivityBonus = 0f;

                LuaTable effectTable = ReadLuaLuaTable(values, "effect");
                LuaTable speed = ReadLuaLuaTable(effectTable, "speed", true);
                if (speed != null) {
                    speedBonus = ReadLuaFloat(speed, "bonus", true, -1f);
                }

                LuaTable productivity = ReadLuaLuaTable(effectTable, "productivity", true);
                if (productivity != null) {
                    productivityBonus = ReadLuaFloat(productivity, "bonus", true, -1f);
                }

                /*
                if (speed == null || speedBonus <= 0)
                {
                    return;
                }
                */
                var limitations = ReadLuaLuaTable(values, "limitation", true);
                List<string> allowedIn = null;
                if (limitations != null) {
                    allowedIn = new List<string>();
                    foreach (var recipe in limitations.Values) {
                        allowedIn.Add((string)recipe);
                    }
                }

                Module newModule = new Module(name, speedBonus, productivityBonus, allowedIn);

                foreach (string s in Settings.Default.EnabledModules) {
                    if (s.Split('|')[0] == name) {
                        newModule.Enabled = (s.Split('|')[1] == "True");
                    }
                }

                Modules.Add(name, newModule);
            } catch (MissingPrototypeValueException e) {
                ErrorLogging.LogLine(string.Format(
                    "Error reading value '{0}' from module prototype '{1}'. Returned error message: '{2}'", e.Key, name,
                    e.Message));
            }
        }

        private static void InterpretInserter(string name, LuaTable values)
        {
            try {
                float rotationSpeed = ReadLuaFloat(values, "rotation_speed");
                Inserter newInserter = new Inserter(name);
                newInserter.RotationSpeed = rotationSpeed;
                newInserter.Icon = LoadImage(ReadLuaString(values, "icon", true));

                Inserters.Add(name, newInserter);
            } catch (MissingPrototypeValueException e) {
                ErrorLogging.LogLine(string.Format(
                    "Error reading value '{0}' from inserter prototype '{1}'. Returned error message: '{2}'", e.Key,
                    name, e.Message));
            }
        }

        private static Dictionary<Item, float> ExtractResultsFromLuaRecipe(LuaTable values)
        {
            Dictionary<Item, float> results = new Dictionary<Item, float>();

            LuaTable source = null;

            if (values[Difficulty] == null)
                source = values;
            else {
                var difficultyTable = ReadLuaLuaTable(values, Difficulty, true);
                if (difficultyTable?["result"] != null || difficultyTable?["results"] != null)
                    source = difficultyTable;
            }

            if (source?["result"] != null) {
                string resultName = ReadLuaString(source, "result");
                float resultCount = ReadLuaFloat(source, "result_count", true);
                if (resultCount == 0f) {
                    resultCount = 1f;
                }
                results.Add(FindOrCreateUnknownItem(resultName), resultCount);
            } else {
                // If we can't read results, try difficulty/results
                LuaTable resultsTable = ReadLuaLuaTable(source, "results", true);

                if (resultsTable != null) {
                    var resultEnumerator = resultsTable.GetEnumerator();
                    while (resultEnumerator.MoveNext()) {
                        LuaTable resultTable = resultEnumerator.Value as LuaTable;
                        Item result;
                        if (resultTable["name"] != null) {
                            result = FindOrCreateUnknownItem(ReadLuaString(resultTable, "name"));
                        } else {
                            result = FindOrCreateUnknownItem((string)resultTable[1]);
                        }

                        float amount = 0f;
                        if (resultTable["amount"] != null) {
                            amount = ReadLuaFloat(resultTable, "amount");
                            //Just the average yield. Maybe in the future it should show more information about the probability
                            var probability = ReadLuaFloat(resultTable, "probability", true, 1.0f);
                            amount *= probability;
                        } else if (resultTable["amount_min"] != null) {
                            float probability = ReadLuaFloat(resultTable, "probability", true, 1f);
                            float amount_min = ReadLuaFloat(resultTable, "amount_min");
                            float amount_max = ReadLuaFloat(resultTable, "amount_max");
                            amount = ((amount_min + amount_max) / 2f) *
                                     probability; //Just the average yield. Maybe in the future it should show more information about the probability
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

        private static Dictionary<Item, float> ExtractIngredientsFromLuaRecipe(LuaTable values)
        {
            Dictionary<Item, float> ingredients = new Dictionary<Item, float>();

            LuaTable ingredientsTable = ReadLuaLuaTable(values, "ingredients", true) ??
                                        ReadLuaLuaTable(ReadLuaLuaTable(values, Difficulty), "ingredients");

            var ingredientEnumerator = ingredientsTable.GetEnumerator();
            while (ingredientEnumerator.MoveNext()) {
                LuaTable ingredientTable = ingredientEnumerator.Value as LuaTable;
                string name;
                float amount;
                if (ingredientTable["name"] != null) {
                    name = ingredientTable["name"] as string;
                } else {
                    name = ingredientTable[1] as string; //Name and amount often have no key in the prototype
                }
                if (ingredientTable["amount"] != null) {
                    amount = Convert.ToSingle(ingredientTable["amount"]);
                } else {
                    amount = Convert.ToSingle(ingredientTable[2]);
                }
                Item ingredient = FindOrCreateUnknownItem(name);
                if (!ingredients.ContainsKey(ingredient)) {
                    ingredients.Add(ingredient, amount);
                } else {
                    ingredients[ingredient] += amount;
                }
            }

            return ingredients;
        }

        private static void MarkCyclicRecipes()
        {
            ProductionGraph testGraph = new ProductionGraph();
            foreach (Recipe recipe in Recipes.Values) {
                var node = RecipeNode.Create(recipe, testGraph);
            }
            testGraph.CreateAllPossibleInputLinks();
            foreach (var scc in testGraph.GetStronglyConnectedComponents(true)) {
                foreach (var node in scc) {
                    ((RecipeNode)node).BaseRecipe.IsCyclic = true;
                }
            }
        }
    }
}
