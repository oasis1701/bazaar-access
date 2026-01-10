using BazaarAccess.Accessibility;
using UnityEngine;

namespace BazaarAccess.UI.Login;

/// <summary>
/// UI accesible para la pantalla inicial (Link Account / Create Account).
/// </summary>
public class LandingUI : BaseUI
{
    private readonly object _view;

    public override string UIName => "Welcome";

    public LandingUI(Transform root, object view) : base(root)
    {
        _view = view;
    }

    protected override void BuildMenu()
    {
        // Link Account button
        AddBazaarButtonFromView("linkAccountButton", "Link Account");

        // Create Account button
        AddBazaarButtonFromView("createAccountButton", "Create Account");
    }

    private void AddBazaarButtonFromView(string fieldName, string fallbackText)
    {
        var field = _view.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var button = field?.GetValue(_view);
        if (button == null) return;

        Menu.AddOption(
            () => GetButtonText(button) ?? fallbackText,
            () => ClickButton(button)
        );
    }

    private string GetButtonText(object bazaarButton)
    {
        if (bazaarButton == null) return null;

        // Intentar obtener ButtonText
        var buttonTextProp = bazaarButton.GetType().GetProperty("ButtonText",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (buttonTextProp != null)
        {
            var tmpText = buttonTextProp.GetValue(bazaarButton) as TMPro.TMP_Text;
            if (tmpText != null && !string.IsNullOrWhiteSpace(tmpText.text))
                return tmpText.text;
        }

        // Intentar buscar TMP_Text en los hijos
        if (bazaarButton is Component comp)
        {
            var tmpText = comp.GetComponentInChildren<TMPro.TMP_Text>();
            if (tmpText != null && !string.IsNullOrWhiteSpace(tmpText.text))
                return tmpText.text;
        }

        return null;
    }

    private void ClickButton(object bazaarButton)
    {
        if (bazaarButton == null) return;

        var onClickField = bazaarButton.GetType().GetField("onClick",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (onClickField != null)
        {
            var onClickEvent = onClickField.GetValue(bazaarButton) as UnityEngine.Events.UnityEvent;
            onClickEvent?.Invoke();
        }
    }

    protected override void OnBack()
    {
        // No hay back desde la landing - es la primera pantalla
    }
}
