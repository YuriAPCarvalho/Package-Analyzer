namespace TrivyProjectManager.App.Services;

public interface IDialogService
{
    Task<string?> PickFolderAsync(CancellationToken cancellationToken = default);
    Task<bool> ConfirmAsync(string title, string message, CancellationToken cancellationToken = default);
    Task ShowMessageAsync(string title, string message, CancellationToken cancellationToken = default);
}
