using Avalonia.Controls;
using TrivyProjectManager.Application.DTOs;

namespace TrivyProjectManager.App.Views;

public sealed partial class ApplicationUpdateWindow : Window
{
    public ApplicationUpdateWindow()
    {
        InitializeComponent();
    }

    public ApplicationUpdateWindow(ApplicationUpdateResult update)
    {
        InitializeComponent();
        DataContext = new ApplicationUpdateWindowModel(
            update.Message,
            $"Versão instalada: {update.InstalledVersion} | Nova versão: {update.AvailableVersion ?? "-"}",
            string.IsNullOrWhiteSpace(update.ReleaseNotes) ? "Sem notas de versão publicadas." : update.ReleaseNotes);
    }

    private void UpdateNow_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(true);
    }

    private void CloseApplication_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(false);
    }

    private sealed record ApplicationUpdateWindowModel(string Message, string VersionText, string ReleaseNotes);
}
