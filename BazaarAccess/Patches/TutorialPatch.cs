using System.Collections.Generic;
using BazaarAccess.Accessibility;
using BazaarAccess.Core;
using BazaarAccess.UI;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace BazaarAccess.Patches;

/// <summary>
/// Patches para hacer el tutorial (FTUE) accesible.
/// Lee los diálogos del tutorial en voz alta y permite navegarlos con teclado.
/// </summary>
public static class TutorialPatch
{
    private static TutorialUI _currentTutorialUI;
    private static string _lastAnnouncedText = "";
    private static float _lastAnnouncedTime = 0f;
    private const float DEDUP_WINDOW = 2f; // seconds to consider text as duplicate

    /// <summary>
    /// Closes the current tutorial UI if one exists.
    /// </summary>
    private static void CloseCurrentTutorialUI()
    {
        if (_currentTutorialUI != null)
        {
            AccessibilityMgr.HideUI(_currentTutorialUI);
            _currentTutorialUI = null;
        }
    }

    /// <summary>
    /// Checks if text was recently announced (duplicate detection).
    /// </summary>
    private static bool IsRecentDuplicate(string text)
    {
        if (string.IsNullOrEmpty(text)) return true;

        float currentTime = Time.realtimeSinceStartup;
        bool isDuplicate = text == _lastAnnouncedText &&
                          (currentTime - _lastAnnouncedTime) < DEDUP_WINDOW;

        if (!isDuplicate)
        {
            _lastAnnouncedText = text;
            _lastAnnouncedTime = currentTime;
        }

        return isDuplicate;
    }

    /// <summary>
    /// Patch para SequenceDialogController.ShowDialog - lee el texto del tutorial.
    /// </summary>
    [HarmonyPatch]
    public static class SequenceDialogShowPatch
    {
        static bool Prepare()
        {
            return AccessTools.TypeByName("TheBazaar.SequenceDialogController") != null;
        }

        static System.Reflection.MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("TheBazaar.SequenceDialogController");
            return AccessTools.Method(type, "ShowDialog");
        }

        [HarmonyPostfix]
        public static void Postfix(object __instance)
        {
            try
            {
                // Obtener el texto del diálogo via reflexión
                var baseType = AccessTools.TypeByName("TheBazaar.BasePointerDialogController");
                var textField = AccessTools.Field(baseType, "_text");
                var text = textField?.GetValue(__instance) as string;

                if (!string.IsNullOrEmpty(text))
                {
                    // Limpiar HTML tags
                    text = CleanText(text);

                    // Skip if duplicate text within dedup window
                    if (IsRecentDuplicate(text))
                    {
                        Plugin.Logger.LogInfo($"Tutorial dialog (skipped duplicate): {text}");
                        return;
                    }

                    Plugin.Logger.LogInfo($"Tutorial dialog: {text}");

                    // Close previous tutorial UI before creating new one
                    CloseCurrentTutorialUI();

                    // Crear UI de tutorial
                    var monoBehaviour = __instance as MonoBehaviour;
                    if (monoBehaviour != null)
                    {
                        _currentTutorialUI = new TutorialUI(monoBehaviour.transform, text, isFullScreen: false);
                        AccessibilityMgr.ShowUI(_currentTutorialUI);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogWarning($"SequenceDialogShowPatch error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Patch para SequenceDialogController.Hide - cierra la UI de tutorial.
    /// </summary>
    [HarmonyPatch]
    public static class SequenceDialogHidePatch
    {
        static bool Prepare()
        {
            return AccessTools.TypeByName("TheBazaar.SequenceDialogController") != null;
        }

        static System.Reflection.MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("TheBazaar.SequenceDialogController");
            return AccessTools.Method(type, "Hide");
        }

        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
                CloseCurrentTutorialUI();
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogWarning($"SequenceDialogHidePatch error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Patch para FullScreenPopupDialogController.ShowDialog - lee el título y cuerpo.
    /// </summary>
    [HarmonyPatch]
    public static class FullScreenPopupShowPatch
    {
        static bool Prepare()
        {
            return AccessTools.TypeByName("TheBazaar.FullScreenPopupDialogController") != null;
        }

        static System.Reflection.MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("TheBazaar.FullScreenPopupDialogController");
            return AccessTools.Method(type, "ShowDialog");
        }

        [HarmonyPostfix]
        public static void Postfix(object __instance)
        {
            try
            {
                var baseType = AccessTools.TypeByName("TheBazaar.BasePointerDialogController");
                var fullScreenType = AccessTools.TypeByName("TheBazaar.FullScreenPopupDialogController");

                // Obtener título
                var textField = AccessTools.Field(baseType, "_text");
                var title = textField?.GetValue(__instance) as string ?? "";

                // Obtener cuerpo
                var bodyField = AccessTools.Field(fullScreenType, "_secondaryText");
                var body = bodyField?.GetValue(__instance) as string ?? "";

                // Obtener botones
                var nextButtonField = AccessTools.Field(fullScreenType, "_nextButton");
                var prevButtonField = AccessTools.Field(fullScreenType, "_previousButton");
                var nextButton = nextButtonField?.GetValue(__instance);
                var prevButton = prevButtonField?.GetValue(__instance);

                // Limpiar textos
                title = CleanText(title);
                body = CleanText(body);

                string fullText = title;
                if (!string.IsNullOrEmpty(body))
                {
                    fullText += ". " + body;
                }

                // Skip if duplicate text within dedup window
                if (IsRecentDuplicate(fullText))
                {
                    Plugin.Logger.LogInfo($"FullScreen tutorial (skipped duplicate): {fullText}");
                    return;
                }

                Plugin.Logger.LogInfo($"FullScreen tutorial: {fullText}");

                // Close previous tutorial UI before creating new one
                CloseCurrentTutorialUI();

                var monoBehaviour = __instance as MonoBehaviour;
                if (monoBehaviour != null)
                {
                    _currentTutorialUI = new TutorialUI(
                        monoBehaviour.transform,
                        fullText,
                        isFullScreen: true,
                        nextButton: nextButton as MonoBehaviour,
                        prevButton: prevButton as MonoBehaviour
                    );
                    AccessibilityMgr.ShowUI(_currentTutorialUI);
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogWarning($"FullScreenPopupShowPatch error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Patch para FullScreenPopupDialogController.Hide.
    /// </summary>
    [HarmonyPatch]
    public static class FullScreenPopupHidePatch
    {
        static bool Prepare()
        {
            return AccessTools.TypeByName("TheBazaar.FullScreenPopupDialogController") != null;
        }

        static System.Reflection.MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("TheBazaar.FullScreenPopupDialogController");
            return AccessTools.Method(type, "Hide");
        }

        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
                CloseCurrentTutorialUI();
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogWarning($"FullScreenPopupHidePatch error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Limpia el texto de tags HTML y caracteres especiales.
    /// </summary>
    private static string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Remover tags HTML/rich text
        text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", "");

        // Remover múltiples espacios
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");

        return text.Trim();
    }
}
