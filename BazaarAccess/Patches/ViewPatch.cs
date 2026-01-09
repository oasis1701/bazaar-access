using HarmonyLib;
using TheBazaar.UI;

namespace BazaarAccess.Patches;

[HarmonyPatch(typeof(View), nameof(View.Show))]
public static class ViewPatch
{
    // Vistas que tienen parches específicos
    private static readonly string[] IgnoredViews = { "MainMenuView", "HeroSelectView" };

    static void Postfix(View __instance)
    {
        string typeName = __instance.GetType().Name;

        // Ignorar vistas con parches específicos
        foreach (var ignored in IgnoredViews)
        {
            if (typeName == ignored) return;
        }

        string viewName = __instance.ViewName;
        if (string.IsNullOrEmpty(viewName))
            viewName = typeName;

        Plugin.Logger.LogInfo($"View.Show(): {viewName}");
        MenuNavigator.AnnounceMenuTitle(__instance.transform);
        MenuNavigator.SetMenuRoot(__instance.transform, viewName);
    }
}
