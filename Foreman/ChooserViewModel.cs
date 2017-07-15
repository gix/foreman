namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;

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

        public static Task<Choice> ChooseAsync(IReadOnlyList<Choice> choices, Point location)
        {
            var model = new ChooserViewModel(choices);
            var tcs = new TaskCompletionSource<Choice>();
            model.Show(location, c => tcs.SetResult(c));
            return tcs.Task;
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
        protected Choice(string displayText, string filterText)
        {
            DisplayText = displayText;
            FilterText = filterText;
        }

        public string DisplayText { get; }
        public string FilterText { get; }
    }

    public class ItemChoice : Choice
    {
        public ItemChoice(Item item, string displayText, string filterText)
            : base(displayText, filterText)
        {
            Item = item;
        }

        public Item Item { get; }
    }

    public class RecipeChoice : Choice
    {
        public RecipeChoice(Recipe recipe, string text, string filterText)
            : base(string.Format(text, recipe.FriendlyName), filterText)
        {
            Recipe = recipe;
            Inputs = Recipe.Ingredients.Keys.Select(
                x => $"{x.FriendlyName} ({Recipe.Ingredients[x]})").ToList();
            Outputs = Recipe.Results.Keys.Select(
                x => $"{x.FriendlyName} ({Recipe.Results[x]})").ToList();
        }

        public Recipe Recipe { get; }
        public IReadOnlyList<string> Inputs { get; }
        public IReadOnlyList<string> Outputs { get; }
    }
}
