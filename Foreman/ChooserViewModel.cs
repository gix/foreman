namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls.Primitives;

    public static class Chooser
    {
        public static Task<Choice> ChooseAsync(
            this IReadOnlyList<Choice> choices, Point screenPoint)
        {
            var model = new ChooserViewModel(choices);
            var tcs = new TaskCompletionSource<Choice>();
            model.Show(screenPoint, c => tcs.SetResult(c));
            return tcs.Task;
        }

        public static Task<Choice> ChooseAsync(
            this IReadOnlyList<Choice> choices, UIElement placementTarget,
            PlacementMode placementMode)
        {
            var model = new ChooserViewModel(choices);
            var tcs = new TaskCompletionSource<Choice>();
            model.Show(placementTarget, placementMode, c => tcs.SetResult(c));
            return tcs.Task;
        }
    }

    public class ChooserViewModel : ViewModel
    {
        private readonly IReadOnlyList<Choice> allChoices;
        private readonly ObservableCollection<Choice> filteredChoices;
        private Choice selectedChoice;
        private Action<Choice> callbackMethod;
        private Popup popup;

        public ChooserViewModel(IReadOnlyList<Choice> choices)
        {
            allChoices = choices;
            filteredChoices = new ObservableCollection<Choice>(choices);
        }

        private string filterText;

        public string FilterText
        {
            get => filterText;
            set
            {
                if (SetProperty(ref filterText, value)) {
                    filteredChoices.Clear();
                    foreach (var choice in allChoices) {
                        if (choice.FilterText.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                            filteredChoices.Add(choice);
                    }
                }
            }
        }

        public IReadOnlyList<Choice> Choices => filteredChoices;

        public Choice SelectedChoice
        {
            get => selectedChoice;
            set => SetProperty(ref selectedChoice, value);
        }

        public void Show(UIElement placementTarget, PlacementMode placementMode, Action<Choice> callback)
        {
            callbackMethod = callback;

            popup = CreatePopup();
            popup.Placement = placementMode;
            popup.PlacementTarget = placementTarget;
            popup.IsOpen = true;
        }

        public void Show(Point location, Action<Choice> callback)
        {
            callbackMethod = callback;

            popup = CreatePopup();
            popup.Placement = PlacementMode.Absolute;
            popup.HorizontalOffset = location.X;
            popup.VerticalOffset = location.Y;
            popup.IsOpen = true;
        }

        public void Hide()
        {
            if (popup != null) {
                popup.Closed -= OnPopupClosed;
                popup.IsOpen = false;
                popup = null;
            }
        }

        private void Cancel()
        {
            callbackMethod(null);
            Hide();
        }

        private Popup CreatePopup()
        {
            var popup = PopupUtils.CreatePopup(this);
            popup.Closed += OnPopupClosed;
            ((FrameworkElement)popup.Child).SizeChanged += (s, e) => {
                var fe = (FrameworkElement)s;
                fe.MinWidth = Math.Max(fe.MinWidth, fe.ActualWidth);
            };
            return popup;
        }

        private void OnPopupClosed(object sender, EventArgs e)
        {
            Cancel();
        }

        public void OnItemClicked(Choice choice)
        {
            callbackMethod(choice);
            Hide();
        }
    }

    public abstract class Choice : ViewModel
    {
        protected Choice(string displayText, string filterText, object value)
        {
            DisplayText = displayText;
            FilterText = filterText ?? displayText;
            Value = value;
        }

        public string DisplayText { get; }
        public string FilterText { get; }
        public object Value { get; }
    }

    public class ItemChoice : Choice
    {
        public ItemChoice(Item item, string displayText, string filterText = null, object value = null)
            : base(displayText, filterText, value)
        {
            Item = item;
        }

        public Item Item { get; }
    }

    public class RecipeChoice : Choice
    {
        public RecipeChoice(Recipe recipe, string text, string filterText = null, object value = null)
            : base(string.Format(text, recipe.FriendlyName), filterText, value)
        {
            Recipe = recipe;
            Inputs = Recipe.Ingredients.Keys.Select(
                x => new ItemQuantity(x, Recipe.Ingredients[x])).ToList();
            Outputs = Recipe.Results.Keys.Select(
                x => new ItemQuantity(x, Recipe.Results[x])).ToList();
        }

        public Recipe Recipe { get; }
        public IReadOnlyList<ItemQuantity> Inputs { get; }
        public IReadOnlyList<ItemQuantity> Outputs { get; }
    }

    public class ItemQuantity
    {
        public ItemQuantity(Item item, float quantity)
        {
            Item = item;
            Quantity = quantity;
        }

        public Item Item { get; }
        public float Quantity { get; }
    }
}
