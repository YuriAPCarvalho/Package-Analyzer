using System.Diagnostics;
using System.Text;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using TrivyProjectManager.App.DTOs;
using TrivyProjectManager.App.Views;
using TrivyProjectManager.Application.DTOs;

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

    public async Task<bool> ShowMandatoryUpdateAsync(ApplicationUpdateResult update, CancellationToken cancellationToken = default)
    {
        if (Owner is null)
        {
            return false;
        }

        var window = new ApplicationUpdateWindow(update);
        return await window.ShowDialog<bool>(Owner);
    }

    public async Task CopyToClipboardAsync(string text, CancellationToken cancellationToken = default)
    {
        if (Owner?.Clipboard is not null)
        {
            await Owner.Clipboard.SetTextAsync(text);
        }
    }

    public async Task SaveTextFileAsync(string suggestedFileName, string content, CancellationToken cancellationToken = default)
    {
        if (Owner?.StorageProvider is null)
        {
            return;
        }

        var file = await Owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Salvar relatório em TXT",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "txt",
            FileTypeChoices =
            [
                new FilePickerFileType("Arquivo de texto")
                {
                    Patterns = ["*.txt"],
                    MimeTypes = ["text/plain"]
                }
            ]
        });
        if (file is null)
        {
            return;
        }

        await using var stream = await file.OpenWriteAsync();
        if (stream.CanSeek)
        {
            stream.SetLength(0);
        }

        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        await writer.WriteAsync(content.AsMemory(), cancellationToken);
    }

    public Task OpenFolderAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.CompletedTask;
        }

        var folder = File.Exists(path) ? Path.GetDirectoryName(path) : path;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return Task.CompletedTask;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = folder,
            UseShellExecute = true
        });
        return Task.CompletedTask;
    }

    public void CloseApplication()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    public async Task<SecurityExceptionDialogResult?> ShowSecurityExceptionDialogAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        if (Owner is null)
        {
            return null;
        }

        var window = new SecurityExceptionWindow(title, message);
        return await window.ShowDialog<SecurityExceptionDialogResult?>(Owner);
    }
}
