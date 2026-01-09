using System.Collections.Generic;
using System.Linq;
using DavyKager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BazaarAccess;

public static class MenuNavigator
{
    private static List<Selectable> _selectables = new List<Selectable>();
    private static int _selectedIndex = 0;
    private static Transform _currentMenuRoot = null;
    private static string _currentView = "";

    // Para restaurar el menú cuando se cierra un popup
    private static Transform _previousMenuRoot = null;
    private static string _previousView = "";
    private static int _previousSelectedIndex = 0;

    public static void SetMenuRoot(Transform root, string viewName)
    {
        _selectables.Clear();
        _selectedIndex = 0;
        _currentMenuRoot = root;
        _currentView = viewName;

        Plugin.Logger.LogInfo($"Menú cambiado a: {viewName}");
    }

    public static void SavePreviousMenu()
    {
        _previousMenuRoot = _currentMenuRoot;
        _previousView = _currentView;
        _previousSelectedIndex = _selectedIndex;
        Plugin.Logger.LogInfo($"Guardado menú anterior: {_previousView}");
    }

    public static void RestorePreviousMenu()
    {
        if (_previousMenuRoot != null && !string.IsNullOrEmpty(_previousView))
        {
            // Verificar que el menú anterior sigue siendo válido
            if (_previousMenuRoot.gameObject.activeInHierarchy)
            {
                Plugin.Logger.LogInfo($"Restaurando menú: {_previousView}");
                _currentMenuRoot = _previousMenuRoot;
                _currentView = _previousView;
                _selectables.Clear();
                _selectedIndex = _previousSelectedIndex;

                // Anunciar que volvimos al menú anterior
                AnnounceMenuTitle(_currentMenuRoot);
            }
            else
            {
                Plugin.Logger.LogInfo($"Menú anterior {_previousView} ya no es válido, limpiando");
                _currentMenuRoot = null;
                _currentView = "";
                _selectables.Clear();
                _selectedIndex = 0;
            }

            // Limpiar datos del menú anterior
            _previousMenuRoot = null;
            _previousView = "";
            _previousSelectedIndex = 0;
        }
    }

    public static void AnnounceMenuTitle(Transform menuRoot)
    {
        if (menuRoot == null) return;

        // Buscar título en el menú
        var texts = menuRoot.GetComponentsInChildren<TMP_Text>()
            .Where(t => t.gameObject.activeInHierarchy)
            .OrderByDescending(t => t.transform.position.y)
            .ThenByDescending(t => t.fontSize)
            .ToList();

        foreach (var text in texts)
        {
            string content = text.text?.Trim();
            if (!string.IsNullOrWhiteSpace(content) && content.Length > 1 && content.Length < 50)
            {
                if (content.Any(char.IsLetter))
                {
                    Tolk.Output(content);
                    return;
                }
            }
        }

        // Fallback
        Tolk.Output(CleanName(menuRoot.name));
    }

    public static void RefreshSelectables()
    {
        _selectables.Clear();

        if (_currentMenuRoot == null)
        {
            // Si no hay raíz definida, buscar en toda la escena pero con filtros más estrictos
            _selectables = Object.FindObjectsOfType<Selectable>()
                .Where(s => s != null && s.gameObject.activeInHierarchy)
                .Where(s => s is Button || s is Toggle || s is Slider || s is TMP_Dropdown)
                .Where(s => IsValidSelectable(s))
                .OrderBy(s => GetSortOrder(s))
                .ToList();
        }
        else
        {
            // Buscar en todos los hijos del menú actual (incluyendo layouts anidados)
            _selectables = _currentMenuRoot.GetComponentsInChildren<Selectable>(true)
                .Where(s => s != null && s.gameObject.activeInHierarchy)
                .Where(s => s is Button || s is Toggle || s is Slider || s is TMP_Dropdown)
                .Where(s => IsValidSelectable(s))
                .OrderBy(s => GetSortOrder(s))
                .ToList();
        }

        if (_selectedIndex >= _selectables.Count)
            _selectedIndex = 0;

        Plugin.Logger.LogInfo($"RefreshSelectables: {_selectables.Count} elementos en {_currentView}");

        // Log detallado para depuración
        foreach (var s in _selectables)
        {
            string typeName = s.GetType().Name;
            string text = GetTextFromSelectable(s);
            Plugin.Logger.LogInfo($"  [{typeName}] {s.gameObject.name}: '{text}'");
        }
    }

