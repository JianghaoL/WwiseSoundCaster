using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using WwiseSoundCaster.ViewModels;
using WwiseSoundCaster.Views;

namespace WwiseSoundCaster.Services;

/// <summary>
/// Manages application window lifecycle, decoupling ViewModels from
/// concrete Avalonia <see cref="Window"/> types.
///
/// Maintains an internal registry so that <see cref="CloseWindow"/>
/// can locate and close the correct window by its ViewModel reference.
///
/// Architectural notes:
///   • ViewModels call <see cref="IWindowService"/> — they never
///     <c>new</c>-up a <see cref="Window"/> or reference one.
///   • Only the composition root (<c>App.axaml.cs</c>) calls
///     <see cref="Register"/> to seed the initial connect window.
///   • <see cref="ShowMainWindow"/> resolves <see cref="MainWindowViewModel"/>
///     from the DI container, ensuring all its dependencies are injected.
/// </summary>
public class WindowService : IWindowService
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Maps a ViewModel instance to the Window that hosts it.
    /// Populated via <see cref="Register"/> and consumed by <see cref="CloseWindow"/>.
    /// </summary>
    private readonly Dictionary<object, Window> _windowRegistry = new();

    public WindowService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    // ── Registration ────────────────────────────────────────────

    /// <summary>
    /// Registers an existing (externally-created) window so the service
    /// can close it later.  Called from the composition root for the
    /// startup connect window.
    /// </summary>
    /// <remarks>
    /// This method is intentionally <b>not</b> on <see cref="IWindowService"/>
    /// because it is an infrastructure concern — ViewModels should never
    /// call it.
    /// </remarks>
    public void Register(object viewModel, Window window)
    {
        _windowRegistry[viewModel] = window;
    }

    // ── IWindowService ──────────────────────────────────────────

    /// <inheritdoc/>
    public void ShowMainWindow()
    {
        // Resolve the MainWindowViewModel through DI so that all its
        // dependencies (IWwiseObjectService, etc.) are constructor-injected.
        var viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();

        var window = new MainWindow { DataContext = viewModel };
        Register(viewModel, window);

        // Promote this window to the application's primary window
        // so that closing it terminates the desktop lifetime.
        if (Application.Current?.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = window;
        }

        window.Show();

        // Kick off the ViewModel's async initialization after the window
        // is visible. This populates the event tree without blocking the UI.
        viewModel.InitializeAsync();
    }

    /// <inheritdoc/>
    public void CloseWindow(object ownerViewModel)
    {
        if (_windowRegistry.TryGetValue(ownerViewModel, out var window))
        {
            _windowRegistry.Remove(ownerViewModel);
            window.Close();
        }
    }
}
