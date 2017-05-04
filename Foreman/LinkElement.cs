using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Foreman
{
	public class LinkElement : GraphElement
	{
		private const float PenThickness = 3f;
		private GraphicsPath currentPath;

		public NodeLink DisplayedLink { get; private set; }
		public ProductionNode Supplier { get { return DisplayedLink.Supplier; } }
		public ProductionNode Consumer { get { return DisplayedLink.Consumer; } }
		public NodeElement SupplierElement { get { return Parent.GetElementForNode(Supplier); } }
		public NodeElement ConsumerElement { get { return Parent.GetElementForNode(Consumer); } }
		public Item Item { get { return DisplayedLink.Item; } }

		public override void Dispose()
		{
			base.Dispose();
			currentPath?.Dispose();
		}

		public override Point Location
		{
			get { return new Point(); }
			set { }
		}
		public override int X
		{
			get { return 0; }
			set { }
		}
		public override int Y
		{
			get { return 0; }
			set { }
		}
		public override Point Size
		{
			get { return new Point(); }
			set { }
		}
		public override int Width
		{
			get { return 0; }
			set { }
		}
		public override int Height
		{
			get { return 0; }
			set { }
		}

		public LinkElement(ProductionGraphViewer parent, NodeLink displayedLink)
			: base(parent)
		{
			DisplayedLink = displayedLink;
		}

		public override bool ContainsPoint(Point point)
		{
			if (currentPath == null)
				return false;

			using (Pen pen = new Pen(Brushes.Black, PenThickness + 10))
				return currentPath.IsOutlineVisible(point, pen);
		}

		public override void Paint(Graphics graphics)
		{
			Point pointN = SupplierElement.GetOutputLineConnectionPoint(Item);
			Point pointM = ConsumerElement.GetInputLineConnectionPoint(Item);
			Point pointN2 = new Point(pointN.X, pointN.Y - Math.Max((int)((pointN.Y - pointM.Y) / 2), 40));
			Point pointM2 = new Point(pointM.X, pointM.Y + Math.Max((int)((pointN.Y - pointM.Y) / 2), 40));

			using (Pen pen = new Pen(DataCache.IconAverageColour(Item.Icon), PenThickness))
			{
				currentPath?.Dispose();
				currentPath = new GraphicsPath();
				currentPath.AddBezier(pointN, pointN2, pointM2, pointM);
				graphics.DrawPath(pen, currentPath);
			}
		}

		public override void MouseUp(Point location, MouseButtons button)
		{
			base.MouseUp(location, button);

			if (button == MouseButtons.Right)
			{
				var menu = new ContextMenu();
				menu.MenuItems.Add(new MenuItem("Delete link", (o, e) => Parent.DeleteLink(this)));
				menu.Show(Parent, Parent.GraphToScreen(Point.Add(location, new Size(X, Y))));
			}
		}
	}
}
