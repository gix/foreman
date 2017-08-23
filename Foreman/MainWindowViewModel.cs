namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using Controls;
    using Extensions;
    using Microsoft.Win32;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Properties;
    using Views;

    public class MainWindowViewModel : ViewModel
    {
        private readonly IMainWindow view;

        private List<Item> unfilteredItemList = new List<Item>();
        private List<Recipe> unfilteredRecipeList = new List<Recipe>();
        private Difficulty difficulty;
        private Language selectedLanguage;
        private Item selectedItem;
        private bool showAssemblers;
        private bool showMiners;
        private string itemFilterText;
        private string recipeFilterText;
        private AmountType amountType = AmountType.FixedAmount;
        private RateUnit selectedRateUnit = RateUnit.PerSecond;

        public MainWindowViewModel(IMainWindow view)
        {
            this.view = view;

            var graph = new ProductionGraph();
            GraphViewModel = new ProductionGraphViewModel(graph);

            CompleteGraphCommand = new AsyncDelegateCommand(CompleteGraph);
            ClearGraphCommand = new AsyncDelegateCommand(ClearGraph);
            ArrangeNodesCommand = new AsyncDelegateCommand(ArrangeNodes);
            SaveGraphCommand = new AsyncDelegateCommand(SaveGraph);
            LoadGraphCommand = new AsyncDelegateCommand(LoadGraph);
            ExportImageCommand = new AsyncDelegateCommand(ExportImage);
            ChangeFactorioDirectoryCommand = new AsyncDelegateCommand(ChangeFactorioDirectory);
            ChangeModDirectoryCommand = new AsyncDelegateCommand(ChangeModDirectory);
            ReloadCommand = new AsyncDelegateCommand(Reload);
            EnableDisableCommand = new AsyncDelegateCommand(EnableDisable);
            AddItemsCommand = new AsyncDelegateCommand<UIElement>(AddItems, CanAddItems);
            AddRecipesCommand = new AsyncDelegateCommand(AddRecipes, CanAddRecipes);
        }

        public ProductionGraphViewModel GraphViewModel { get; }

        public AsyncDelegateCommand CompleteGraphCommand { get; }
        public AsyncDelegateCommand ClearGraphCommand { get; }
        public AsyncDelegateCommand ArrangeNodesCommand { get; }
        public AsyncDelegateCommand SaveGraphCommand { get; }
        public AsyncDelegateCommand LoadGraphCommand { get; }
        public AsyncDelegateCommand ExportImageCommand { get; }
        public AsyncDelegateCommand ChangeFactorioDirectoryCommand { get; }
        public AsyncDelegateCommand ChangeModDirectoryCommand { get; }
        public AsyncDelegateCommand ReloadCommand { get; }
        public AsyncDelegateCommand EnableDisableCommand { get; }
        public AsyncDelegateCommand<UIElement> AddItemsCommand { get; }
        public AsyncDelegateCommand AddRecipesCommand { get; }

        public ObservableCollection<Language> Languages { get; } = new ObservableCollection<Language>();

        public Language SelectedLanguage
        {
            get => selectedLanguage;
            set
            {
                if (SetProperty(ref selectedLanguage, value))
                    OnLanguageChanged();
            }
        }

        public ObservableCollection<Item> ItemList { get; } = new ObservableCollection<Item>();

        public Item SelectedItem
        {
            get => selectedItem;
            set => SetProperty(ref selectedItem, value);
        }

        public ObservableCollection<Recipe> RecipeList { get; } = new ObservableCollection<Recipe>();
        public IList<Recipe> SelectedRecipes { get; } = new ObservableCollection<Recipe>();

        public Difficulty Difficulty
        {
            get => difficulty;
            set
            {
                if (SetProperty(ref difficulty, value))
                    DifficultyChanged().Forget();
            }
        }

        public bool ShowAssemblers
        {
            get => showAssemblers;
            set
            {
                if (SetProperty(ref showAssemblers, value))
                    OnShowAssemblersChanged();
            }
        }

        public bool ShowMiners
        {
            get => showMiners;
            set
            {
                if (SetProperty(ref showMiners, value))
                    OnShowMinersChanged();
            }
        }

        public string ItemFilterText
        {
            get => itemFilterText;
            set
            {
                if (SetProperty(ref itemFilterText, value))
                    OnItemFilterTextChanged();
            }
        }

        public string RecipeFilterText
        {
            get => recipeFilterText;
            set
            {
                if (SetProperty(ref recipeFilterText, value))
                    OnRecipeFilterTextChanged();
            }
        }

        public AmountType AmountType
        {
            get => amountType;
            set
            {
                if (SetProperty(ref amountType, value))
                    OnAmountTypeChanged();
            }
        }

        public RateUnit SelectedRateUnit
        {
            get => selectedRateUnit;
            set
            {
                if (SetProperty(ref selectedRateUnit, value))
                    OnSelectedRateUnitChanged();
            }
        }

        public async Task Load()
        {
            //I changed the name of the variable, so this copies the value over for people who are upgrading their Foreman version
            if (Settings.Default.FactorioPath == "" && Settings.Default.FactorioDataPath != "") {
                Settings.Default.FactorioPath = Path.GetDirectoryName(Settings.Default.FactorioDataPath);
                Settings.Default.FactorioDataPath = "";
            }

            if (!Directory.Exists(Settings.Default.FactorioPath)) {
                foreach (string defaultPath in new[] {
                    Path.Combine(Environment.GetEnvironmentVariable("PROGRAMFILES(X86)"), "Factorio"),
                    Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432"), "Factorio"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Applications",
                        "factorio.app", "Contents")
                }) //Not actually tested on a Mac
                {
                    if (Directory.Exists(defaultPath)) {
                        Settings.Default.FactorioPath = defaultPath;
                        Settings.Default.Save();
                        break;
                    }
                }
            }

            if (!Directory.Exists(Settings.Default.FactorioPath)) {
                var dialog = new DirectoryChooserDialog("");
                if (dialog.ShowDialog(view) == true) {
                    Settings.Default.FactorioPath = dialog.SelectedPath;
                    Settings.Default.Save();
                } else {
                    view.Close();
                    return;
                }
            }

            if (!Directory.Exists(Settings.Default.FactorioModPath)) {
                if (Directory.Exists(Path.Combine(Settings.Default.FactorioPath, "mods"))) {
                    Settings.Default.FactorioModPath =
                        Path.Combine(Settings.Default.FactorioPath, "mods");
                } else if (Directory.Exists(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "factorio", "mods"))) {
                    Settings.Default.FactorioModPath =
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "factorio",
                            "mods");
                }
            }

            if (Settings.Default.EnabledMods == null)
                Settings.Default.EnabledMods = new StringCollection();
            if (Settings.Default.EnabledAssemblers == null)
                Settings.Default.EnabledAssemblers = new StringCollection();
            if (Settings.Default.EnabledMiners == null)
                Settings.Default.EnabledMiners = new StringCollection();
            if (Settings.Default.EnabledModules == null)
                Settings.Default.EnabledModules = new StringCollection();

            switch (Settings.Default.FactorioDifficulty) {
                case "normal":
                    DataCache.Current.Difficulty = "normal";
                    Difficulty = Difficulty.Normal;
                    break;
                case "expensive":
                    DataCache.Current.Difficulty = "expensive";
                    Difficulty = Difficulty.Expensive;
                    break;
                default:
                    Settings.Default.FactorioDifficulty = "normal";
                    Settings.Default.Save();
                    DataCache.Current.Difficulty = "normal";
                    Difficulty = Difficulty.Normal;
                    break;
            }

            ShowAssemblers = GraphViewModel.ShowAssemblers = Settings.Default.ShowAssemblers;
            ShowMiners = GraphViewModel.ShowMiners = Settings.Default.ShowMiners;

            await Task.Run(DataCache.Reload);

            Languages.AddRange(DataCache.Current.Languages);
            SelectedLanguage = DataCache.Current.Languages.FirstOrDefault(l => l.Name == Settings.Default.Language);

            UpdateControlValues();
        }

        private void LoadItemList()
        {
            unfilteredItemList = DataCache.Current.Items.Values.ToList();
            unfilteredItemList.StableSortBy(x => x.FriendlyName);

            ItemList.Clear();
            ItemList.AddRange(unfilteredItemList);
        }

        private void LoadRecipeList()
        {
            unfilteredRecipeList = DataCache.Current.Recipes.Values.ToList();
            unfilteredRecipeList.StableSortBy(x => x.FriendlyName);

            RecipeList.Clear();
            RecipeList.AddRange(unfilteredRecipeList);
        }

        private bool CanAddItems(UIElement source)
        {
            return SelectedItem != null;
        }

        private async Task AddItems(UIElement source)
        {
            foreach (Item item in new[] { SelectedItem }) {
                NodeElement newElement = null;

                var itemSupplyOption = new ItemChoice(item, "Create infinite supply node", item.FriendlyName);
                var itemOutputOption = new ItemChoice(item, "Create output node", item.FriendlyName);

                var optionList = new List<Choice>();
                optionList.Add(itemOutputOption);
                foreach (Recipe recipe in DataCache.Current.Recipes.Values.Where(r => r.Enabled)) {
                    if (recipe.Results.ContainsKey(item)) {
                        optionList.Add(new RecipeChoice(recipe,
                            string.Format("Create '{0}' recipe node", recipe.FriendlyName), recipe.FriendlyName));
                    }
                }
                optionList.Add(itemSupplyOption);

                foreach (Recipe recipe in DataCache.Current.Recipes.Values.Where(r => r.Enabled)) {
                    if (recipe.Ingredients.ContainsKey(item)) {
                        optionList.Add(new RecipeChoice(recipe,
                            string.Format("Create '{0}' recipe node", recipe.FriendlyName), recipe.FriendlyName));
                    }
                }

                var c = await optionList.ChooseAsync(source, PlacementMode.Right);
                if (c != null) {
                    if (c == itemSupplyOption) {
                        newElement = new NodeElement(SupplyNode.Create(item, GraphViewModel.Graph), GraphViewModel);
                    } else if (c is RecipeChoice rc) {
                        newElement = new NodeElement(
                            RecipeNode.Create(rc.Recipe, GraphViewModel.Graph), GraphViewModel);
                    } else if (c == itemOutputOption) {
                        newElement = new NodeElement(ConsumerNode.Create(item, GraphViewModel.Graph), GraphViewModel);
                    }

                    newElement.Update();
                    newElement.Position = GraphViewModel.ActualViewbox.GetCenter();

                    GraphViewModel.Elements.Add(newElement);
                }
            }

            GraphViewModel.Graph.UpdateNodeValues();
        }

        private void OnAmountTypeChanged()
        {
            GraphViewModel.Graph.SelectedAmountType = AmountType;
            GraphViewModel.Graph.UpdateNodeValues();
            GraphViewModel.UpdateNodes();
        }

        private void OnSelectedRateUnitChanged()
        {
            GraphViewModel.Graph.SelectedUnit = SelectedRateUnit;
            GraphViewModel.Graph.UpdateNodeValues();
            GraphViewModel.UpdateNodes();
        }

        private Task CompleteGraph()
        {
            GraphViewModel.Graph.LinkUpAllInputs();
            GraphViewModel.Graph.UpdateNodeValues();
            GraphViewModel.AddRemoveElements();
            GraphViewModel.PositionNodes();
            return Task.CompletedTask;
        }

        private Task ClearGraph()
        {
            GraphViewModel.Graph.Nodes.Clear();
            GraphViewModel.Elements.Clear();
            return Task.CompletedTask;
        }

        private void OnShowAssemblersChanged()
        {
            Settings.Default.ShowAssemblers = ShowAssemblers;
            Settings.Default.Save();
            GraphViewModel.ShowAssemblers = Settings.Default.ShowAssemblers;
            GraphViewModel.Graph.UpdateNodeValues();
        }

        private Task ExportImage()
        {
            var dialog = new ImageExportDialog(GraphViewModel);
            dialog.Show();
            return Task.CompletedTask;
        }

        private void OnShowMinersChanged()
        {
            Settings.Default.ShowMiners = ShowMiners;
            Settings.Default.Save();
            GraphViewModel.ShowMiners = Settings.Default.ShowMiners;
            GraphViewModel.Graph.UpdateNodeValues();
        }

        private void OnItemFilterTextChanged()
        {
            var items = unfilteredItemList
                .Where(i => i.FriendlyName.IndexOf(ItemFilterText, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(i => i.FriendlyName);
            ItemList.Clear();
            ItemList.AddRange(items);
        }

        private async Task ChangeFactorioDirectory()
        {
            var dialog = new DirectoryChooserDialog(Settings.Default.FactorioPath);
            dialog.Title = "Locate the Factorio directory";
            if (dialog.ShowDialog(view.Handle) == true) {
                Settings.Default.FactorioPath = dialog.SelectedPath;
                Settings.Default.Save();

                JObject savedGraph = JObject.Parse(JsonConvert.SerializeObject(GraphViewModel));
                await GraphViewModel.LoadFromJson(savedGraph);
                UpdateControlValues();
            }
        }

        private async Task ChangeModDirectory()
        {
            var dialog = new DirectoryChooserDialog(Settings.Default.FactorioModPath);
            dialog.Title = "Locate the mods directory";
            if (dialog.ShowDialog(view) == true) {
                Settings.Default.FactorioModPath = dialog.SelectedPath;
                Settings.Default.Save();

                JObject savedGraph = JObject.Parse(JsonConvert.SerializeObject(GraphViewModel));
                await GraphViewModel.LoadFromJson(savedGraph);
                UpdateControlValues();
            }
        }

        private async Task Reload()
        {
            await GraphViewModel.LoadFromJson(JObject.Parse(JsonConvert.SerializeObject(GraphViewModel)));
            UpdateControlValues();
        }

        private Task SaveGraph()
        {
            var dialog = new SaveFileDialog();
            dialog.DefaultExt = ".json";
            dialog.Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*";
            dialog.AddExtension = true;
            dialog.OverwritePrompt = true;
            dialog.FileName = "Flowchart.json";
            if (dialog.ShowDialog(view) != true)
                return Task.CompletedTask;

            var serialiser = JsonSerializer.Create();
            serialiser.Formatting = Formatting.Indented;
            var writer = new JsonTextWriter(new StreamWriter(dialog.FileName));
            try {
                serialiser.Serialize(writer, GraphViewModel);
            } catch (Exception exception) {
                MessageBox.Show("Could not save this file. See log for more details");
                ErrorLogging.LogLine(string.Format("Error saving file '{0}'. Error: '{1}'", dialog.FileName,
                    exception.Message));
            } finally {
                writer.Close();
            }

            return Task.CompletedTask;
        }

        private async Task LoadGraph()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*";
            dialog.CheckFileExists = true;
            if (dialog.ShowDialog(view) != true)
                return;

            try {
                await GraphViewModel.LoadFromJson(JObject.Parse(File.ReadAllText(dialog.FileName)));
            } catch (Exception exception) {
                MessageBox.Show("Could not load this file. See log for more details");
                ErrorLogging.LogLine(string.Format("Error loading file '{0}'. Error: '{1}'", dialog.FileName,
                    exception.Message));
            }

            UpdateControlValues();
        }

        private async Task EnableDisable()
        {
            var dialog = new EnableDisableItemsDialog();
            dialog.ShowDialog(view);
            SaveEnabledObjects();

            if (dialog.ModsChanged) {
                await GraphViewModel.LoadFromJson(JObject.Parse(JsonConvert.SerializeObject(GraphViewModel)));
                UpdateControlValues();
            }
        }

        private void SaveEnabledObjects()
        {
            Settings.Default.EnabledMods.Clear();
            Settings.Default.EnabledAssemblers.Clear();
            Settings.Default.EnabledMiners.Clear();
            Settings.Default.EnabledModules.Clear();

            Settings.Default.EnabledMods.AddRange(DataCache.Current.Mods
                .Select(m => m.Name + "|" + m.Enabled.ToString()).ToArray());
            Settings.Default.EnabledAssemblers.AddRange(DataCache.Current.Assemblers.Values
                .Select(a => a.Name + "|" + a.Enabled.ToString()).ToArray());
            Settings.Default.EnabledMiners.AddRange(DataCache.Current.Miners.Values
                .Select(m => m.Name + "|" + m.Enabled.ToString()).ToArray());
            Settings.Default.EnabledModules.AddRange(DataCache.Current.Modules.Values
                .Select(m => m.Name + "|" + m.Enabled.ToString()).ToArray());

            Settings.Default.Save();
        }

        private void OnLanguageChanged()
        {
            string newLocale = SelectedLanguage.Name;
            Task.Run(async () => await DataCache.Current.ChangeLocaleAsync(newLocale));

            GraphViewModel.UpdateNodes();
            UpdateControlValues();

            Settings.Default.Language = newLocale;
            Settings.Default.Save();
        }

        private void UpdateControlValues()
        {
            LoadItemList();
            LoadRecipeList();
        }

        private Task ArrangeNodes()
        {
            GraphViewModel.PositionNodes();
            return Task.CompletedTask;
        }

        private void OnRecipeFilterTextChanged()
        {
            var recipes = unfilteredRecipeList
                .Where(i => i.FriendlyName.IndexOf(RecipeFilterText, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(i => i.FriendlyName);

            RecipeList.Clear();
            RecipeList.AddRange(recipes);
        }

        private bool CanAddRecipes()
        {
            return SelectedRecipes.Count > 0;
        }

        private Task AddRecipes()
        {
            foreach (Recipe recipe in SelectedRecipes) {
                Point position = GraphViewModel.ActualViewbox.GetCenter();

                var newElement = new NodeElement(
                    RecipeNode.Create(recipe, GraphViewModel.Graph), GraphViewModel);
                newElement.Update();
                newElement.Position = position;
                GraphViewModel.Elements.Add(newElement);
            }

            GraphViewModel.Graph.UpdateNodeValues();
            return Task.CompletedTask;
        }

        private async Task DifficultyChanged()
        {
            var currentDifficulty = DataCache.Current.Difficulty;

            if (Difficulty == Difficulty.Normal)
                DataCache.Current.Difficulty = "normal";
            else if (Difficulty == Difficulty.Expensive)
                DataCache.Current.Difficulty = "expensive";

            if (currentDifficulty == DataCache.Current.Difficulty)
                return;

            Settings.Default.FactorioDifficulty = DataCache.Current.Difficulty;
            Settings.Default.Save();

            JObject savedGraph = JObject.Parse(JsonConvert.SerializeObject(GraphViewModel));
            await GraphViewModel.LoadFromJson(savedGraph);
            UpdateControlValues();
        }
    }
}