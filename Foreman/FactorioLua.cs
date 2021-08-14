namespace Foreman
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using Foreman.Extensions;
    using Foreman.Properties;
    using NLua;

    /// <summary>
    ///   Lua context customized for Factorio. Includes custom package searcher
    ///   and loader to load mods from zip files, serpent library used by
    ///   Factorio (and thus may be used by mods).
    /// </summary>
    internal class FactorioLua : Lua
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

            _ = DoString(@"
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
                var n => string.Join(", ", Enumerable.Range(0, n - 1).Select(x => "{" + x + '}')) + '\n',
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
                var n => string.Join(", ", Enumerable.Range(0, n - 1).Select(x => "{" + x + '}')) + '\n',
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
}
