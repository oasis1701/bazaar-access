using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BazaarAccess.Accessibility;

/// <summary>
/// Base class for accessible screens.
/// Provides helpers for interacting with the game UI.
/// </summary>
public abstract class BaseScreen : IAccessibleScreen
{
    protected readonly Transform Root;
    protected readonly AccessibleMenu Menu;

    public abstract string ScreenName { get; }

    protected BaseScreen(Transform root)
    {
        Root = root;
        Menu = new AccessibleMenu(ScreenName);
        BuildMenu();
    }

    /// <summary>
    /// Builds the menu with screen options.
    /// Override in derived classes.
    /// </summary>
    protected abstract void BuildMenu();

    public virtual void HandleInput(AccessibleKey key)
    {
        Menu.HandleInput(key);
    }

    public virtual string GetHelp()
    {
        return Menu.GetHelp();
    }

    public virtual void OnFocus()
    {
        // Debug: list all buttons found
        LogAllButtons();
        Menu.StartReading(announceMenuName: true);
    }

    /// <summary>
    /// Debug: Lists all buttons in the UI.
    /// </summary>
    protected void LogAllButtons()
    {
        if (Root == null) return;

        var buttons = Root.GetComponentsInChildren<Button>(true);
        Plugin.Logger.LogInfo($"=== Buttons in {ScreenName} ({buttons.Length} total) ===");

        foreach (var button in buttons)
        {
            string text = GetButtonText(button);
            string active = button.gameObject.activeInHierarchy ? "active" : "inactive";
            string interactable = button.interactable ? "interactable" : "non-interactable";
            Plugin.Logger.LogInfo($"  [{button.gameObject.name}] text='{text}' ({active}, {interactable})");
        }

        Plugin.Logger.LogInfo("=== End buttons ===");
    }

    public virtual bool IsValid()
    {
        if (Root == null) return false;
        if (!Root.gameObject.activeInHierarchy) return false;
        return true;
    }

    // --- Helpers for interacting with game UI ---

    /// <summary>
    /// Finds and clicks a button by its visible text.
    /// </summary>
    protected bool ClickButtonByText(string text)
    {
        var button = FindButtonByText(text);
        if (button != null && button.interactable)
        {
            Plugin.Logger.LogInfo($"Click by text: {text}");
            button.onClick.Invoke();
            return true;
        }

        Plugin.Logger.LogWarning($"Button not found by text: {text}");
        return false;
    }

    /// <summary>
    /// Finds and clicks a button by GameObject name.
    /// </summary>
    protected bool ClickButtonByName(string name)
    {
        var button = FindButtonByName(name);
        if (button != null && button.interactable)
        {
            Plugin.Logger.LogInfo($"Click by name: {name}");
            button.onClick.Invoke();
            return true;
        }

        Plugin.Logger.LogWarning($"Button not found by name: {name}");
        return false;
    }

    /// <summary>
    /// Finds a button by its visible text (case-insensitive).
    /// </summary>
    protected Button FindButtonByText(string text)
    {
        if (Root == null) return null;

        var buttons = Root.GetComponentsInChildren<Button>(true)
            .Where(b => b.gameObject.activeInHierarchy);

        foreach (var button in buttons)
        {
            string buttonText = GetButtonText(button);
            if (buttonText != null && buttonText.Equals(text, System.StringComparison.OrdinalIgnoreCase))
            {
                return button;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds a button by GameObject name (case-insensitive).
    /// </summary>
    protected Button FindButtonByName(string name)
    {
        if (Root == null) return null;

        return Root.GetComponentsInChildren<Button>(true)
            .FirstOrDefault(b => b.gameObject.activeInHierarchy &&
                                 b.gameObject.name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets button text by GameObject name.
    /// Useful for creating dynamic labels.
    /// </summary>
    protected string GetButtonTextByName(string name)
    {
        var button = FindButtonByName(name);
        if (button == null) return name; // Fallback to name

        string text = GetButtonText(button);
        return string.IsNullOrWhiteSpace(text) ? name : text;
    }

    /// <summary>
    /// Gets the text of a button.
    /// </summary>
    protected string GetButtonText(Button button)
    {
        // Try BazaarButtonController first
        var bazaarButton = button as BazaarButtonController;
        if (bazaarButton != null && bazaarButton.ButtonText != null)
        {
            return bazaarButton.ButtonText.text?.Trim();
        }

        // TMP_Text en hijos
        var tmp = button.GetComponentInChildren<TMP_Text>();
        if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
        {
            return tmp.text.Trim();
        }

        // Text legacy
        var legacyText = button.GetComponentInChildren<Text>();
        if (legacyText != null && !string.IsNullOrWhiteSpace(legacyText.text))
        {
            return legacyText.text.Trim();
        }

        return null;
    }

    /// <summary>
    /// Finds a Toggle by name.
    /// </summary>
    protected Toggle FindToggle(string name)
    {
        if (Root == null) return null;

        return Root.GetComponentsInChildren<Toggle>(true)
            .FirstOrDefault(t => t.gameObject.activeInHierarchy &&
                                 t.gameObject.name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds a Slider by name.
    /// </summary>
    protected Slider FindSlider(string name)
    {
        if (Root == null) return null;

        return Root.GetComponentsInChildren<Slider>(true)
            .FirstOrDefault(s => s.gameObject.activeInHierarchy &&
                                 s.gameObject.name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
    }
}
