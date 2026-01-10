using BazaarAccess.Accessibility;
using BazaarAccess.Core;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

namespace BazaarAccess.UI.Login;

/// <summary>
/// UI accesible para la aceptación de términos de servicio.
/// </summary>
public class CreateAccountTermsUI : BaseUI
{
    private readonly object _view;

    public override string UIName => "Create Account - Terms";

    public CreateAccountTermsUI(Transform root, object view) : base(root)
    {
        _view = view;
    }

    protected override void BuildMenu()
    {
        // ToS toggle
        var tosToggle = GetToggle("tosToggle");
        if (tosToggle != null)
        {
            Menu.AddOption(
                () => $"Terms of Service: {(tosToggle.isOn ? "accepted" : "not accepted")}",
                () => ToggleAndAnnounce(tosToggle),
                null,
                (dir) => ToggleAndAnnounce(tosToggle)
            );
        }

        // EULA toggle
        var eulaToggle = GetToggle("eulaToggle");
        if (eulaToggle != null)
        {
            Menu.AddOption(
                () => $"End User License Agreement: {(eulaToggle.isOn ? "accepted" : "not accepted")}",
                () => ToggleAndAnnounce(eulaToggle),
                null,
                (dir) => ToggleAndAnnounce(eulaToggle)
            );
        }

        // Promo toggle (opcional)
        var promoToggle = GetToggle("promoToggle");
        if (promoToggle != null)
        {
            Menu.AddOption(
                () => $"Marketing emails: {(promoToggle.isOn ? "on" : "off")}",
                () => ToggleAndAnnounce(promoToggle),
                null,
                (dir) => ToggleAndAnnounce(promoToggle)
            );
        }

        // Continue button
        AddBazaarButton("continueButton", "Continue");
    }

    private Toggle GetToggle(string fieldName)
    {
        var field = _view.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(_view) as Toggle;
    }

    private void AddBazaarButton(string fieldName, string fallbackText)
    {
        var field = _view.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        var button = field?.GetValue(_view);
        if (button == null) return;

        Menu.AddOption(
            () =>
            {
                string text = GetButtonText(button) ?? fallbackText;
                if (!IsButtonInteractable(button))
                    text += " (disabled - accept required terms)";
                return text;
            },
            () =>
            {
                if (IsButtonInteractable(button))
                    ClickButton(button);
                else
                    TolkWrapper.Speak("Please accept Terms of Service and EULA");
            }
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

    private bool IsButtonInteractable(object bazaarButton)
    {
        if (bazaarButton == null) return false;

        var interactableProp = bazaarButton.GetType().GetProperty("interactable",
            BindingFlags.Public | BindingFlags.Instance);
        if (interactableProp != null)
        {
            return (bool)interactableProp.GetValue(bazaarButton);
        }

        return true;
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

    private void ToggleAndAnnounce(Toggle toggle)
    {
        toggle.isOn = !toggle.isOn;
        TolkWrapper.Speak(toggle.isOn ? "accepted" : "not accepted");
    }

    protected override void OnBack()
    {
        // Volver a la pantalla anterior
    }
}
