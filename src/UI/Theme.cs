using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Markup;
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
            resources[WindowBackground] = Brush(dark ? "#101418" : "#F0F3F6");
            resources[Surface] = Brush(dark ? "#181E24" : "#FFFFFF");
            resources[SurfaceAlt] = Brush(dark ? "#20272E" : "#E9EEF2");
            resources[Input] = Brush(dark ? "#12171C" : "#F8FAFC");
            resources[Border] = Brush(dark ? "#313A43" : "#BEC9D2");
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
            resources[typeof(ScrollBar)] = CreateScrollBarStyle();
        }

        public static void Bind(FrameworkElement element, DependencyProperty property, string key)
        {
            element.SetResourceReference(property, key);
        }

        public static void StyleMenu(ContextMenu menu, Window window)
        {
            menu.Style = CreateContextMenuStyle(window);
            menu.Resources[typeof(MenuItem)] = CreateMenuItemStyle(window);
            menu.Resources[typeof(Separator)] = CreateSeparatorStyle(window);
        }

        public static bool IsDarkTheme(Window window)
        {
            SolidColorBrush brush = window.FindResource(WindowBackground) as SolidColorBrush;
            if (brush == null)
                return true;
            Color color = brush.Color;
            return color.R + color.G + color.B < 384;
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
            Trigger focus = new Trigger { Property = UIElement.IsKeyboardFocusWithinProperty, Value = true };
            focus.Setters.Add(new Setter(Control.BorderBrushProperty, window.FindResource(Primary)));
            style.Triggers.Add(focus);
            Trigger disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.5));
            style.Triggers.Add(disabled);
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
            frame.Name = "comboFrame";
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
            Trigger hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(System.Windows.Controls.Border.BorderBrushProperty, window.FindResource(PrimaryHover), "comboFrame"));
            template.Triggers.Add(hover);
            Trigger focus = new Trigger { Property = UIElement.IsKeyboardFocusWithinProperty, Value = true };
            focus.Setters.Add(new Setter(System.Windows.Controls.Border.BorderBrushProperty, window.FindResource(Primary), "comboFrame"));
            template.Triggers.Add(focus);
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

            ControlTemplate template = new ControlTemplate(typeof(CheckBox));
            FrameworkElementFactory root = new FrameworkElementFactory(typeof(StackPanel));
            root.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            FrameworkElementFactory box = new FrameworkElementFactory(typeof(Border));
            box.Name = "checkBoxFrame";
            box.SetValue(FrameworkElement.WidthProperty, 18d);
            box.SetValue(FrameworkElement.HeightProperty, 18d);
            box.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            box.SetValue(System.Windows.Controls.Border.CornerRadiusProperty, new CornerRadius(3));
            box.SetValue(System.Windows.Controls.Border.BackgroundProperty, window.FindResource(Input));
            box.SetValue(System.Windows.Controls.Border.BorderBrushProperty, window.FindResource(Border));
            box.SetValue(System.Windows.Controls.Border.BorderThicknessProperty, new Thickness(1));
            FrameworkElementFactory mark = new FrameworkElementFactory(typeof(TextBlock));
            mark.Name = "checkMark";
            mark.SetValue(TextBlock.TextProperty, "\uE73E");
            mark.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Segoe MDL2 Assets"));
            mark.SetValue(TextBlock.FontSizeProperty, 10d);
            mark.SetValue(TextBlock.ForegroundProperty, Brushes.White);
            mark.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            mark.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            mark.SetValue(UIElement.VisibilityProperty, Visibility.Hidden);
            box.AppendChild(mark);
            root.AppendChild(box);

            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetBinding(ContentPresenter.ContentProperty, new Binding("Content") { RelativeSource = TemplatedParent() });
            content.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding("ContentTemplate") { RelativeSource = TemplatedParent() });
            content.SetValue(FrameworkElement.MarginProperty, new Thickness(8, 0, 0, 0));
            content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            root.AppendChild(content);
            template.VisualTree = root;

            Trigger checkedState = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
            checkedState.Setters.Add(new Setter(System.Windows.Controls.Border.BackgroundProperty, window.FindResource(Primary), "checkBoxFrame"));
            checkedState.Setters.Add(new Setter(System.Windows.Controls.Border.BorderBrushProperty, window.FindResource(Primary), "checkBoxFrame"));
            checkedState.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible, "checkMark"));
            template.Triggers.Add(checkedState);
            Trigger hoverState = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverState.Setters.Add(new Setter(System.Windows.Controls.Border.BorderBrushProperty, window.FindResource(PrimaryHover), "checkBoxFrame"));
            template.Triggers.Add(hoverState);
            Trigger disabledState = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            disabledState.Setters.Add(new Setter(UIElement.OpacityProperty, 0.45));
            template.Triggers.Add(disabledState);
            style.Setters.Add(new Setter(Control.TemplateProperty, template));
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

        private static Style CreateScrollBarStyle()
        {
            const string xaml =
                "<Style xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
                "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" TargetType=\"{x:Type ScrollBar}\">" +
                "<Setter Property=\"Background\" Value=\"{DynamicResource SurfaceAlt}\"/>" +
                "<Setter Property=\"Width\" Value=\"12\"/>" +
                "<Setter Property=\"Template\"><Setter.Value>" +
                "<ControlTemplate TargetType=\"{x:Type ScrollBar}\">" +
                "<Grid x:Name=\"Root\" Background=\"{TemplateBinding Background}\" ClipToBounds=\"True\">" +
                "<Track x:Name=\"PART_Track\" Orientation=\"Vertical\" IsDirectionReversed=\"True\" " +
                "Minimum=\"{TemplateBinding Minimum}\" Maximum=\"{TemplateBinding Maximum}\" Value=\"{TemplateBinding Value}\" " +
                "ViewportSize=\"{TemplateBinding ViewportSize}\" Focusable=\"False\">" +
                "<Track.DecreaseRepeatButton><RepeatButton x:Name=\"DecreaseButton\" Command=\"{x:Static ScrollBar.PageUpCommand}\" Focusable=\"False\">" +
                "<RepeatButton.Template><ControlTemplate TargetType=\"{x:Type RepeatButton}\"><Border Background=\"Transparent\"/></ControlTemplate></RepeatButton.Template>" +
                "</RepeatButton></Track.DecreaseRepeatButton>" +
                "<Track.Thumb><Thumb x:Name=\"ScrollThumb\" MinHeight=\"26\" Background=\"{DynamicResource Border}\">" +
                "<Thumb.Template><ControlTemplate TargetType=\"{x:Type Thumb}\">" +
                "<Border x:Name=\"ThumbVisual\" Margin=\"2\" Background=\"{TemplateBinding Background}\" CornerRadius=\"4\"/>" +
                "<ControlTemplate.Triggers><Trigger Property=\"IsMouseOver\" Value=\"True\">" +
                "<Setter TargetName=\"ThumbVisual\" Property=\"Background\" Value=\"{DynamicResource Primary}\"/>" +
                "</Trigger></ControlTemplate.Triggers></ControlTemplate>" +
                "</Thumb.Template></Thumb></Track.Thumb>" +
                "<Track.IncreaseRepeatButton><RepeatButton x:Name=\"IncreaseButton\" Command=\"{x:Static ScrollBar.PageDownCommand}\" Focusable=\"False\">" +
                "<RepeatButton.Template><ControlTemplate TargetType=\"{x:Type RepeatButton}\"><Border Background=\"Transparent\"/></ControlTemplate></RepeatButton.Template>" +
                "</RepeatButton></Track.IncreaseRepeatButton>" +
                "</Track></Grid>" +
                "<ControlTemplate.Triggers><Trigger Property=\"Orientation\" Value=\"Horizontal\">" +
                "<Setter Property=\"Width\" Value=\"Auto\"/><Setter Property=\"Height\" Value=\"12\"/>" +
                "<Setter TargetName=\"PART_Track\" Property=\"Orientation\" Value=\"Horizontal\"/>" +
                "<Setter TargetName=\"PART_Track\" Property=\"IsDirectionReversed\" Value=\"False\"/>" +
                "<Setter TargetName=\"DecreaseButton\" Property=\"Command\" Value=\"{x:Static ScrollBar.PageLeftCommand}\"/>" +
                "<Setter TargetName=\"IncreaseButton\" Property=\"Command\" Value=\"{x:Static ScrollBar.PageRightCommand}\"/>" +
                "<Setter TargetName=\"ScrollThumb\" Property=\"MinHeight\" Value=\"0\"/>" +
                "<Setter TargetName=\"ScrollThumb\" Property=\"MinWidth\" Value=\"26\"/>" +
                "</Trigger><Trigger Property=\"IsEnabled\" Value=\"False\"><Setter Property=\"Opacity\" Value=\"0\"/></Trigger>" +
                "</ControlTemplate.Triggers></ControlTemplate>" +
                "</Setter.Value></Setter></Style>";
            return (Style)XamlReader.Parse(xaml);
        }

        private static Style CreateContextMenuStyle(Window window)
        {
            Style style = new Style(typeof(ContextMenu));
            style.Setters.Add(new Setter(Control.ForegroundProperty, window.FindResource(Text)));
            style.Setters.Add(new Setter(Control.BackgroundProperty, window.FindResource(SurfaceAlt)));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, window.FindResource(Border)));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(5)));
            style.Setters.Add(new Setter(Control.FontFamilyProperty, new FontFamily("Segoe UI")));

            ControlTemplate template = new ControlTemplate(typeof(ContextMenu));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetBinding(System.Windows.Controls.Border.BackgroundProperty, new Binding("Background") { RelativeSource = TemplatedParent() });
            border.SetBinding(System.Windows.Controls.Border.BorderBrushProperty, new Binding("BorderBrush") { RelativeSource = TemplatedParent() });
            border.SetBinding(System.Windows.Controls.Border.BorderThicknessProperty, new Binding("BorderThickness") { RelativeSource = TemplatedParent() });
            border.SetBinding(System.Windows.Controls.Border.PaddingProperty, new Binding("Padding") { RelativeSource = TemplatedParent() });
            border.SetValue(System.Windows.Controls.Border.CornerRadiusProperty, new CornerRadius(6));
            FrameworkElementFactory items = new FrameworkElementFactory(typeof(StackPanel));
            items.SetValue(Panel.IsItemsHostProperty, true);
            border.AppendChild(items);
            template.VisualTree = border;
            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            return style;
        }

        private static Style CreateMenuItemStyle(Window window)
        {
            Style style = new Style(typeof(MenuItem));
            style.Setters.Add(new Setter(Control.ForegroundProperty, window.FindResource(Text)));
            style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(13, 8, 13, 8)));
            style.Setters.Add(new Setter(Control.FontSizeProperty, 12.5d));
            style.Setters.Add(new Setter(FrameworkElement.CursorProperty, System.Windows.Input.Cursors.Hand));

            ControlTemplate template = new ControlTemplate(typeof(MenuItem));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "menuItemBorder";
            border.SetBinding(System.Windows.Controls.Border.BackgroundProperty, new Binding("Background") { RelativeSource = TemplatedParent() });
            border.SetValue(System.Windows.Controls.Border.CornerRadiusProperty, new CornerRadius(4));
            FrameworkElementFactory row = new FrameworkElementFactory(typeof(StackPanel));
            row.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            FrameworkElementFactory check = new FrameworkElementFactory(typeof(TextBlock));
            check.Name = "menuCheck";
            check.SetValue(TextBlock.TextProperty, "\uE73E");
            check.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Segoe MDL2 Assets"));
            check.SetValue(TextBlock.FontSizeProperty, 10d);
            check.SetValue(FrameworkElement.WidthProperty, 19d);
            check.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            check.SetValue(UIElement.VisibilityProperty, Visibility.Hidden);
            row.AppendChild(check);
            FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetBinding(ContentPresenter.ContentProperty, new Binding("Header") { RelativeSource = TemplatedParent() });
            presenter.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding("HeaderTemplate") { RelativeSource = TemplatedParent() });
            presenter.SetBinding(ContentPresenter.MarginProperty, new Binding("Padding") { RelativeSource = TemplatedParent() });
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            row.AppendChild(presenter);
            border.AppendChild(row);
            template.VisualTree = border;

            Trigger highlighted = new Trigger { Property = MenuItem.IsHighlightedProperty, Value = true };
            highlighted.Setters.Add(new Setter(Control.BackgroundProperty, window.FindResource(Primary)));
            highlighted.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            template.Triggers.Add(highlighted);
            Trigger disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.45));
            template.Triggers.Add(disabled);
            Trigger checkedState = new Trigger { Property = MenuItem.IsCheckedProperty, Value = true };
            checkedState.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible, "menuCheck"));
            template.Triggers.Add(checkedState);
            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            return style;
        }

        private static Style CreateSeparatorStyle(Window window)
        {
            Style style = new Style(typeof(Separator));
            style.Setters.Add(new Setter(FrameworkElement.HeightProperty, 1d));
            style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(10, 5, 10, 5)));
            ControlTemplate template = new ControlTemplate(typeof(Separator));
            FrameworkElementFactory line = new FrameworkElementFactory(typeof(Border));
            line.SetValue(System.Windows.Controls.Border.BackgroundProperty, window.FindResource(Border));
            template.VisualTree = line;
            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            return style;
        }

        private static SolidColorBrush Brush(string value)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
        }
    }
}
