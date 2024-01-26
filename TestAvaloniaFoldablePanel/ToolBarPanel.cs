using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
#if AVALONIA_11
using IControl=Avalonia.Controls.Control;
using IVisual=Avalonia.Visual;
#endif

namespace TestAvaloniaFoldablePanel;

public class ToolBarPanel : Panel
{
    public static readonly StyledProperty<Panel?> OutOfBoundsPanelProperty = AvaloniaProperty.Register<ToolBarPanel, Panel?>(nameof(OutOfBoundsPanel));
    public static readonly StyledProperty<bool> IsOverflowProperty = AvaloniaProperty.Register<ToolBarPanel, bool>(nameof(IsOverflow));
    public static readonly StyledProperty<double> SpacingProperty = AvaloniaProperty.Register<ToolBarPanel, double>(nameof(Spacing), 4);

    private List<IControl> overflowControls = new List<IControl>();
    
    public Panel? OutOfBoundsPanel
    {
        get => GetValue(OutOfBoundsPanelProperty);
        set => SetValue(OutOfBoundsPanelProperty, value);
    }
    
    public bool IsOverflow
    {
        get => GetValue(IsOverflowProperty);
        set => SetValue(IsOverflowProperty, value);
    }
    
    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    static ToolBarPanel()
    {
        OutOfBoundsPanelProperty.Changed.AddClassHandler<ToolBarPanel>((panel, e) => panel.OnChangedOutOfBoundPanel(e));
        AffectsMeasure<ToolBarPanel>(SpacingProperty);
        AffectsArrange<ToolBarPanel>(SpacingProperty);
    }

    private void OnChangedOutOfBoundPanel(AvaloniaPropertyChangedEventArgs changed)
    {
        var newPanel = changed.NewValue as Panel;
        if (newPanel == null)
            return;
        
        newPanel.AttachedToVisualTree += OutOfBoundsPanelAttached;
    }

    private void OutOfBoundsPanelAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        // when the out of bounds panel is opened, move the overflow controls to it
        foreach (var child in overflowControls) 
            OutOfBoundsPanel!.Children.Insert(0, child);
        overflowControls.Clear();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var spacing = Spacing;
        double desiredWidth = 0;
        double desiredHeight = 0;
        var children = Children;
        bool any = false;
        for (int i = 0, count = children.Count; i < count; ++i)
        {
            var child = children[i];

            if (child == null)
                continue;

            if (!child.IsVisible)
            {
                child.Measure(availableSize.WithWidth(double.PositiveInfinity));
                continue;
            }

            any = true;
            child.Measure(availableSize.WithWidth(double.PositiveInfinity));
            desiredWidth += child.DesiredSize.Width + spacing;
            desiredHeight = Math.Max(desiredHeight, child.DesiredSize.Height);
        }

        foreach (var child in overflowControls)
            desiredWidth += child.DesiredSize.Width + spacing;
        
        if (OutOfBoundsPanel != null)
        {
            OutOfBoundsPanel.Measure(availableSize.WithWidth(double.PositiveInfinity));
            desiredWidth += OutOfBoundsPanel.DesiredSize.Width + spacing;
        }

        return new Size(any ? desiredWidth : 0, desiredHeight);
    }
    
    protected override Size ArrangeOverride(Size finalSize)
    {
        var children = Children;
        Rect rcChild = new Rect(finalSize);
        double previousChildSize = 0.0;
        var spacing = Spacing;

        double totalDesiredWidth = 0;
        bool any = false;
        
        for (int i = 0, count = children.Count; i < count; ++i)
        {
            var child = children[i];

            if (child == null || !child.IsVisible)
                continue;

            any = true;
            totalDesiredWidth += child.DesiredSize.Width + spacing;
        }
        if (any)
            totalDesiredWidth -= spacing;

        double leftSpace = finalSize.Width - totalDesiredWidth;
        
        for (int i = 0, count = children.Count; i < count; ++i)
        {
            var child = children[i];

            if (child == null || !child.IsVisible)
                continue;

            rcChild = rcChild.WithX(rcChild.X + previousChildSize);
            rcChild = rcChild.WithHeight(Math.Max(finalSize.Height, child.DesiredSize.Height));
            previousChildSize = child.DesiredSize.Width;
            rcChild = rcChild.WithWidth(previousChildSize);
            previousChildSize += spacing;

            if (rcChild.Right > finalSize.Width)
                rcChild = rcChild.WithWidth(Math.Max(0, finalSize.Width - rcChild.X));

            if (rcChild.Right > finalSize.Width)
            {
                if (OutOfBoundsPanel is { } panel)
                {
                    for (var j = i; j < count; ++j)
                    {
                        var c = children[^1];
                        Children.RemoveAt(Children.Count - 1);
#if AVALONIA_10
                        if (((IVisual)panel).IsAttachedToVisualTree)
#else
                        if (panel.IsAttachedToVisualTree())
#endif
                            panel.Children.Insert(0, c);
                        else
                            overflowControls.Add(c);
                    }                        
                    IsOverflow = true;
                    return finalSize;
                }
            }
            ArrangeChild(child, rcChild, finalSize);
        }

        if (OutOfBoundsPanel != null)
        {
            if (leftSpace > 0)
            {
                IControl? child = null;
                if (overflowControls.Count > 0)
                    child = overflowControls[^1];
                else if (OutOfBoundsPanel.Children.Count > 0)
                    child = OutOfBoundsPanel.Children[0];

                if (child != null)
                {
                    if (leftSpace > child.Bounds.Width + spacing)
                    {
                        if (overflowControls.Count > 0)
                            overflowControls.RemoveAt(overflowControls.Count - 1);
                        else
                            OutOfBoundsPanel.Children.RemoveAt(0);
                        Children.Add(child);
                    }
                }
            }
            IsOverflow = OutOfBoundsPanel.Children.Count > 0 || overflowControls.Count > 0;
        }

        return finalSize;
    }
    
    internal virtual void ArrangeChild(
        IControl child,
        Rect rect,
        Size panelSize)
    {
        child.Arrange(rect);
    }
}