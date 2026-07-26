using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MVMediaStudio.UI
{
    internal sealed class AdaptiveGrid : Panel
    {
        public AdaptiveGrid()
        {
            ItemMinWidth = 220;
            ColumnSpacing = 12;
            RowSpacing = 12;
            MaximumColumns = 4;
        }

        public double ItemMinWidth { get; set; }
        public double ColumnSpacing { get; set; }
        public double RowSpacing { get; set; }
        public int MaximumColumns { get; set; }

        protected override Size MeasureOverride(Size availableSize)
        {
            double width = NormalizeWidth(availableSize.Width);
            int columns = ResolveColumns(width);
            double itemWidth = ResolveItemWidth(width, columns);
            List<double> rowHeights = new List<double>();

            int visibleIndex = 0;
            for (int index = 0; index < InternalChildren.Count; index++)
            {
                UIElement child = InternalChildren[index];
                if (child.Visibility == Visibility.Collapsed)
                {
                    child.Measure(new Size(0, 0));
                    continue;
                }
                child.Measure(new Size(itemWidth, double.PositiveInfinity));
                int row = visibleIndex / columns;
                while (rowHeights.Count <= row)
                    rowHeights.Add(0);
                rowHeights[row] = Math.Max(rowHeights[row], child.DesiredSize.Height);
                visibleIndex++;
            }

            double height = Sum(rowHeights) + Math.Max(0, rowHeights.Count - 1) * RowSpacing;
            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double width = Math.Max(0, finalSize.Width);
            int columns = ResolveColumns(width);
            double itemWidth = ResolveItemWidth(width, columns);
            List<double> rowHeights = RowHeights(columns);
            double y = 0;

            int visibleIndex = 0;
            for (int index = 0; index < InternalChildren.Count; index++)
            {
                UIElement child = InternalChildren[index];
                if (child.Visibility == Visibility.Collapsed)
                {
                    child.Arrange(new Rect(0, 0, 0, 0));
                    continue;
                }
                int column = visibleIndex % columns;
                int row = visibleIndex / columns;
                double x = column * (itemWidth + ColumnSpacing);
                child.Arrange(new Rect(x, y, itemWidth, rowHeights[row]));
                visibleIndex++;
                if (column == columns - 1 || visibleIndex == VisibleChildCount())
                    y += rowHeights[row] + RowSpacing;
            }
            return finalSize;
        }

        private int ResolveColumns(double width)
        {
            int maximum = Math.Max(1, Math.Min(MaximumColumns, Math.Max(1, VisibleChildCount())));
            if (width <= 0 || double.IsInfinity(width))
                return maximum;
            int columns = (int)Math.Floor((width + ColumnSpacing) / (Math.Max(1, ItemMinWidth) + ColumnSpacing));
            return Math.Max(1, Math.Min(maximum, columns));
        }

        private double ResolveItemWidth(double width, int columns)
        {
            if (double.IsInfinity(width) || width <= 0)
                return Math.Max(1, ItemMinWidth);
            return Math.Max(1, (width - Math.Max(0, columns - 1) * ColumnSpacing) / columns);
        }

        private List<double> RowHeights(int columns)
        {
            List<double> heights = new List<double>();
            int visibleIndex = 0;
            for (int index = 0; index < InternalChildren.Count; index++)
            {
                if (InternalChildren[index].Visibility == Visibility.Collapsed)
                    continue;
                int row = visibleIndex / columns;
                while (heights.Count <= row)
                    heights.Add(0);
                heights[row] = Math.Max(heights[row], InternalChildren[index].DesiredSize.Height);
                visibleIndex++;
            }
            return heights;
        }

        private double NormalizeWidth(double width)
        {
            if (!double.IsInfinity(width) && !double.IsNaN(width))
                return Math.Max(0, width);

            double desired = 0;
            foreach (UIElement child in InternalChildren)
            {
                if (child.Visibility == Visibility.Collapsed)
                    continue;
                child.Measure(new Size(Math.Max(1, ItemMinWidth), double.PositiveInfinity));
                desired = Math.Max(desired, child.DesiredSize.Width);
            }
            int columns = Math.Max(1, Math.Min(MaximumColumns, VisibleChildCount()));
            return columns * Math.Max(ItemMinWidth, desired) + Math.Max(0, columns - 1) * ColumnSpacing;
        }

        private int VisibleChildCount()
        {
            int count = 0;
            foreach (UIElement child in InternalChildren)
            {
                if (child.Visibility != Visibility.Collapsed)
                    count++;
            }
            return count;
        }

        private static double Sum(IEnumerable<double> values)
        {
            double total = 0;
            foreach (double value in values)
                total += value;
            return total;
        }
    }
}
