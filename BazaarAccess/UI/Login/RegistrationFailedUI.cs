using BazaarAccess.Accessibility;
using UnityEngine;
using System.Reflection;

namespace BazaarAccess.UI.Login;

/// <summary>
/// UI accesible para la pantalla de error de registro.
/// </summary>
public class RegistrationFailedUI : BaseUI
{
    private readonly object _view;

    public override string UIName => "Registration Failed";

    public RegistrationFailedUI(Transform root, object view) : base(root)
    {
        _view = view;
    }

    protected override void BuildMenu()
    {
        // Continue button
        AddBazaarButton("continueButton", "Try Again");
    }

    private void AddBazaarButton(string fieldName, string fallbackText)
    {
        var field = _view.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);
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

        var buttonTextProp = bazaarButton.GetType().GetProperty("ButtonText",
            BindingFlags.Public | BindingFlags.Instance);
        if (buttonTextProp != null)
        {
            var tmpText = buttonTextProp.GetValue(bazaarButton) as TMPro.TMP_Text;
            if (tmpText != null && !string.IsNullOrWhiteSpace(tmpText.text))
                return tmpText.text;
        }

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
            BindingFlags.Public | BindingFlags.Instance);
        if (onClickField != null)
        {
            var onClickEvent = onClickField.GetValue(bazaarButton) as UnityEngine.Events.UnityEvent;
            onClickEvent?.Invoke();
        }
    }

    protected override void OnBack()
    {
        // Intentar de nuevo al presionar back
        ClickButtonByName("continueButton");
    }
}
