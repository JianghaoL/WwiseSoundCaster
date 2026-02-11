using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System;
using System.Linq;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using WwiseSoundCaster.Services;
using WwiseSoundCaster.ViewModels;
using WwiseSoundCaster.Views;

namespace WwiseSoundCaster;

/// <summary>
/// Application entry point.
///
/// This is the <b>composition root</b> — the only place that knows about
/// concrete service implementations and concrete View types.  All other
/// classes depend exclusively on interfaces.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Global service provider.  Prefer constructor injection in all new
    /// classes; only fall back to this for edge cases (e.g. ViewLocator).
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // ── Build the DI container ──────────────────────────
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            Services = serviceCollection.BuildServiceProvider();

            // ── Bootstrap the startup window ────────────────────
            // Resolve the connect-window ViewModel through DI so all
            // its dependencies (IWwiseConnectionService, IWindowService)
            // are satisfied automatically.
            var connectVm = Services.GetRequiredService<WwiseConnectWindowViewModel>();
            var connectWindow = new WwiseConnectWindow { DataContext = connectVm };

            // Register the connect window with the WindowService so it
            // can be closed when navigation to MainWindow occurs.
            // Note: We cast to the concrete WindowService here because
            // Register() is an infrastructure concern, not part of the
            // public IWindowService contract.
            var windowService = (WindowService)Services.GetRequiredService<IWindowService>();
            windowService.Register(connectVm, connectWindow);

            desktop.MainWindow = connectWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    // ── Service Registration ────────────────────────────────────

    /// <summary>
    /// Registers all services, ViewModels, and their lifetimes.
    ///
    /// Singleton services share state across the application lifetime.
    /// Transient ViewModels get a fresh instance each time (standard
    /// practice — each window gets its own ViewModel).
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        // ── Services ────────────────────────────────────────────
        services.AddSingleton<IWwiseConnectionService, WwiseConnectionService>();
        services.AddSingleton<IWwiseObjectService, WwiseObjectService>();
        services.AddSingleton<IWindowService, WindowService>();

        // ── ViewModels ──────────────────────────────────────────
        services.AddTransient<WwiseConnectWindowViewModel>();
        services.AddTransient<MainWindowViewModel>();
    }

    // ── Helpers ─────────────────────────────────────────────────

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}