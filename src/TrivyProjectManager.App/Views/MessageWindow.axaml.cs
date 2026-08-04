using Avalonia.Controls;

namespace TrivyProjectManager.App.Views;

public sealed partial class MessageWindow : Window
{
    public MessageWindow()
    {
        InitializeComponent();
    }

    public MessageWindow(string title, string message, bool showCancel)
    {
        InitializeComponent();
        DataContext = new MessageWindowModel(title, message, showCancel);
    }

    private void Ok_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(true);
    }

    private void Cancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(false);
    }

    private sealed record MessageWindowModel(string TitleText, string Message, bool ShowCancel);
}
