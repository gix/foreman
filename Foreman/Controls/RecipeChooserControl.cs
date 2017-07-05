namespace Foreman
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    public partial class RecipeChooserControl : ChooserControl
    {
        public Recipe DisplayedRecipe { get; }
        private readonly Bitmap colorIcon;
        private readonly Bitmap grayIcon;

        public RecipeChooserControl(Recipe recipe, string text, string filterText) : base(text, filterText)
        {
            InitializeComponent();
            DisplayedRecipe = recipe;

            colorIcon = recipe.Icon;
            grayIcon = GraphicsStuff.MakeMonochrome(recipe.Icon);

            setClickHandler(RecipeChooserControl_MouseUp, this);
        }

        private static void setClickHandler(MouseEventHandler h, Control c)
        {
            c.MouseUp += h;
            foreach (Control child in c.Controls) {
                setClickHandler(h, child);
            }
        }


        private void RecipeChooserOption_Load(object sender, EventArgs e)
        {
            nameLabel.Text = string.Format(DisplayText, DisplayedRecipe.FriendlyName);
            foreach (Item ingredient in DisplayedRecipe.Ingredients.Keys) {
                inputListBox.Items.Add(string.Format("{0} ({1})", ingredient.FriendlyName,
                    DisplayedRecipe.Ingredients[ingredient]));
            }
            foreach (Item result in DisplayedRecipe.Results.Keys) {
                outputListBox.Items.Add(
                    string.Format("{0} ({1})", result.FriendlyName, DisplayedRecipe.Results[result]));
            }
            iconPictureBox.Image = DisplayedRecipe.Icon;
            iconPictureBox.SizeMode = PictureBoxSizeMode.CenterImage;

            FakeDisable(DisplayedRecipe.Enabled);

            RegisterMouseEvents(this);
        }


        private void RecipeChooserControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) {
                DisplayedRecipe.Enabled = !DisplayedRecipe.Enabled;
                FakeDisable(DisplayedRecipe.Enabled);
            }
        }

        // when Enabled is false, change appearance to that of a disabled control, without actually disabling.
        private void FakeDisable(bool enabled)
        {
            Color newListboxColor = (enabled ? SystemColors.WindowText : SystemColors.GrayText);
            Color newTextboxColor = (enabled ? SystemColors.WindowText : SystemColors.GrayText);

            nameLabel.ForeColor = newTextboxColor;
            inputListBox.ForeColor = newListboxColor;
            outputListBox.ForeColor = newListboxColor;

            iconPictureBox.Image = (enabled ? colorIcon : grayIcon);

            Invalidate();
        }
    }
}
