using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TestAvaloniaFoldablePanel;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    #if AVALONIA_10
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    #endif
}