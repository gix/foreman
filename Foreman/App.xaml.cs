﻿namespace Foreman
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Threading;
    using Controls;
    using Extensions;
    using PresentationTheme.Aero;
    using Views;

    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ThemeManager.Install();
            AeroTheme.SetAsCurrentTheme();
            PopupEx.InstallHook();

            var window = new MainWindow();
            var viewModel = new MainWindowViewModel(window);
            window.DataContext = viewModel;
            MainWindow = window;
            window.Show();

            viewModel.Load().Forget();
        }

        private void OnChooserChoiceMouseUp(object sender, MouseButtonEventArgs e)
        {
            var container = (FrameworkElement)sender;
            var itemsControl = ItemsControl.ItemsControlFromItemContainer(container);
            if (itemsControl.DataContext is ChooserViewModel chooser &&
                container.DataContext is Choice choice) {
                container.Dispatcher.InvokeAsync(
                    () => chooser.OnItemClicked(choice), DispatcherPriority.Render);
            }
        }
    }
}
