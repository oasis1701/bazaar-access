using System.Collections.Generic;
using System.Reflection;
using BazaarAccess.Accessibility;
using BazaarAccess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BazaarAccess.UI.Login;

/// <summary>
/// Clase base para UIs de login/cuenta que necesitan manejar campos de texto.
/// Gestiona el modo edición para campos de texto y bloquea la navegación cuando se está editando.
/// </summary>
public abstract class LoginBaseUI : BaseUI
{
    protected readonly List<TextFieldOption> _textFieldOptions = new List<TextFieldOption>();
    private bool _isInEditMode;

    /// <summary>
    /// Indica si algún campo de texto está en modo edición.
    /// </summary>
    public bool IsInEditMode => _isInEditMode;

    protected LoginBaseUI(Transform root) : base(root) { }

    public override void HandleInput(AccessibleKey key)
    {
        // Si estamos en modo edición, solo responder a Enter (salir) o Escape (cancelar)
        if (_isInEditMode)
        {
            if (key == AccessibleKey.Confirm || key == AccessibleKey.Back)
            {
                // Salir del modo edición
                ExitAllEditModes();
                // Re-leer la opción actual
                Menu.ReadCurrentOption();
            }
            // Todas las demás teclas son consumidas por el input field de Unity
            return;
        }

        // No estamos en modo edición - manejar navegación normalmente
        base.HandleInput(key);
    }

    /// <summary>
    /// Añade un campo de texto al menú.
    /// </summary>
    protected void AddTextField(string label, TMP_InputField inputField)
    {
        if (inputField == null) return;

        var textFieldOption = new TextFieldOption(label, inputField);
        textFieldOption.OnEditModeChanged += OnEditModeChanged;
        _textFieldOptions.Add(textFieldOption);

        // Añadir al menú con acción Enter para alternar modo edición
        Menu.AddOption(
            () => textFieldOption.GetDisplayText(),
            () => textFieldOption.ToggleEditMode()
        );
    }

    /// <summary>
    /// Obtiene un TMP_InputField de la vista via reflexión.
    /// </summary>
    protected TMP_InputField GetInputField(object view, string fieldName)
    {
        if (view == null) return null;
        var field = view.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(view) as TMP_InputField;
    }

    /// <summary>
    /// Obtiene un Toggle de la vista via reflexión.
    /// </summary>
    protected Toggle GetToggle(object view, string fieldName)
    {
        if (view == null) return null;
        var field = view.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(view) as Toggle;
    }

    /// <summary>
    /// Obtiene un BazaarButtonController de la vista via reflexión.
    /// </summary>
    protected object GetBazaarButton(object view, string fieldName)
    {
        if (view == null) return null;
        var field = view.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(view);
    }

    /// <summary>
    /// Obtiene un Button de la vista via reflexión.
    /// </summary>
    protected Button GetUnityButton(object view, string fieldName)
    {
        if (view == null) return null;
        var field = view.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(view) as Button;
    }

    /// <summary>
    /// Obtiene un TMP_Text de la vista via reflexión.
    /// </summary>
    protected TMP_Text GetText(object view, string fieldName)
    {
        if (view == null) return null;
        var field = view.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(view) as TMP_Text;
    }

    /// <summary>
    /// Intenta hacer click en un BazaarButtonController.
    /// </summary>
    protected bool ClickBazaarButton(object bazaarButton)
    {
        if (bazaarButton == null) return false;

        // BazaarButtonController tiene onClick que es un UnityEvent
        var onClickField = bazaarButton.GetType().GetField("onClick",
            BindingFlags.Public | BindingFlags.Instance);
        if (onClickField == null)
        {
            // Intentar como propiedad
            var onClickProp = bazaarButton.GetType().GetProperty("onClick",
                BindingFlags.Public | BindingFlags.Instance);
            if (onClickProp != null)
            {
                var onClick = onClickProp.GetValue(bazaarButton) as UnityEngine.Events.UnityEvent;
                onClick?.Invoke();
                return true;
            }
            return false;
        }

        var onClickEvent = onClickField.GetValue(bazaarButton) as UnityEngine.Events.UnityEvent;
        onClickEvent?.Invoke();
        return true;
    }

