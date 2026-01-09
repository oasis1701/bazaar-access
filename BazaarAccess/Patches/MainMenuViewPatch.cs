using HarmonyLib;
using TheBazaar.UI;

namespace BazaarAccess.Patches;

[HarmonyPatch(typeof(MainMenuView), nameof(MainMenuView.Show))]
public static class MainMenuViewPatch
{
    static void Postfix(MainMenuView __instance)
    {
        Plugin.Logger.LogInfo("MainMenuView.Show()");
        MenuNavigator.AnnounceMenuTitle(__instance.transform);
        MenuNavigator.SetMenuRoot(__instance.transform, "MainMenu");
    }
}
