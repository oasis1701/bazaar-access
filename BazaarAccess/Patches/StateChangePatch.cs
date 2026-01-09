using System;
using System.Collections.Generic;
using System.Reflection;
using BazaarAccess.Accessibility;
using BazaarAccess.Core;
using BazaarAccess.Gameplay;
using BazaarGameClient.Domain.Models.Cards;
using BazaarGameShared.Domain.Runs;
using BazaarGameShared.Infra.Messages.GameSimEvents;
using HarmonyLib;
using TheBazaar;

namespace BazaarAccess.Patches;

/// <summary>
/// Escucha cambios de estado del gameplay en tiempo real.
/// </summary>
public static class StateChangePatch
{
    private static ERunState _lastState = ERunState.Choice;
    private static bool _subscribed = false;
    private static Type _eventsType;

    /// <summary>
    /// Suscribe a todos los eventos relevantes.
    /// </summary>
    public static void Subscribe()
    {
        if (_subscribed) return;

        try
        {
            // Events es internal, acceder via reflexión
            _eventsType = typeof(AppState).Assembly.GetType("TheBazaar.Events");
            if (_eventsType == null)
            {
                Plugin.Logger.LogError("StateChangePatch: No se encontró TheBazaar.Events");
                return;
            }

            // Suscribir a cambio de estado
            SubscribeToEvent("StateChanged", typeof(Action<StateChangedEvent>),
                (Action<StateChangedEvent>)OnStateChanged);

            // Suscribir a compra de items (para refrescar la lista)
            SubscribeToEvent("CardPurchasedSimEvent", typeof(Action<GameSimEventCardPurchased>),
                (Action<GameSimEventCardPurchased>)OnCardPurchased);

            // Suscribir a nuevos items en tienda
            SubscribeToEvent("CardDealtSimEvent", typeof(Action<List<Card>>),
                (Action<List<Card>>)OnCardsDealt);

            // Suscribir a venta de items
            SubscribeToEvent("CardSoldSimEvent", typeof(Action<GameSimEventCardSold>),
                (Action<GameSimEventCardSold>)OnCardSold);

            _subscribed = true;
            Plugin.Logger.LogInfo("StateChangePatch: Suscrito a eventos del juego");
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"StateChangePatch.Subscribe error: {ex.Message}");
        }
    }

    private static void SubscribeToEvent(string eventName, Type handlerType, Delegate handler)
    {
        try
        {
            var eventField = _eventsType.GetField(eventName, BindingFlags.Public | BindingFlags.Static);
            if (eventField == null)
            {
                Plugin.Logger.LogWarning($"StateChangePatch: No se encontró Events.{eventName}");
                return;
            }

            var eventObj = eventField.GetValue(null);
            if (eventObj == null)
            {
                Plugin.Logger.LogWarning($"StateChangePatch: Events.{eventName} es null");
                return;
            }

            var addListenerMethod = eventObj.GetType().GetMethod("AddListener",
                new Type[] { handlerType, typeof(UnityEngine.MonoBehaviour) });

            if (addListenerMethod == null)
            {
                Plugin.Logger.LogWarning($"StateChangePatch: No se encontró AddListener para {eventName}");
                return;
            }

            addListenerMethod.Invoke(eventObj, new object[] { handler, null });
            Plugin.Logger.LogInfo($"StateChangePatch: Suscrito a {eventName}");
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning($"StateChangePatch: Error suscribiendo a {eventName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Callback cuando cambia el estado del juego.
    /// </summary>
    private static void OnStateChanged(StateChangedEvent evt)
    {
        try
        {
            // Obtener el nuevo estado
            var newState = GetCurrentRunState();

            if (newState != _lastState)
            {
                Plugin.Logger.LogInfo($"State changed: {_lastState} -> {newState}");
                _lastState = newState;

                // Anunciar el cambio solo si estamos en gameplay y no hay UI abierta
                if (AccessibilityMgr.GetFocusedUI() == null)
                {
                    var screen = AccessibilityMgr.GetCurrentScreen() as GameplayScreen;
                    screen?.OnStateChanged(newState);
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"StateChangePatch.OnStateChanged error: {ex.Message}");
        }
    }

    /// <summary>
    /// Callback cuando se compra una carta.
    /// </summary>
    private static void OnCardPurchased(GameSimEventCardPurchased evt)
    {
        try
        {
            Plugin.Logger.LogInfo($"Card purchased: {evt.InstanceId}");
            RefreshGameplayScreen();
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"OnCardPurchased error: {ex.Message}");
        }
    }

    /// <summary>
    /// Callback cuando se reparten nuevas cartas (tienda refrescada).
    /// </summary>
    private static void OnCardsDealt(List<Card> cards)
    {
        try
        {
            Plugin.Logger.LogInfo($"Cards dealt: {cards?.Count ?? 0}");
            RefreshGameplayScreen();
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"OnCardsDealt error: {ex.Message}");
        }
    }

    /// <summary>
    /// Callback cuando se vende una carta.
    /// </summary>
    private static void OnCardSold(GameSimEventCardSold evt)
    {
        try
        {
            Plugin.Logger.LogInfo($"Card sold: {evt.InstanceId}");
            RefreshGameplayScreen();
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"OnCardSold error: {ex.Message}");
        }
    }

    /// <summary>
    /// Refresca la pantalla de gameplay si está activa.
    /// </summary>
    private static void RefreshGameplayScreen()
    {
        if (AccessibilityMgr.GetFocusedUI() != null) return;

        var screen = AccessibilityMgr.GetCurrentScreen() as GameplayScreen;
        if (screen != null)
        {
            // Usar coroutine para dar tiempo al juego de actualizar
            Plugin.Instance.StartCoroutine(DelayedRefresh(screen));
        }
    }

    private static System.Collections.IEnumerator DelayedRefresh(GameplayScreen screen)
    {
        // Primer refresh rápido
        yield return new UnityEngine.WaitForSeconds(0.1f);
        screen.RefreshNavigator();

        // Segundo refresh para capturar cambios tardíos
        yield return new UnityEngine.WaitForSeconds(0.3f);
        screen.RefreshNavigator();
    }

    /// <summary>
    /// Obtiene el estado actual del run.
    /// </summary>
    public static ERunState GetCurrentRunState()
    {
        try
        {
            return Data.CurrentState?.StateName ?? ERunState.Choice;
        }
        catch
        {
            return ERunState.Choice;
        }
    }

    /// <summary>
    /// Obtiene una descripción del estado actual.
    /// </summary>
    public static string GetStateDescription(ERunState state)
    {
        return state switch
        {
            ERunState.Choice => "Shop",
            ERunState.Encounter => "Choose encounter",
            ERunState.Combat => "Combat",
            ERunState.PVPCombat => "PvP Combat",
            ERunState.Loot => "Loot",
            ERunState.LevelUp => "Level up",
            ERunState.Pedestal => "Upgrade station",
            ERunState.EndRunVictory => "Victory!",
            ERunState.EndRunDefeat => "Defeat",
            ERunState.NewRun => "Starting run",
            ERunState.Shutdown => "Game ending",
            _ => state.ToString()
        };
    }
}
