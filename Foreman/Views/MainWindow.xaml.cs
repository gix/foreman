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

    public class KeyDownEventTrigger : Microsoft.Xaml.Behaviors.EventTrigger
    {
        public KeyDownEventTrigger()
            : base("KeyDown")
        {
        }

        public Key Key { get; set; }

        protected override void OnEvent(EventArgs eventArgs)
        {
            if (eventArgs is KeyEventArgs e && e.Key == Key)
                InvokeActions(eventArgs);
        }
    }

    public partial class MainWindow : IMainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void FilterTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (ItemListView.Items.Count == 0) {
                return;
            }
            //int currentSelection;
            //if (ItemListView.SelectedIndices.Count == 0) {
            //    currentSelection = -1;
            //} else {
            //    currentSelection = ItemListView.SelectedIndices[0];
            //}
            if (e.Key == Key.Down) {
                //int newSelection = currentSelection + 1;
                //if (newSelection >= ItemListView.Items.Count) newSelection = ItemListView.Items.Count - 1;
                //if (newSelection <= 0) newSelection = 0;
                //ItemListView.SelectedIndices.Clear();
                //ItemListView.SelectedIndices.Add(newSelection);
                e.Handled = true;
            } else if (e.Key == Key.Up) {
                //int newSelection = currentSelection - 1;
                //if (newSelection == -1) newSelection = 0;
                //ItemListView.SelectedIndices.Clear();
                //ItemListView.SelectedIndices.Add(newSelection);
                e.Handled = true;
            } else if (e.Key == Key.Enter) {
                AddItemButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }

        private void RecipeFilterTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            //if (RecipeListView.Items.Count == 0)
            //    return;

            //int currentSelection;
            //if (RecipeListView.SelectedIndices.Count == 0) {
            //    currentSelection = -1;
            //} else {
            //    currentSelection = RecipeListView.SelectedIndices[0];
            //}
            if (e.Key == Key.Down) {
                RecipeListView.SelectedIndex += 1;
                //    int newSelection = currentSelection + 1;
                //    if (newSelection >= RecipeListView.Items.Count) newSelection = RecipeListView.Items.Count - 1;
                //    if (newSelection <= 0) newSelection = 0;
                //    RecipeListView.SelectedIndices.Clear();
                //    RecipeListView.SelectedIndices.Add(newSelection);
                e.Handled = true;
            } else if (e.Key == Key.Up) {
                RecipeListView.SelectedIndex -= 1;
                //    int newSelection = currentSelection - 1;
                //    if (newSelection == -1) newSelection = 0;
                //    RecipeListView.SelectedIndices.Clear();
                //    RecipeListView.SelectedIndices.Add(newSelection);
                e.Handled = true;
            } else if (e.Key == Key.Enter) {
                AddRecipeButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }

        private void ItemListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            AddItemButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
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
