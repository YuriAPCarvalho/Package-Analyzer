using TrivyProjectManager.App.DTOs;
using TrivyProjectManager.Application.DTOs;

namespace TrivyProjectManager.App.Services;

public interface IDialogService
{
    Task<string?> PickFolderAsync(CancellationToken cancellationToken = default);
    Task<bool> ConfirmAsync(string title, string message, CancellationToken cancellationToken = default);
    Task ShowMessageAsync(string title, string message, CancellationToken cancellationToken = default);
    Task<bool> ShowMandatoryUpdateAsync(ApplicationUpdateResult update, CancellationToken cancellationToken = default);
    Task CopyToClipboardAsync(string text, CancellationToken cancellationToken = default);
    Task SaveTextFileAsync(string suggestedFileName, string content, CancellationToken cancellationToken = default);
    Task OpenFolderAsync(string path, CancellationToken cancellationToken = default);
    void CloseApplication();
    Task<SecurityExceptionDialogResult?> ShowSecurityExceptionDialogAsync(string title, string message, CancellationToken cancellationToken = default);
}
