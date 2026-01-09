using HarmonyLib;
using TheBazaar;
using UnityEngine;

namespace BazaarAccess.Patches;

[HarmonyPatch(typeof(PopupBase), nameof(PopupBase.Show))]
public static class PopupShowPatch
{
    static void Postfix(PopupBase __instance)
    {
        string popupName = __instance.GetType().Name;
        Plugin.Logger.LogInfo($"PopupBase.Show(): {popupName}");

        // Guardar el menú anterior antes de cambiar al popup
        MenuNavigator.SavePreviousMenu();

        MenuNavigator.AnnounceMenuTitle(__instance.transform);
        MenuNavigator.SetMenuRoot(__instance.transform, popupName);
    }
}

[HarmonyPatch(typeof(PopupBase), nameof(PopupBase.Hide))]
public static class PopupHidePatch
{
    static void Postfix(PopupBase __instance)
    {
        string popupName = __instance.GetType().Name;
        Plugin.Logger.LogInfo($"PopupBase.Hide(): {popupName}");

        // Restaurar el menú anterior
        MenuNavigator.RestorePreviousMenu();
    }
}
