using BazaarAccess.Accessibility;
using BazaarAccess.Core;
using BazaarAccess.Patches;
using BazaarAccess.UI;
using BazaarGameClient.Domain.Models.Cards;
using BazaarGameShared.Domain.Core.Types;
using BazaarGameShared.Domain.Runs;
using TheBazaar;
using TheBazaar.AppFramework;

namespace BazaarAccess.Gameplay;

/// <summary>
/// Pantalla accesible para el gameplay.
/// Navegación dinámica con items y acciones.
/// </summary>
public class GameplayScreen : IAccessibleScreen
{
    public string ScreenName => "Gameplay";

    private readonly GameplayNavigator _navigator;
    private bool _isValid = true;
    private ERunState _lastState = ERunState.Choice;

    public GameplayScreen()
    {
        _navigator = new GameplayNavigator();
    }

    public void HandleInput(AccessibleKey key)
    {
        switch (key)
        {
            // Navegación de secciones
            case AccessibleKey.Tab:
                _navigator.NextSection();
                break;

            case AccessibleKey.GoToBoard:
                _navigator.GoToBoard();
                break;

            case AccessibleKey.GoToHero:
                _navigator.GoToHero();
                break;

            case AccessibleKey.GoToChoices:
                _navigator.GoToChoices();
                break;

            // Navegación dentro de la sección actual
            case AccessibleKey.Right:
                _navigator.Next();
                break;

            case AccessibleKey.Left:
                _navigator.Previous();
                break;

            // Up/Down para navegación vertical en Hero mode
            case AccessibleKey.Up:
                if (_navigator.IsInHeroSection)
                    _navigator.Previous();
                break;

            case AccessibleKey.Down:
                if (_navigator.IsInHeroSection)
                    _navigator.Next();
                break;

            // Información detallada (Ctrl+flecha)
            case AccessibleKey.ReadDetails:
                _navigator.ReadDetailedInfo();
                break;

            // Acción principal
            case AccessibleKey.Confirm:
                HandleConfirm();
                break;

            // Atajos directos para Exit/Reroll
            case AccessibleKey.Exit:
                _navigator.TryExit();
                break;

            case AccessibleKey.Reroll:
                _navigator.TryReroll();
                break;

            // Cancelar/Releer estado
            case AccessibleKey.Back:
                _navigator.AnnounceState();
                break;

            // Buffer de mensajes
            case AccessibleKey.NextMessage:
                MessageBuffer.ReadNewest();
                break;

            case AccessibleKey.PrevMessage:
                MessageBuffer.ReadPrevious();
                break;

            // Espacio - mover entre tablero y stash
            case AccessibleKey.Space:
                HandleMoveAction();
                break;
        }
    }

    public string GetHelp()
    {
        return "Left/Right: Navigate. Tab: Switch section. " +
               "B: Board. V: Hero. C: Choices. " +
               "Enter: Select/Buy/Sell. E: Exit. R: Refresh. " +
               "Ctrl+Arrow: Details. Space: Move item.";
    }

    public void OnFocus()
    {
        _navigator.Refresh();
        _lastState = StateChangePatch.GetCurrentRunState();
        _navigator.AnnounceState();
    }

    /// <summary>
    /// Llamado cuando cambia el estado del juego.
    /// </summary>
    public void OnStateChanged(ERunState newState)
    {
        if (newState == _lastState) return;

        _lastState = newState;

        string stateDesc = StateChangePatch.GetStateDescription(newState);
        TolkWrapper.Speak(stateDesc);

        // Hacer varios refreshes con delays para asegurar que capturamos todo
        Plugin.Instance.StartCoroutine(MultipleRefresh());
    }

    private System.Collections.IEnumerator MultipleRefresh()
    {
        // Primer refresh inmediato
        _navigator.Refresh();
        if (_navigator.HasContent())
        {
            _navigator.AnnounceCurrentItem();
        }

        // Segundo refresh después de un momento
        yield return new UnityEngine.WaitForSeconds(0.3f);
        _navigator.Refresh();

        // Tercer refresh para estados que tardan más en inicializarse
        yield return new UnityEngine.WaitForSeconds(0.5f);
        _navigator.Refresh();
    }

    public bool IsValid()
    {
        if (!_isValid) return false;

        try { return Singleton<BoardManager>.Instance != null; }
        catch { return false; }
    }

    public void Invalidate() => _isValid = false;

    private void HandleConfirm()
    {
        // Si estamos en Hero, leer toda la info
        if (_navigator.IsInHeroSection)
        {
            _navigator.ReadDetailedInfo();
            return;
        }

        // Si estamos en Selection, ver qué tipo de item es
        if (_navigator.IsInSelectionSection())
        {
            var navItem = _navigator.GetCurrentNavItem();
            if (navItem == null)
            {
                TolkWrapper.Speak("Nothing selected");
                return;
            }

            switch (navItem.Type)
            {
                case NavItemType.Exit:
                    _navigator.TryExit();
                    break;

                case NavItemType.Reroll:
                    _navigator.TryReroll();
                    break;

                case NavItemType.Card:
                    HandleCardConfirm(navItem.Card);
                    break;
            }
            return;
        }

        // Si estamos en Board/Stash, es venta
        if (_navigator.IsInPlayerSection())
        {
            var card = _navigator.GetCurrentCard();
            if (card != null)
            {
                HandleSellConfirm(card);
            }
            else
            {
                TolkWrapper.Speak("Nothing selected");
            }
            return;
        }

        // Skills - solo leer info
        _navigator.ReadDetailedInfo();
    }

