using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace WwiseSoundCaster.Behaviors;

/// <summary>
/// Attached behavior that binds a command to the DoubleTapped event.
/// Used in MVVM to trigger editing mode on double-click without code-behind.
///
/// Usage in XAML:
///   beh:DoubleTapBehavior.Command="{Binding SomeCommand}"
/// </summary>
public static class DoubleTapBehavior
{
    public static readonly AttachedProperty<ICommand?> CommandProperty =
        AvaloniaProperty.RegisterAttached<Control, ICommand?>(
            "Command", typeof(DoubleTapBehavior));

    public static void SetCommand(Control element, ICommand? value) =>
        element.SetValue(CommandProperty, value);

    public static ICommand? GetCommand(Control element) =>
        element.GetValue(CommandProperty);

    static DoubleTapBehavior()
    {
        CommandProperty.Changed.AddClassHandler<Control>(OnCommandChanged);
    }

    private static void OnCommandChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        control.DoubleTapped -= OnDoubleTapped;

        if (e.NewValue is ICommand)
            control.DoubleTapped += OnDoubleTapped;
    }

    private static void OnDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Control control)
        {
            var command = GetCommand(control);
            if (command?.CanExecute(null) == true)
                command.Execute(null);
        }
    }
}
