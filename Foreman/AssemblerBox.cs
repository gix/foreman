namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    public class AssemblerBox : GraphElement
    {
        public Dictionary<MachinePermutation, double> AssemblerList;

        public override Point Size
        {
            get
            {
                int leftColumnWidth = 0;
                int rightColumnWidth = 0;

                List<AssemblerIconElement> iconList = SubElements.OfType<AssemblerIconElement>().ToList();

                for (int i = 0; i < iconList.Count; i += 2) {
                    leftColumnWidth = Math.Max(iconList[i].Width, leftColumnWidth);
                }
                for (int i = 1; i < iconList.Count; i += 2) {
                    rightColumnWidth = Math.Max(iconList[i].Width, rightColumnWidth);
                }

                return new Point(leftColumnWidth + rightColumnWidth,
                    ((int)Math.Ceiling(AssemblerList.Count / 2f) * AssemblerIconElement.IconSize));
            }
        }

        public AssemblerBox(ProductionGraphViewer parent)
            : base(parent)
        {
            AssemblerList = new Dictionary<MachinePermutation, double>();
        }

        public void Update()
        {
            foreach (AssemblerIconElement element in SubElements.OfType<AssemblerIconElement>().ToList()) {
                if (!AssemblerList.Keys.Contains(element.DisplayedMachine)) {
                    SubElements.Remove(element);
                }
            }

            foreach (var kvp in AssemblerList) {
                if (!SubElements.OfType<AssemblerIconElement>().Any(aie => aie.DisplayedMachine == kvp.Key)) {
                    SubElements.Add(new AssemblerIconElement(kvp.Key, kvp.Value, Parent));
                }
            }

            int y = (int)(Height / Math.Ceiling(AssemblerList.Count / 2d));
            int widthOver2 = Width / 2;

            int i = 0;
            foreach (AssemblerIconElement element in SubElements.OfType<AssemblerIconElement>()) {
                element.DisplayedNumber = AssemblerList[element.DisplayedMachine];

                if (i % 2 == 0) {
                    element.X = widthOver2 - element.Width;
                } else {
                    element.X = widthOver2;
                }
                element.Y = (int)Math.Floor(i / 2d) * y;

                if (AssemblerList.Count == 1) {
                    element.X = (Width - element.Width) / 2;
                } else if (i == AssemblerList.Count - 1 && AssemblerList.Count % 2 != 0) {
                    element.X = widthOver2 - (element.Width / 2);
                }

                i++;
            }
        }

        public override void Paint(Graphics graphics)
        {
            base.Paint(graphics);
        }
    }

    public class AssemblerIconElement : GraphElement
    {
        private const int MaxFontSize = 14;
        private double displayedNumber;

        public MachinePermutation DisplayedMachine { get; set; }

        public double DisplayedNumber
        {
            get => displayedNumber;
            set
            {
                displayedNumber = value;
                using (Graphics graphics = Parent.CreateGraphics()) {
                    if (DisplayedNumber > 0) {
                        stringWidth = graphics.MeasureString(DisplayedNumber.ToString(DisplayNumberFormat), font).Width;
                    } else {
                        stringWidth = 0;
                    }
                    UpdateSize();
                }
            }
        }

        private float stringWidth;
        private readonly StringFormat centreFormat = new StringFormat();
        public const int IconSize = 32;
        private readonly Font font = new Font(FontFamily.GenericSansSerif, MaxFontSize);
        private const string DisplayNumberFormat = "F2";

        public AssemblerIconElement(MachinePermutation assembler, double number, ProductionGraphViewer parent)
            : base(parent)
        {
            DisplayedMachine = assembler;
            DisplayedNumber = number;
            centreFormat.Alignment = centreFormat.LineAlignment = StringAlignment.Center;
        }

        private void UpdateSize()
        {
            Width = (int)stringWidth + IconSize;
            Height = IconSize;
        }

        public override void Paint(Graphics graphics)
        {
            Point iconPoint = new Point((int)((Width + IconSize + stringWidth) / 2 - IconSize),
                (Height - IconSize) / 2);

            graphics.DrawImage(DisplayedMachine.Assembler.Icon, iconPoint.X, iconPoint.Y, IconSize, IconSize);
            if (DisplayedNumber > 0) {
                graphics.DrawString(DisplayedNumber.ToString(DisplayNumberFormat), font, Brushes.Black,
                    new Point((int)((Width - IconSize - stringWidth) / 2 + stringWidth / 2), Height / 2), centreFormat);
            }

            if (DisplayedMachine.Modules.Any()) {
                int moduleCount = DisplayedMachine.Modules.Count;
                int numModuleRows = (int)Math.Ceiling(moduleCount / 2d);
                int moduleSize = Math.Min(IconSize / numModuleRows, IconSize / (2 - moduleCount % 2)) - 2;

                int i = 0;
                int x;

                if (moduleCount == 1) {
                    x = iconPoint.X + (IconSize - moduleSize) / 2;
                } else {
                    x = iconPoint.X + (IconSize - moduleSize - moduleSize) / 2;
                }
                int y = iconPoint.Y + (IconSize - (moduleSize * numModuleRows)) / 2;
                for (int r = 0; r < numModuleRows; r++) {
                    graphics.DrawImage(DisplayedMachine.Modules[i].Icon, x, y + (r * moduleSize), moduleSize,
                        moduleSize);
                    i++;
                    if (i < DisplayedMachine.Modules.Count && DisplayedMachine.Modules[i] != null) {
                        graphics.DrawImage(DisplayedMachine.Modules[i].Icon, x + moduleSize, y + (r * moduleSize),
                            moduleSize, moduleSize);
                        i++;
                    }
                }
            }
        }
    }
}
