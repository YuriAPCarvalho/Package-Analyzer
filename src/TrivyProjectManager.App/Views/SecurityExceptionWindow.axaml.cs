using Avalonia.Controls;
using TrivyProjectManager.App.DTOs;

namespace TrivyProjectManager.App.Views;

public sealed partial class SecurityExceptionWindow : Window
{
    public SecurityExceptionWindow()
    {
        InitializeComponent();
    }

    public SecurityExceptionWindow(string title, string message)
    {
        InitializeComponent();
        DataContext = new SecurityExceptionWindowModel(title, message);
    }

    private void Ok_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var reason = ReasonBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            ErrorText.Text = "Informe um motivo para a exceção.";
            return;
        }

        DateTimeOffset? expiresAt = null;
        var expiresText = ExpiresBox.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(expiresText))
        {
            if (!DateOnly.TryParse(expiresText, out var date))
            {
                ErrorText.Text = "Informe a validade no formato yyyy-MM-dd ou deixe em branco.";
                return;
            }

            expiresAt = date.ToDateTime(TimeOnly.MaxValue);
        }

        Close(new SecurityExceptionDialogResult(reason, expiresAt));
    }

    private void Cancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(null);
    }

    private sealed record SecurityExceptionWindowModel(string TitleText, string Message);
}
