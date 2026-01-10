using BazaarAccess.Accessibility;
using BazaarAccess.Core;
using UnityEngine;
using System.Reflection;
using TMPro;

namespace BazaarAccess.UI.Login;

/// <summary>
/// UI accesible para la pantalla de acceso denegado.
/// </summary>
public class AccessDeniedUI : BaseUI
{
    private readonly object _view;

    public override string UIName => "Access Denied";

    public AccessDeniedUI(Transform root, object view) : base(root)
    {
        _view = view;
    }

    protected override void BuildMenu()
    {
        // Leer el mensaje de error si existe
        var descriptionField = _view.GetType().GetField("descriptionText",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var descriptionText = descriptionField?.GetValue(_view) as TMP_Text;
        if (descriptionText != null && !string.IsNullOrWhiteSpace(descriptionText.text))
        {
            // Añadir el mensaje como opción de solo lectura
            Menu.AddOption(
                () => descriptionText.text,
                () => TolkWrapper.Speak(descriptionText.text)
            );
        }

        // Continue button
        AddBazaarButton("continueButton", "Continue");
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
            var tmpText = buttonTextProp.GetValue(bazaarButton) as TMP_Text;
            if (tmpText != null && !string.IsNullOrWhiteSpace(tmpText.text))
                return tmpText.text;
        }

        if (bazaarButton is Component comp)
        {
            var tmpText = comp.GetComponentInChildren<TMP_Text>();
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
        // Continuar al presionar back
        ClickButtonByName("continueButton");
    }
}
