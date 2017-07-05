namespace Foreman
{
    using System;
    using System.Windows.Forms;

    public partial class ItemChooserControl : ChooserControl
    {
        public Item DisplayedItem { get; }

        public ItemChooserControl(Item item, string text, string filterText) : base(text, filterText)
        {
            InitializeComponent();

            DisplayedItem = item;
            TextLabel.Text = text;
        }

        private void RecipeChooserSupplyNodeOption_Load(object sender, EventArgs e)
        {
            iconPictureBox.Image = DisplayedItem != null ? DisplayedItem.Icon : null;
            iconPictureBox.SizeMode = PictureBoxSizeMode.CenterImage;

            RegisterMouseEvents(this);
        }
    }
}