    /// <summary>
    /// Comprueba si un BazaarButtonController es interactable.
    /// </summary>
    protected bool IsBazaarButtonInteractable(object bazaarButton)
    {
        if (bazaarButton == null) return false;

        // Intentar obtener la propiedad interactable
        var interactableProp = bazaarButton.GetType().GetProperty("interactable",
            BindingFlags.Public | BindingFlags.Instance);
        if (interactableProp != null)
        {
            return (bool)interactableProp.GetValue(bazaarButton);
        }

        // Intentar como campo
        var interactableField = bazaarButton.GetType().GetField("interactable",
            BindingFlags.Public | BindingFlags.Instance);
        if (interactableField != null)
        {
            return (bool)interactableField.GetValue(bazaarButton);
        }

        // Fallback: asumir que está activo
        return true;
    }

    /// <summary>
    /// Obtiene el texto de un BazaarButtonController.
    /// </summary>
    protected string GetBazaarButtonText(object bazaarButton)
    {
        if (bazaarButton == null) return "";

        // Intentar obtener ButtonText
        var buttonTextProp = bazaarButton.GetType().GetProperty("ButtonText",
            BindingFlags.Public | BindingFlags.Instance);
        if (buttonTextProp != null)
        {
            var tmpText = buttonTextProp.GetValue(bazaarButton) as TMP_Text;
            if (tmpText != null)
                return tmpText.text;
        }

        // Intentar buscar TMP_Text en los hijos
        if (bazaarButton is Component comp)
        {
            var tmpText = comp.GetComponentInChildren<TMP_Text>();
            if (tmpText != null)
                return tmpText.text;
        }

        return "";
    }

    /// <summary>
    /// Añade un botón BazaarButtonController al menú.
    /// </summary>
    protected void AddBazaarButton(object view, string fieldName, string fallbackText)
    {
        var button = GetBazaarButton(view, fieldName);
        if (button == null) return;

        Menu.AddOption(
            () =>
            {
                string text = GetBazaarButtonText(button);
                if (string.IsNullOrEmpty(text)) text = fallbackText;
                if (!IsBazaarButtonInteractable(button))
                    text += " (disabled)";
                return text;
            },
            () =>
            {
                if (IsBazaarButtonInteractable(button))
                    ClickBazaarButton(button);
                else
                    TolkWrapper.Speak("Button is disabled");
            }
        );
    }

    /// <summary>
    /// Añade un botón Unity Button al menú.
    /// </summary>
    protected void AddUnityButton(object view, string fieldName, string fallbackText)
    {
        var button = GetUnityButton(view, fieldName);
        if (button == null) return;

        Menu.AddOption(
            () =>
            {
                var tmpText = button.GetComponentInChildren<TMP_Text>();
                string text = tmpText?.text ?? fallbackText;
                if (!button.interactable)
                    text += " (disabled)";
                return text;
            },
            () =>
            {
                if (button.interactable)
                    button.onClick.Invoke();
                else
                    TolkWrapper.Speak("Button is disabled");
            }
        );
    }

    /// <summary>
    /// Añade un toggle al menú.
    /// </summary>
    protected void AddToggle(object view, string fieldName, string label)
    {
        var toggle = GetToggle(view, fieldName);
        if (toggle == null) return;

        Menu.AddOption(
            () => $"{label}: {(toggle.isOn ? "on" : "off")}",
            () => ToggleAndAnnounce(toggle),
            null,
            (dir) => ToggleAndAnnounce(toggle)
        );
    }

    private void ToggleAndAnnounce(Toggle toggle)
    {
        toggle.isOn = !toggle.isOn;
        TolkWrapper.Speak(toggle.isOn ? "on" : "off");
    }

    private void OnEditModeChanged(bool isEditing)
    {
        _isInEditMode = isEditing;
    }

    private void ExitAllEditModes()
    {
        foreach (var textField in _textFieldOptions)
        {
            if (textField.IsEditing)
            {
                textField.ExitEditMode();
            }
        }
        _isInEditMode = false;
    }

    /// <summary>
    /// Busca texto de label cerca de un input field.
    /// </summary>
    protected string FindLabelForInput(TMP_InputField inputField)
    {
        if (inputField == null) return "Field";

        // Intentar placeholder text
        if (inputField.placeholder is TMP_Text placeholder &&
            !string.IsNullOrWhiteSpace(placeholder.text))
        {
            return placeholder.text.Trim();
        }

        // Intentar sibling TMP_Text
        var parent = inputField.transform.parent;
        if (parent != null)
        {
            var texts = parent.GetComponentsInChildren<TMP_Text>(true);
            foreach (var txt in texts)
            {
                if (txt.gameObject != inputField.gameObject &&
                    txt != inputField.textComponent &&
                    txt != inputField.placeholder as TMP_Text &&
                    !string.IsNullOrWhiteSpace(txt.text))
                {
                    return txt.text.Trim();
                }
            }
        }

        return "Field";
    }
}
