namespace Foreman.Views
{
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using Extensions;

    public partial class EnableDisableItemsDialog
    {
        public EnableDisableItemsDialog()
        {
            InitializeComponent();
            DataContext = this;

            Assemblers.AddRange(DataCache.Current.Assemblers.Values.OrderBy(x => x.FriendlyName));
            Miners.AddRange(DataCache.Current.Miners.Values.OrderBy(x => x.FriendlyName));
            Modules.AddRange(DataCache.Current.Modules.Values.OrderBy(x => x.FriendlyName));
            Mods.AddRange(DataCache.Current.Mods.OrderBy(x => x.Name));
        }

        public bool ModsChanged { get; private set; }
        public ObservableCollection<Assembler> Assemblers { get; } = new();
        public ObservableCollection<Mod> Mods { get; } = new();
        public ObservableCollection<Miner> Miners { get; } = new();
        public ObservableCollection<Module> Modules { get; } = new();

        private void OnModChecked(object sender, RoutedEventArgs args)
        {
            var checkBox = (CheckBox)sender;
            var mod = (Mod)checkBox.DataContext;
            if (!mod.Enabled) {
                for (int i = 0; i < Mods.Count; ++i) {
                    if (Mods[i].DependsOn(mod, true)) {
                        Mods[i].Enabled = false;
                        Mods[i] = Mods[i];
                    }
                }
            } else {
                for (int i = 0; i < Mods.Count; ++i) {
                    if (mod.DependsOn(Mods[i], true)) {
                        Mods[i].Enabled = true;
                        Mods[i] = Mods[i];
                    }
                }
            }

            ModsChanged = true;
        }
    }
}
