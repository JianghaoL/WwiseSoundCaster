using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;

namespace WwiseSoundCaster.ViewModels;

/// <summary>
/// Represents a single Wwise Event associated with the currently selected object.
/// Displayed in the header panel's Related Events list.
///
/// Architecture notes:
///   • This ViewModel is intentionally decoupled from <see cref="MainWindowViewModel"/>
///     and from any Wwise service/client. It exposes a <see cref="PlayCommand"/> that
///     is wired up via an <see cref="System.Action"/> delegate injected at construction
///     time, keeping the class testable and free of direct WAAPI references.
///   • The owning ViewModel (or a service coordinator) is responsible for supplying
///     the play callback that bridges to the actual WAAPI PostEvent call.
/// </summary>
public partial class EventViewModel : ViewModelBase
{
    /// <summary>
    /// The Wwise object ID of this event (GUID string).
    /// </summary>
    [ObservableProperty]
    private string _id = string.Empty;

    /// <summary>
    /// Display name of the event.
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// Plays this event via the injected callback.
    /// </summary>
    [RelayCommand]
    private async Task PlayAsync()
    {
        // TODO: Wire up actual WAAPI event playback logic here.
        //
        // Implementation approach (do NOT add direct WAAPI calls in this class):
        //   1. Accept an Action<string> or Func<string, Task> callback via the
        //      constructor or a settable property (e.g. PlayAction).
        //   2. The parent ViewModel or a service coordinator sets the callback
        //      to invoke IWwiseSoundEngineService.PostEvent(Id).
        //   3. This keeps EventViewModel free of any service dependency.
        //
        // Example future integration:
        //   PlayAction?.Invoke(Id);

        if (WwiseClient.client == null)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[EventViewModel] Cannot play event '{Name}' (ID: {Id}) - not connected to Wwise.");
            return;
        }

        try
        {

            var args = new JObject
            {
                {"event", Name},
                {"gameObject", MainWindowViewModel.GameObject["gameObject"]}
            };
            Console.WriteLine($"[EventViewModel] Playing event '{Name}' on game object ID: {MainWindowViewModel.GameObject["gameObject"]}");
            await WwiseClient.client?.Call(ak.soundengine.postEvent, args);
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[EventViewModel] Failed to play event '{Name}' (ID: {Id}): {ex.Message}");
        }
    }

}
