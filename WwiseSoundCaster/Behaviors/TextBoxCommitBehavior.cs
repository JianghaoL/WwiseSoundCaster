using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace WwiseSoundCaster.Behaviors;

/// <summary>
/// Attached behavior for TextBox inline editing that:
///   1. Auto-focuses and selects all text when the TextBox becomes visible.
///   2. Executes a commit command when the TextBox loses focus.
///
/// Designed for MVVM inline editing scenarios (e.g., RTPC value editing)
/// where no code-behind event handlers are allowed.
///
/// Usage in XAML:
///   beh:TextBoxCommitBehavior.CommitCommand="{Binding CommitEditCommand}"
/// </summary>
public static class TextBoxCommitBehavior
{
    public static readonly AttachedProperty<ICommand?> CommitCommandProperty =
        AvaloniaProperty.RegisterAttached<TextBox, ICommand?>(
            "CommitCommand", typeof(TextBoxCommitBehavior));

    public static void SetCommitCommand(TextBox element, ICommand? value) =>
        element.SetValue(CommitCommandProperty, value);

    public static ICommand? GetCommitCommand(TextBox element) =>
        element.GetValue(CommitCommandProperty);

    static TextBoxCommitBehavior()
    {
        CommitCommandProperty.Changed.AddClassHandler<TextBox>(OnCommitCommandChanged);

        // Watch IsVisible changes on all TextBoxes that have a CommitCommand set.
        // The handler filters by checking for the attached property presence.
        Visual.IsVisibleProperty.Changed.AddClassHandler<TextBox>(OnIsVisibleChanged);
    }

    private static void OnCommitCommandChanged(TextBox textBox, AvaloniaPropertyChangedEventArgs e)
    {
        textBox.LostFocus -= OnLostFocus;

        if (e.NewValue is ICommand)
            textBox.LostFocus += OnLostFocus;
    }

    /// <summary>
    /// When a TextBox with our CommitCommand becomes visible,
    /// automatically focus it and select all text for immediate editing.
    /// </summary>
    private static void OnIsVisibleChanged(TextBox textBox, AvaloniaPropertyChangedEventArgs e)
    {
        // Only act on TextBoxes that have our CommitCommand attached
        if (GetCommitCommand(textBox) is null) return;

        if (e.NewValue is true)
        {
            // Defer focus to ensure the TextBox is fully rendered
            Dispatcher.UIThread.Post(() =>
            {
                textBox.Focus();
                textBox.SelectAll();
            }, DispatcherPriority.Input);
        }
    }

    private static void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            var command = GetCommitCommand(textBox);
            command?.Execute(null);
        }
    }
}
