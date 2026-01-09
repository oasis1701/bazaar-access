using BazaarAccess.Accessibility;
using BazaarAccess.Gameplay;
using HarmonyLib;
using TheBazaar;

namespace BazaarAccess.Patches;

/// <summary>
/// Detecta entrada al gameplay.
/// </summary>
[HarmonyPatch(typeof(BoardManager), "OnAwake")]
public static class GameplayPatch
{
    private static GameplayScreen _gameplayScreen;
    private static bool _stateSubscribed = false;

    [HarmonyPostfix]
    public static void Postfix(BoardManager __instance)
    {
        Plugin.Logger.LogInfo("BoardManager.OnAwake - Entering gameplay");

        // Suscribir a cambios de estado (solo una vez)
        if (!_stateSubscribed)
        {
            StateChangePatch.Subscribe();
            _stateSubscribed = true;
        }

        // Crear la pantalla de gameplay
        _gameplayScreen = new GameplayScreen();
        AccessibilityMgr.SetScreen(_gameplayScreen);
    }

    /// <summary>
    /// Obtiene la pantalla de gameplay actual.
    /// </summary>
    public static GameplayScreen GetGameplayScreen() => _gameplayScreen;
}
