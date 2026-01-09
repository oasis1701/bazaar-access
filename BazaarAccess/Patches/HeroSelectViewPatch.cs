using HarmonyLib;
using TheBazaar.UI;

namespace BazaarAccess.Patches;

[HarmonyPatch(typeof(HeroSelectView), nameof(HeroSelectView.Show))]
public static class HeroSelectViewPatch
{
    static void Postfix(HeroSelectView __instance)
    {
        Plugin.Logger.LogInfo("HeroSelectView.Show()");
        MenuNavigator.AnnounceMenuTitle(__instance.transform);
        MenuNavigator.SetMenuRoot(__instance.transform, "HeroSelect");
    }
}
