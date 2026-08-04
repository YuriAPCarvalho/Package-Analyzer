using Avalonia.Controls;
using Avalonia.Platform.Storage;
using TrivyProjectManager.App.Views;

namespace TrivyProjectManager.App.Services;

public sealed class DialogService : IDialogService
{
    public Window? Owner { get; set; }

    public async Task<string?> PickFolderAsync(CancellationToken cancellationToken = default)
    {
        if (Owner?.StorageProvider is null)
        {
            return null;
        }

        var folders = await Owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Selecionar pasta do projeto"
        });

        return folders.FirstOrDefault()?.TryGetLocalPath();
    }

    public async Task<bool> ConfirmAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        if (Owner is null)
        {
            return false;
        }

        var window = new MessageWindow(title, message, showCancel: true);
        return await window.ShowDialog<bool>(Owner);
    }

    public async Task ShowMessageAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        if (Owner is null)
        {
            return;
        }

        var window = new MessageWindow(title, message, showCancel: false);
        await window.ShowDialog<bool>(Owner);
    }
}
