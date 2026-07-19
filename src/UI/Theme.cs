using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace MVMediaStudio.UI
{
    internal static class Theme
    {
        public const string WindowBackground = "WindowBackground";
        public const string Surface = "Surface";
        public const string SurfaceAlt = "SurfaceAlt";
        public const string Input = "Input";
        public const string Border = "Border";
        public const string Text = "Text";
        public const string Muted = "Muted";
        public const string Primary = "Primary";
        public const string PrimaryHover = "PrimaryHover";
        public const string Success = "Success";
        public const string Warning = "Warning";
        public const string Danger = "Danger";
        public const string Console = "Console";
        public const string ConsoleText = "ConsoleText";

        public static void Apply(Window window, bool dark)
        {
            ResourceDictionary resources = window.Resources;
            resources[WindowBackground] = Brush(dark ? "#101418" : "#F4F6F8");
            resources[Surface] = Brush(dark ? "#181E24" : "#FFFFFF");
            resources[SurfaceAlt] = Brush(dark ? "#20272E" : "#EDF1F4");
            resources[Input] = Brush(dark ? "#12171C" : "#F8FAFB");
            resources[Border] = Brush(dark ? "#313A43" : "#DCE2E7");
            resources[Text] = Brush(dark ? "#F3F6F8" : "#17212B");
            resources[Muted] = Brush(dark ? "#9DAAB6" : "#60707E");
            resources[Primary] = Brush(dark ? "#20A4F3" : "#087FCE");
            resources[PrimaryHover] = Brush(dark ? "#44B7F6" : "#006EB6");
            resources[Success] = Brush(dark ? "#49D49D" : "#16845D");
            resources[Warning] = Brush(dark ? "#F7C66B" : "#A96200");
            resources[Danger] = Brush(dark ? "#FF7B86" : "#C43B48");
            resources[Console] = Brush(dark ? "#0B0F12" : "#192229");
            resources[ConsoleText] = Brush("#B8F5D1");

            resources[typeof(Button)] = CreateButtonStyle(window);
            resources[typeof(TextBox)] = CreateTextBoxStyle(window);
            resources[typeof(ComboBox)] = CreateComboBoxStyle(window);
            resources[typeof(CheckBox)] = CreateCheckBoxStyle(window);
            resources[typeof(ProgressBar)] = CreateProgressStyle(window);
        }

        public static void Bind(FrameworkElement element, DependencyProperty property, string key)
        {
            element.SetResourceReference(property, key);
        }

        private static Style CreateButtonStyle(Window window)
        {
            Style style = new Style(typeof(Button));
            style.Setters.Add(new Setter(Control.ForegroundProperty, window.FindResource(Text)));
            style.Setters.Add(new Setter(Control.BackgroundProperty, window.FindResource(SurfaceAlt)));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, window.FindResource(Border)));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(16, 9, 16, 9)));
            style.Setters.Add(new Setter(Control.FontSizeProperty, 13d));
            style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
            style.Setters.Add(new Setter(FrameworkElement.CursorProperty, System.Windows.Input.Cursors.Hand));

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "buttonBorder";
            border.SetBinding(System.Windows.Controls.Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetBinding(System.Windows.Controls.Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetBinding(System.Windows.Controls.Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetValue(System.Windows.Controls.Border.CornerRadiusProperty, new CornerRadius(6));
            FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            presenter.SetBinding(ContentPresenter.MarginProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.AppendChild(presenter);
            template.VisualTree = border;

            Trigger hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Control.OpacityProperty, 0.88));
            template.Triggers.Add(hover);
            Trigger pressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
            pressed.Setters.Add(new Setter(Control.OpacityProperty, 0.72));
            template.Triggers.Add(pressed);
            Trigger disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            disabled.Setters.Add(new Setter(Control.OpacityProperty, 0.42));
            template.Triggers.Add(disabled);
            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            return style;
        }

        private static Style CreateTextBoxStyle(Window window)
        {
            Style style = new Style(typeof(TextBox));
            style.Setters.Add(new Setter(Control.ForegroundProperty, window.FindResource(Text)));
            style.Setters.Add(new Setter(Control.BackgroundProperty, window.FindResource(Input)));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, window.FindResource(Border)));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 9, 12, 9)));
            style.Setters.Add(new Setter(Control.FontFamilyProperty, new FontFamily("Segoe UI")));
            style.Setters.Add(new Setter(Control.FontSizeProperty, 13d));
            style.Setters.Add(new Setter(TextBox.CaretBrushProperty, window.FindResource(Primary)));
            return style;
        }

        private static Style CreateComboBoxStyle(Window window)
        {
            Style style = new Style(typeof(ComboBox));
            style.Setters.Add(new Setter(Control.ForegroundProperty, window.FindResource(Text)));
            style.Setters.Add(new Setter(Control.BackgroundProperty, window.FindResource(Input)));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, window.FindResource(Border)));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(9, 7, 9, 7)));
            style.Setters.Add(new Setter(Control.FontSizeProperty, 13d));
            style.Setters.Add(new Setter(Control.TemplateProperty, CreateComboBoxTemplate(window)));

            Style itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, window.FindResource(Text)));
            itemStyle.Setters.Add(new Setter(Control.BackgroundProperty, window.FindResource(Input)));
            itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 8, 10, 8)));
            itemStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
            Trigger highlighted = new Trigger { Property = ComboBoxItem.IsHighlightedProperty, Value = true };
            highlighted.Setters.Add(new Setter(Control.BackgroundProperty, window.FindResource(Primary)));
            highlighted.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            itemStyle.Triggers.Add(highlighted);
            style.Setters.Add(new Setter(ItemsControl.ItemContainerStyleProperty, itemStyle));
            return style;
        }

        private static ControlTemplate CreateComboBoxTemplate(Window window)
        {
            ControlTemplate template = new ControlTemplate(typeof(ComboBox));
            FrameworkElementFactory root = new FrameworkElementFactory(typeof(Grid));

            FrameworkElementFactory frame = new FrameworkElementFactory(typeof(Border));
            frame.SetBinding(System.Windows.Controls.Border.BackgroundProperty, new Binding("Background") { RelativeSource = TemplatedParent() });
            frame.SetBinding(System.Windows.Controls.Border.BorderBrushProperty, new Binding("BorderBrush") { RelativeSource = TemplatedParent() });
            frame.SetBinding(System.Windows.Controls.Border.BorderThicknessProperty, new Binding("BorderThickness") { RelativeSource = TemplatedParent() });
            frame.SetValue(System.Windows.Controls.Border.CornerRadiusProperty, new CornerRadius(5));
            root.AppendChild(frame);

            FrameworkElementFactory selected = new FrameworkElementFactory(typeof(ContentPresenter));
            selected.SetBinding(ContentPresenter.ContentProperty, new Binding("SelectionBoxItem") { RelativeSource = TemplatedParent() });
            selected.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding("SelectionBoxItemTemplate") { RelativeSource = TemplatedParent() });
            selected.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            selected.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            selected.SetValue(FrameworkElement.MarginProperty, new Thickness(11, 0, 32, 0));
            selected.SetValue(UIElement.IsHitTestVisibleProperty, false);
            root.AppendChild(selected);

            FrameworkElementFactory arrow = new FrameworkElementFactory(typeof(TextBlock));
            arrow.SetValue(TextBlock.TextProperty, "▾");
            arrow.SetValue(TextBlock.FontSizeProperty, 11d);
            arrow.SetBinding(TextBlock.ForegroundProperty, new Binding("Foreground") { RelativeSource = TemplatedParent() });
            arrow.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            arrow.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            arrow.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 11, 1));
            arrow.SetValue(UIElement.IsHitTestVisibleProperty, false);
            root.AppendChild(arrow);

            FrameworkElementFactory toggle = new FrameworkElementFactory(typeof(ToggleButton));
            toggle.Name = "DropDownToggle";
            toggle.SetValue(Control.BackgroundProperty, Brushes.Transparent);
            toggle.SetValue(Control.BorderThicknessProperty, new Thickness(0));
            toggle.SetValue(ButtonBase.ClickModeProperty, ClickMode.Press);
            toggle.SetValue(UIElement.FocusableProperty, false);
            Binding openBinding = new Binding("IsDropDownOpen") { RelativeSource = TemplatedParent(), Mode = BindingMode.TwoWay };
            toggle.SetBinding(ToggleButton.IsCheckedProperty, openBinding);
            ControlTemplate toggleTemplate = new ControlTemplate(typeof(ToggleButton));
            FrameworkElementFactory transparent = new FrameworkElementFactory(typeof(Border));
            transparent.SetValue(System.Windows.Controls.Border.BackgroundProperty, Brushes.Transparent);
            toggleTemplate.VisualTree = transparent;
            toggle.SetValue(Control.TemplateProperty, toggleTemplate);
            root.AppendChild(toggle);

            FrameworkElementFactory popup = new FrameworkElementFactory(typeof(Popup));
            popup.Name = "PART_Popup";
            popup.SetValue(Popup.PlacementProperty, PlacementMode.Bottom);
            popup.SetValue(Popup.AllowsTransparencyProperty, true);
            popup.SetValue(Popup.PopupAnimationProperty, PopupAnimation.Fade);
            popup.SetValue(UIElement.FocusableProperty, false);
            popup.SetBinding(Popup.IsOpenProperty, openBinding);

            FrameworkElementFactory dropBorder = new FrameworkElementFactory(typeof(Border));
            dropBorder.SetValue(System.Windows.Controls.Border.BackgroundProperty, window.FindResource(Input));
            dropBorder.SetValue(System.Windows.Controls.Border.BorderBrushProperty, window.FindResource(Border));
            dropBorder.SetValue(System.Windows.Controls.Border.BorderThicknessProperty, new Thickness(1));
            dropBorder.SetValue(System.Windows.Controls.Border.CornerRadiusProperty, new CornerRadius(5));
            dropBorder.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 3, 0, 0));
            dropBorder.SetBinding(FrameworkElement.MinWidthProperty, new Binding("ActualWidth") { RelativeSource = TemplatedParent() });

            FrameworkElementFactory scroll = new FrameworkElementFactory(typeof(ScrollViewer));
            scroll.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
            scroll.SetValue(FrameworkElement.MaxHeightProperty, 320d);
            FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ItemsPresenter));
            scroll.AppendChild(presenter);
            dropBorder.AppendChild(scroll);
            popup.AppendChild(dropBorder);
            root.AppendChild(popup);

            Trigger disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.45));
            template.Triggers.Add(disabled);
            template.VisualTree = root;
            return template;
        }

        private static RelativeSource TemplatedParent()
        {
            return new RelativeSource(RelativeSourceMode.TemplatedParent);
        }

        private static Style CreateCheckBoxStyle(Window window)
        {
            Style style = new Style(typeof(CheckBox));
            style.Setters.Add(new Setter(Control.ForegroundProperty, window.FindResource(Text)));
            style.Setters.Add(new Setter(Control.FontSizeProperty, 13d));
            style.Setters.Add(new Setter(FrameworkElement.CursorProperty, System.Windows.Input.Cursors.Hand));
            style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, 0, 22, 0)));
            return style;
        }

        private static Style CreateProgressStyle(Window window)
        {
            Style style = new Style(typeof(ProgressBar));
            style.Setters.Add(new Setter(Control.ForegroundProperty, window.FindResource(Primary)));
            style.Setters.Add(new Setter(Control.BackgroundProperty, window.FindResource(SurfaceAlt)));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
            style.Setters.Add(new Setter(FrameworkElement.HeightProperty, 7d));
            return style;
        }

        private static SolidColorBrush Brush(string value)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
        }
    }
}