    private static bool IsValidSelectable(Selectable s)
    {
        string name = s.gameObject.name.ToLower();

        // Ignorar elementos decorativos
        string[] ignored = { "background", "bg", "frame", "border", "mask", "template", "viewport", "scrollbar" };
        foreach (var ig in ignored)
        {
            if (name == ig) return false;
        }

        // Para botones, verificar que tienen texto
        if (s is Button)
        {
            string text = GetTextFromSelectable(s);
            if (string.IsNullOrWhiteSpace(text)) return false;
            if (text.Length == 1 && !char.IsLetterOrDigit(text[0])) return false;
        }

        return true;
    }

    private static float GetSortOrder(Selectable s)
    {
        var rect = s.GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector3 pos = rect.position;
            return -pos.y * 10000 + pos.x;
        }
        return 0;
    }

    public static void Navigate(int direction)
    {
        // Verificar si el menú actual sigue siendo válido
        if (!IsCurrentMenuValid())
        {
            Plugin.Logger.LogInfo("Menú actual no válido, limpiando...");
            _selectables.Clear();
            _currentMenuRoot = null;
            _currentView = "";
            return; // No navegar si no hay menú válido
        }

        // Limpiar elementos que ya no son válidos (solo verificar que existen y están activos)
        _selectables = _selectables
            .Where(s => s != null && s.gameObject.activeInHierarchy)
            .ToList();

        if (_selectables.Count == 0)
        {
            RefreshSelectables();
            if (_selectables.Count == 0) return; // No decir nada
        }

        _selectedIndex += direction;
        if (_selectedIndex < 0) _selectedIndex = _selectables.Count - 1;
        if (_selectedIndex >= _selectables.Count) _selectedIndex = 0;

        ReadCurrentSelectable();
    }

    private static bool IsCurrentMenuValid()
    {
        // Si no hay menú definido, no es válido
        if (_currentMenuRoot == null) return false;

        // Verificar que el GameObject del menú sigue activo
        if (!_currentMenuRoot.gameObject.activeInHierarchy) return false;

        // Verificar que tiene un Canvas activo (está visible en pantalla)
        var canvas = _currentMenuRoot.GetComponentInParent<Canvas>();
        if (canvas != null && !canvas.enabled) return false;

        return true;
    }

    public static void RefreshAndRead()
    {
        // Verificar si el menú actual sigue siendo válido
        if (!IsCurrentMenuValid())
        {
            Plugin.Logger.LogInfo("Menú actual no válido en RefreshAndRead, limpiando...");
            _selectables.Clear();
            _currentMenuRoot = null;
            _currentView = "";
            return;
        }

        RefreshSelectables();
        if (_selectables.Count > 0)
        {
            _selectedIndex = 0;
            ReadCurrentSelectable();
        }
        // No decir nada si no hay elementos
    }

    private static void ReadCurrentSelectable()
    {
        if (_selectables.Count == 0 || _selectedIndex >= _selectables.Count) return;

        var selectable = _selectables[_selectedIndex];
        if (selectable == null) return;

        string label = GetLabelForSelectable(selectable);
        string output = label;

        // Verificar Button primero (más específico), ya que algunos objetos pueden tener ambos Button y Toggle
        if (selectable is Button)
        {
            // Es un botón, no añadir estado
        }
        else if (selectable is Toggle toggle)
        {
            output = $"{label}: {(toggle.isOn ? "activado" : "desactivado")}";
        }
        else if (selectable is Slider slider)
        {
            int percent = Mathf.RoundToInt((slider.value - slider.minValue) / (slider.maxValue - slider.minValue) * 100);
            output = $"{label}: {percent}%";
        }
        else if (selectable is TMP_Dropdown dropdown)
        {
            string value = dropdown.captionText != null ? dropdown.captionText.text : "";
            if (!string.IsNullOrEmpty(value))
                output = $"{label}: {value}";
        }

        // Indicar si el elemento está deshabilitado
        if (!selectable.interactable)
        {
            output = $"{output}, deshabilitado";
        }

        Tolk.Output(output);
    }

    public static void ActivateSelected()
    {
        // Verificar si el menú actual sigue siendo válido
        if (!IsCurrentMenuValid())
        {
            _selectables.Clear();
            return;
        }

        if (_selectables.Count == 0 || _selectedIndex >= _selectables.Count) return;

        var selectable = _selectables[_selectedIndex];
        if (selectable == null) return;

        // Si no es interactable, indicarlo
        if (!selectable.interactable)
        {
            Tolk.Output("no disponible");
            return;
        }

        // Verificar Button primero (más específico)
        if (selectable is Button button)
        {
            Plugin.Logger.LogInfo($"Activando botón: {selectable.gameObject.name}");
            button.onClick.Invoke();
            // Refrescar después de activar un botón (puede haber cambiado el menú)
            _selectables.Clear();
        }
        else if (selectable is Toggle toggle)
        {
            toggle.isOn = !toggle.isOn;
            Tolk.Output(toggle.isOn ? "activado" : "desactivado");
        }
        else if (selectable is Slider slider)
        {
            int percent = Mathf.RoundToInt((slider.value - slider.minValue) / (slider.maxValue - slider.minValue) * 100);
            Tolk.Output($"{percent}%");
        }
        else if (selectable is TMP_Dropdown dropdown)
        {
            string value = dropdown.captionText != null ? dropdown.captionText.text : "";
            if (!string.IsNullOrEmpty(value))
                Tolk.Output(value);
        }
    }

    public static void AdjustValue(int direction)
    {
        // Verificar si el menú actual sigue siendo válido
        if (!IsCurrentMenuValid())
        {
            _selectables.Clear();
            return;
        }

        if (_selectables.Count == 0 || _selectedIndex >= _selectables.Count) return;

        var selectable = _selectables[_selectedIndex];
        if (selectable == null) return;

        if (selectable is Slider slider)
        {
            float step = (slider.maxValue - slider.minValue) * 0.1f;
            slider.value = Mathf.Clamp(slider.value + (step * direction), slider.minValue, slider.maxValue);
            int percent = Mathf.RoundToInt((slider.value - slider.minValue) / (slider.maxValue - slider.minValue) * 100);
            Tolk.Output($"{percent}%");
        }
        else if (selectable is TMP_Dropdown dropdown)
        {
            int newIndex = dropdown.value + direction;
            if (newIndex < 0) newIndex = 0;
            dropdown.value = Mathf.Max(0, newIndex);
            string value = dropdown.captionText != null ? dropdown.captionText.text : "";
            if (!string.IsNullOrEmpty(value))
                Tolk.Output(value);
        }
        else if (selectable is Toggle toggle)
        {
            toggle.isOn = !toggle.isOn;
            Tolk.Output(toggle.isOn ? "activado" : "desactivado");
        }
    }

    private static string GetLabelForSelectable(Selectable selectable)
    {
        var parent = selectable.transform.parent;
        if (parent != null)
        {
            int siblingIndex = selectable.transform.GetSiblingIndex();
            for (int i = siblingIndex - 1; i >= 0 && i >= siblingIndex - 3; i--)
            {
                var sibling = parent.GetChild(i);
                var siblingTmp = sibling.GetComponent<TMP_Text>();
                if (siblingTmp != null && IsValidLabel(siblingTmp.text))
                {
                    return CleanLabel(siblingTmp.text);
                }

                var siblingText = sibling.GetComponent<Text>();
                if (siblingText != null && IsValidLabel(siblingText.text))
                {
                    return CleanLabel(siblingText.text);
                }
            }
        }

        string innerText = GetTextFromSelectable(selectable);
        if (IsValidLabel(innerText))
        {
            return CleanLabel(innerText);
        }

        if (parent != null)
        {
            var parentTmp = parent.GetComponent<TMP_Text>();
            if (parentTmp != null && IsValidLabel(parentTmp.text))
            {
                return CleanLabel(parentTmp.text);
            }
        }

        return CleanName(selectable.gameObject.name);
    }

    private static bool IsValidLabel(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (text.Length <= 1) return false;
        if (!text.Any(char.IsLetter)) return false;
        return true;
    }

    private static string GetTextFromSelectable(Selectable selectable)
    {
        // Intentar obtener ButtonText de BazaarButtonController primero
        var bazaarButton = selectable as BazaarButtonController;
        if (bazaarButton != null && bazaarButton.ButtonText != null && !string.IsNullOrWhiteSpace(bazaarButton.ButtonText.text))
        {
            return bazaarButton.ButtonText.text;
        }

        var tmp = selectable.GetComponentInChildren<TMP_Text>();
        if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
        {
            return tmp.text;
        }

        var text = selectable.GetComponentInChildren<Text>();
        if (text != null && !string.IsNullOrWhiteSpace(text.text))
        {
            return text.text;
        }

        return "";
    }

    private static string CleanLabel(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        text = text.Trim();
        if (text.EndsWith(":"))
            text = text.Substring(0, text.Length - 1).Trim();
        return text;
    }

    private static string CleanName(string name)
    {
        var result = System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");

        string[] prefixes = { "Btn", "Button", "Toggle", "Slider", "Dropdown", "_", "TMP_" };
        foreach (var prefix in prefixes)
        {
            if (result.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
            {
                result = result.Substring(prefix.Length).Trim();
            }
        }

        return result.Trim();
    }
}
