using System;

namespace BazaarAccess.Accessibility;

/// <summary>
/// Represents an option in an accessible menu.
/// Follows the Hearthstone pattern with delegates for dynamic text.
/// </summary>
public class MenuOption
{
    /// <summary>
    /// Delegate to get the option text (allows dynamic text).
    /// </summary>
    public Func<string> GetText { get; }

    /// <summary>
    /// Action on confirm (Enter).
    /// </summary>
    public Action OnConfirm { get; }

    /// <summary>
    /// Action when reading the option (optional, for special behavior).
    /// </summary>
    public Action OnRead { get; }

    /// <summary>
    /// Action for adjusting with left/right (for sliders/toggles).
    /// </summary>
    public Action<int> OnAdjust { get; }

    /// <summary>
    /// Delegate to check if option is visible (optional).
    /// </summary>
    public Func<bool> IsVisible { get; }

    /// <summary>
    /// Hotkey (optional).
    /// </summary>
    public string Hotkey { get; }

    // Constructor with static text
    public MenuOption(string text, Action onConfirm, Action onRead = null, Action<int> onAdjust = null, string hotkey = null, Func<bool> isVisible = null)
        : this(() => text, onConfirm, onRead, onAdjust, hotkey, isVisible)
    {
    }

    // Constructor with dynamic text
    public MenuOption(Func<string> getText, Action onConfirm, Action onRead = null, Action<int> onAdjust = null, string hotkey = null, Func<bool> isVisible = null)
    {
        GetText = getText ?? (() => "");
        OnConfirm = onConfirm;
        OnRead = onRead;
        OnAdjust = onAdjust;
        Hotkey = hotkey;
        IsVisible = isVisible ?? (() => true);
    }
}
