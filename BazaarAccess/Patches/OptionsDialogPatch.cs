using HarmonyLib;
using UnityEngine;

namespace BazaarAccess.Patches;

[HarmonyPatch(typeof(OptionsDialogController), "OnEnable")]
public static class OptionsDialogEnablePatch
{
    static void Postfix(OptionsDialogController __instance)
    {
        // Solo procesar si el objeto está realmente visible
        if (!__instance.gameObject.activeInHierarchy) return;

        Plugin.Logger.LogInfo("OptionsDialogController.OnEnable()");

        // Guardar el menú anterior
        MenuNavigator.SavePreviousMenu();

        MenuNavigator.AnnounceMenuTitle(__instance.transform);
        MenuNavigator.SetMenuRoot(__instance.transform, "Options");
    }
}

[HarmonyPatch(typeof(OptionsDialogController), "OnDisable")]
public static class OptionsDialogDisablePatch
{
    static void Postfix(OptionsDialogController __instance)
    {
        Plugin.Logger.LogInfo("OptionsDialogController.OnDisable()");

        // Restaurar el menú anterior
        MenuNavigator.RestorePreviousMenu();
    }
}
