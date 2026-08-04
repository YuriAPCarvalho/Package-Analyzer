using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrivyProjectManager.App.Services;
using TrivyProjectManager.App.ViewModels;
using TrivyProjectManager.App.Views;
using TrivyProjectManager.Infrastructure;
using TrivyProjectManager.Infrastructure.Data;

namespace TrivyProjectManager.App;

public sealed partial class App : Avalonia.Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(new LocalFileLoggerProvider()));
        services.AddTrivyProjectManager();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddTransient<MainWindowViewModel>();
        _services = services.BuildServiceProvider();

        using (var scope = _services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<TrivyProjectManagerDbContext>().Database.Migrate();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dialogService = (DialogService)_services.GetRequiredService<IDialogService>();
            var window = new MainWindow();
            dialogService.Owner = window;
            var viewModel = _services.GetRequiredService<MainWindowViewModel>();
            window.DataContext = viewModel;
            desktop.MainWindow = window;
            viewModel.LoadCommand.Execute(null);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