    private void HandleCardConfirm(Card card)
    {
        switch (card.Type)
        {
            case ECardType.Item:
                BuyItem(card);
                break;

            case ECardType.Skill:
                SelectSkill(card);
                break;

            case ECardType.CombatEncounter:
            case ECardType.EventEncounter:
            case ECardType.PedestalEncounter:
            case ECardType.EncounterStep:
            case ECardType.PvpEncounter:
                // Encounters van directo sin confirmación
                SelectEncounterDirect(card);
                break;

            default:
                TolkWrapper.Speak("Cannot select this");
                break;
        }
    }

    private void BuyItem(Card card)
    {
        var itemCard = card as ItemCard;
        if (itemCard == null) { TolkWrapper.Speak("Not an item"); return; }

        string name = ItemReader.GetCardName(card);

        if (_navigator.IsSelectionFree())
        {
            // Selección gratis - ir directo
            ActionHelper.BuyItem(itemCard);
            RefreshAndAnnounce();
        }
        else
        {
            // Con precio - pedir confirmación
            int price = ItemReader.GetBuyPrice(card);
            var ui = new ConfirmActionUI(ConfirmActionType.Buy, name, price,
                onConfirm: () => {
                    ActionHelper.BuyItem(itemCard);
                    RefreshAndAnnounce();
                },
                onCancel: () => TolkWrapper.Speak("Cancelled"));
            AccessibilityMgr.ShowUI(ui);
        }
    }

    private void SelectSkill(Card card)
    {
        var skillCard = card as SkillCard;
        if (skillCard == null) { TolkWrapper.Speak("Not a skill"); return; }

        // Skills van directo sin confirmación
        ActionHelper.SelectSkill(skillCard);
        RefreshAndAnnounce();
    }

    /// <summary>
    /// Selecciona un encounter directamente sin confirmación.
    /// </summary>
    private void SelectEncounterDirect(Card card)
    {
        string name = ItemReader.GetCardName(card);

        // Leer el texto del evento si existe
        string flavorText = ItemReader.GetFlavorText(card);
        if (!string.IsNullOrEmpty(flavorText))
        {
            TolkWrapper.Speak(flavorText);
        }

        TolkWrapper.Speak($"Selecting {name}");
        ActionHelper.SelectEncounter(card);

        // Esperar un momento para que el estado cambie
        Plugin.Instance.StartCoroutine(DelayedRefresh());
    }

    private void HandleSellConfirm(Card card)
    {
        var itemCard = card as ItemCard;
        if (itemCard == null) { TolkWrapper.Speak("Cannot sell this"); return; }

        if (!_navigator.CanSellInCurrentState())
        {
            TolkWrapper.Speak("Cannot sell right now");
            return;
        }

        string name = ItemReader.GetCardName(card);
        int price = ItemReader.GetSellPrice(card);

        // Vender siempre pide confirmación
        var ui = new ConfirmActionUI(ConfirmActionType.Sell, name, price,
            onConfirm: () => {
                ActionHelper.SellItem(itemCard);
                RefreshAndAnnounce();
            },
            onCancel: () => TolkWrapper.Speak("Cancelled"));
        AccessibilityMgr.ShowUI(ui);
    }

    private void HandleMoveAction()
    {
        if (!_navigator.IsInPlayerSection())
        {
            TolkWrapper.Speak("Select an item on your board first");
            return;
        }

        var card = _navigator.GetCurrentCard() as ItemCard;
        if (card == null)
        {
            TolkWrapper.Speak("Cannot move this");
            return;
        }

        if (!_navigator.CanMoveInCurrentState())
        {
            TolkWrapper.Speak("Cannot move right now");
            return;
        }

        // Mover directamente sin confirmación
        bool toStash = _navigator.CurrentSection == NavigationSection.Board;
        ActionHelper.MoveItem(card, toStash);
        RefreshAndAnnounce();
    }

    private void RefreshAndAnnounce()
    {
        _navigator.Refresh();
        _navigator.AnnounceCurrentItem();
    }

    /// <summary>
    /// Refresca el navegador (llamado externamente por eventos del juego).
    /// </summary>
    public void RefreshNavigator()
    {
        _navigator.Refresh();
    }

    private System.Collections.IEnumerator DelayedRefresh()
    {
        yield return new UnityEngine.WaitForSeconds(0.5f);
        _navigator.Refresh();
        _navigator.AnnounceState();
    }
}
