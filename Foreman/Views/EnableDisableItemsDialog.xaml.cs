﻿namespace Foreman.Views
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

            Assemblers.AddRange(DataCache.Assemblers.Values.OrderBy(x => x.FriendlyName));
            Miners.AddRange(DataCache.Miners.Values.OrderBy(x => x.FriendlyName));
            Modules.AddRange(DataCache.Modules.Values.OrderBy(x => x.FriendlyName));
            Mods.AddRange(DataCache.Mods.OrderBy(x => x.Name));
        }

        public bool ModsChanged { get; private set; }
        public ObservableCollection<Assembler> Assemblers { get; } = new ObservableCollection<Assembler>();
        public ObservableCollection<Mod> Mods { get; } = new ObservableCollection<Mod>();
        public ObservableCollection<Miner> Miners { get; } = new ObservableCollection<Miner>();
        public ObservableCollection<Module> Modules { get; } = new ObservableCollection<Module>();

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