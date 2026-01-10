using BazaarAccess.Accessibility;
using UnityEngine;
using System.Reflection;

namespace BazaarAccess.UI.Login;

/// <summary>
/// UI accesible para la pantalla de confirmación de recuperación de contraseña.
/// </summary>
public class ForgotPasswordConfirmUI : BaseUI
{
    private readonly object _view;

    public override string UIName => "Password Reset Sent";

    public ForgotPasswordConfirmUI(Transform root, object view) : base(root)
    {
        _view = view;
    }

    protected override void BuildMenu()
    {
        // Continue button
        AddBazaarButton("continueButton", "Continue");

        // Resend button
        AddBazaarButton("resendButton", "Resend Email");
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
        // Volver a la pantalla anterior
        ClickButtonByName("continueButton");
    }
}
