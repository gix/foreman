namespace Foreman
{
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Threading;
    using Controls;
    using Extensions;
    using Foreman.Properties;
    using PresentationTheme.Aero;
    using Views;

    public partial class App
    {
        private MainWindowViewModel mainWindowViewModel;

        protected override void OnStartup(StartupEventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture;

            base.OnStartup(e);

            ThemeManager.Install();
            AeroTheme.SetAsCurrentTheme();
            PopupEx.InstallHook();

            var window = new MainWindow();
            var viewModel = new MainWindowViewModel(window);
            mainWindowViewModel = viewModel;

            window.DataContext = viewModel;
            MainWindow = window;
            window.Show();

            viewModel.Load().Forget();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Settings.Default.RecentGraphs.Clear();
            foreach (var path in mainWindowViewModel.RecentGraphs)
                Settings.Default.RecentGraphs.Add(path);
            Settings.Default.Save();

            base.OnExit(e);
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
