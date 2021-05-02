namespace Foreman.Views
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Interop;

    public interface IMainWindow : IWin32Window
    {
        void Close();
    }

    public partial class MainWindow : IMainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void FilterTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (ItemListView.Items.Count == 0)
                return;

            if (e.Key == Key.Down) {
                ItemListView.SelectedIndex = Math.Min(
                    ItemListView.SelectedIndex + 1, ItemListView.Items.Count);
                e.Handled = true;
            } else if (e.Key == Key.Up) {
                ItemListView.SelectedIndex = Math.Max(
                    ItemListView.SelectedIndex - 1, 0);
                e.Handled = true;
            } else if (e.Key == Key.Enter) {
                AddItemButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, this));
            }
        }

        private void RecipeFilterTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (RecipeListView.Items.Count == 0)
                return;

            if (e.Key == Key.Down) {
                RecipeListView.SelectedIndex = Math.Min(
                    RecipeListView.SelectedIndex + 1, RecipeListView.Items.Count);
                e.Handled = true;
            } else if (e.Key == Key.Up) {
                RecipeListView.SelectedIndex = Math.Max(
                    RecipeListView.SelectedIndex - 1, 0);
                e.Handled = true;
            } else if (e.Key == Key.Enter) {
                AddRecipeButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, this));
            }
        }

        private void ItemListViewItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) {
                var draggedItems = new HashSet<Item>();
                foreach (Item item in ItemListView.SelectedItems)
                    draggedItems.Add(item);
                DragDrop.DoDragDrop(ItemListView, draggedItems, DragDropEffects.Copy);
            }
        }

        private void RecipeListViewItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) {
                var draggedRecipes = new HashSet<Recipe>();
                foreach (Recipe recipe in RecipeListView.SelectedItems)
                    draggedRecipes.Add(recipe);
                DragDrop.DoDragDrop(RecipeListView, draggedRecipes, DragDropEffects.Copy);
            }
        }

        IntPtr IWin32Window.Handle => new WindowInteropHelper(this).EnsureHandle();
    }
}
