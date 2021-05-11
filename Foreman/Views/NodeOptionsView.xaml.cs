namespace Foreman.Views
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    public partial class NodeOptionsView
    {
        private NodeOptionsViewModel viewModel = null!;

        public NodeOptionsView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(
            object sender, DependencyPropertyChangedEventArgs args)
        {
            viewModel = (NodeOptionsViewModel)args.NewValue;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Enter)
                viewModel.Graph.UpdateNodeValues();
        }

        private async void OnAssemblerButtonClicked(object sender, EventArgs e)
        {
            await viewModel.ChooseAssembler(assemblerButton);
        }

        private async void OnModulesButtonClicked(object sender, EventArgs e)
        {
            await viewModel.ChooseModuleSelector(modulesButton);
        }

        private async void OnModuleSlotClicked(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var slot = (ModuleSlot)button.DataContext;
            await viewModel.ChooseModule(slot, button);
        }
    }
}
