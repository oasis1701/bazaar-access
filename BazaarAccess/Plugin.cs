using BepInEx;
using BepInEx.Logging;
using DavyKager;
using HarmonyLib;
using UnityEngine;

namespace BazaarAccess;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    private static Harmony _harmony;

    private void Awake()
    {
        Logger = base.Logger;

        Tolk.Load();
        Tolk.Output("Bazaar Access cargado");

        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} cargado");
    }

    private void OnGUI()
    {
        Event e = Event.current;
        if (e != null && e.type == EventType.KeyDown)
        {
            bool handled = true;
            switch (e.keyCode)
            {
                case KeyCode.DownArrow:
                    MenuNavigator.Navigate(1);
                    break;
                case KeyCode.UpArrow:
                    MenuNavigator.Navigate(-1);
                    break;
                case KeyCode.RightArrow:
                    MenuNavigator.AdjustValue(1);
                    break;
                case KeyCode.LeftArrow:
                    MenuNavigator.AdjustValue(-1);
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    MenuNavigator.ActivateSelected();
                    break;
                case KeyCode.F5:
                    MenuNavigator.RefreshAndRead();
                    break;
                default:
                    handled = false;
                    break;
            }
            if (handled) e.Use();
        }
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        Tolk.Unload();
    }
}
