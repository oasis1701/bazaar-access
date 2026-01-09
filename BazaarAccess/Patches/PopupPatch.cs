using System.Reflection;
using BazaarAccess.Accessibility;
using BazaarAccess.Core;
using BazaarAccess.UI;
using HarmonyLib;
using TheBazaar;
using TMPro;
using UnityEngine;

namespace BazaarAccess.Patches;

/// <summary>
/// Hook en PopupBase.Show para detectar cuando se abre un popup.
/// </summary>
[HarmonyPatch(typeof(PopupBase), nameof(PopupBase.Show))]
public static class PopupShowPatch
{
    private static IAccessibleUI _currentPopupUI;

    static void Postfix(PopupBase __instance)
    {
        string popupName = __instance.GetType().Name;
        Plugin.Logger.LogInfo($"PopupBase.Show: {popupName}");

        try
        {
            // Crear UI accesible según el tipo de popup
            if (__instance is GenericPopup genericPopup)
            {
                _currentPopupUI = GenericPopupUI.CreateFromPopup(genericPopup);
                AccessibilityMgr.ShowUI(_currentPopupUI);
            }
            else
            {
                // Para otros tipos de popup, intentar extraer texto genérico
                string title = ExtractTextFromChild(__instance.transform, "_titleLabel", "Title", "TitleText");
                string message = ExtractTextFromChild(__instance.transform, "_messageLabel", "Message", "MessageText", "Text");

                if (!string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(message))
                {
                    _currentPopupUI = new GenericPopupUI(__instance.transform, title, message);
                    AccessibilityMgr.ShowUI(_currentPopupUI);
                }
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"PopupShowPatch error: {ex.Message}");
        }
    }

    private static string ExtractTextFromChild(Transform root, params string[] names)
    {
        foreach (var name in names)
        {
            var child = root.Find(name);
            if (child != null)
            {
                var tmp = child.GetComponent<TMP_Text>();
                if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
                    return tmp.text.Trim();
            }
        }

        // Buscar por nombre de GameObject
        var allTmp = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTmp)
        {
            foreach (var name in names)
            {
                if (tmp.gameObject.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (!string.IsNullOrWhiteSpace(tmp.text))
                        return tmp.text.Trim();
                }
            }
        }

        return "";
    }

    public static void ClearCurrentUI()
    {
        _currentPopupUI = null;
    }
}

/// <summary>
/// Hook en PopupBase.Hide para detectar cuando se cierra un popup.
/// </summary>
[HarmonyPatch(typeof(PopupBase), nameof(PopupBase.Hide))]
public static class PopupHidePatch
{
    static void Postfix(PopupBase __instance)
    {
        string popupName = __instance.GetType().Name;
        Plugin.Logger.LogInfo($"PopupBase.Hide: {popupName}");

        // Pop de la UI si había una
        AccessibilityMgr.PopUI();
        PopupShowPatch.ClearCurrentUI();
    }
}

/// <summary>
/// Hook para detectar diálogos de tutorial/secuencia.
/// </summary>
[HarmonyPatch]
public static class TutorialDialogPatch
{
    // Obtener el método Show de SequenceDialogController dinámicamente
    static MethodBase TargetMethod()
    {
        var type = typeof(PopupBase).Assembly.GetType("TheBazaar.SequenceDialogController");
        if (type != null)
        {
            var method = type.GetMethod("ShowDialog", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                Plugin.Logger.LogInfo("TutorialDialogPatch: Found SequenceDialogController.ShowDialog");
                return method;
            }
        }
        Plugin.Logger.LogWarning("TutorialDialogPatch: Could not find SequenceDialogController.ShowDialog");
        return null;
    }

    static void Postfix(object __instance)
    {
        try
        {
            // Obtener el texto del diálogo via reflexión
            var dialogTextField = __instance.GetType().GetField("_dialogText",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            if (dialogTextField != null)
            {
                var dialogText = dialogTextField.GetValue(__instance) as TMP_Text;
                if (dialogText != null && !string.IsNullOrWhiteSpace(dialogText.text))
                {
                    string text = dialogText.text.Trim();
                    Plugin.Logger.LogInfo($"Tutorial dialog: {text}");

                    // Añadir al buffer y leer
                    MessageBuffer.Add(text);
                    TolkWrapper.Speak(text);
                }
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"TutorialDialogPatch error: {ex.Message}");
        }
    }
}

/// <summary>
/// Hook alternativo para BasePointerDialogController.ShowDialog
/// </summary>
[HarmonyPatch]
public static class BaseDialogPatch
{
    static MethodBase TargetMethod()
    {
        var type = typeof(PopupBase).Assembly.GetType("TheBazaar.BasePointerDialogController");
        if (type != null)
        {
            var method = type.GetMethod("ShowDialog", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                Plugin.Logger.LogInfo("BaseDialogPatch: Found BasePointerDialogController.ShowDialog");
                return method;
            }
        }
        return null;
    }

    static void Postfix(object __instance)
    {
        try
        {
            var dialogTextField = __instance.GetType().GetField("_dialogText",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            if (dialogTextField != null)
            {
                var dialogText = dialogTextField.GetValue(__instance) as TMP_Text;
                if (dialogText != null && !string.IsNullOrWhiteSpace(dialogText.text))
                {
                    string text = dialogText.text.Trim();

                    // Evitar duplicados si ya lo anunció TutorialDialogPatch
                    if (!MessageBuffer.ContainsRecent(text))
                    {
                        Plugin.Logger.LogInfo($"Dialog: {text}");
                        MessageBuffer.Add(text);
                        TolkWrapper.Speak(text);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"BaseDialogPatch error: {ex.Message}");
        }
    }
}
