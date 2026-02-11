namespace WwiseSoundCaster.Services;

/// <summary>
/// Abstracts window lifecycle management so that ViewModels never
/// reference concrete Avalonia <c>Window</c> types.
///
/// This is the key mechanism that prevents tight coupling between
/// <c>WwiseConnectWindowViewModel</c> and <c>MainWindow</c>:
/// the ViewModel asks the service to "show the main window" without
/// knowing which class that actually is.
/// </summary>
public interface IWindowService
{
    /// <summary>
    /// Creates and shows the main application window,
    /// setting it as the desktop lifetime's primary window.
    /// </summary>
    void ShowMainWindow();

    /// <summary>
    /// Closes the window that is currently hosting the given ViewModel.
    /// </summary>
    /// <param name="ownerViewModel">
    /// The ViewModel whose host window should be closed.
    /// </param>
    void CloseWindow(object ownerViewModel);
}
