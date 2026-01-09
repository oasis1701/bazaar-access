using System.Collections.Generic;
using System.Linq;
using BazaarAccess.Core;
using BazaarAccess.Patches;
using BazaarGameClient.Domain.Models.Cards;
using BazaarGameShared.Domain.Core.Types;
using BazaarGameShared.Domain.Runs;
using TheBazaar;
using TheBazaar.AppFramework;

namespace BazaarAccess.Gameplay;

/// <summary>
/// Sección actual de navegación.
/// </summary>
public enum NavigationSection
{
    Selection,  // Lo que el juego te ofrece (encuentros, tienda, recompensas) + acciones
    Board,      // Tus items equipados
    Stash,      // Tu almacén
    Skills,     // Tus habilidades
    Hero        // Stats del héroe
}

/// <summary>
/// Tipos de items navegables (cartas o acciones).
/// </summary>
public enum NavItemType
{
    Card,       // Una carta normal
    Exit,       // Acción de salir
    Reroll      // Acción de refrescar
}

/// <summary>
/// Item navegable (puede ser carta o acción).
/// </summary>
public class NavItem
{
    public NavItemType Type { get; set; }
    public Card Card { get; set; }
    public int RerollCost { get; set; }

    public static NavItem FromCard(Card card) => new NavItem { Type = NavItemType.Card, Card = card };
    public static NavItem CreateExit() => new NavItem { Type = NavItemType.Exit };
    public static NavItem CreateReroll(int cost) => new NavItem { Type = NavItemType.Reroll, RerollCost = cost };
}

/// <summary>
/// Navegador de gameplay fiel al flujo original del juego.
/// - Selección: lo que puedes elegir (encuentros, items de tienda)
/// - Tablero: tus items (puedes vender, mover)
/// </summary>
public class GameplayNavigator
{
    private NavigationSection _currentSection = NavigationSection.Selection;
    private int _currentIndex = 0;

    // Cache de items navegables (cartas + acciones)
    private List<NavItem> _selectionItems = new List<NavItem>();  // SelectionSet + Exit/Reroll
    private List<int> _boardIndices = new List<int>();        // Slots ocupados del tablero
    private List<int> _stashIndices = new List<int>();        // Slots ocupados del stash
    private List<int> _skillIndices = new List<int>();        // Slots ocupados de skills

    // Stats del héroe para navegación
    private int _heroStatIndex = 0;
    private static readonly EPlayerAttributeType[] HeroStats = new[]
    {
        EPlayerAttributeType.Health,
        EPlayerAttributeType.HealthMax,
        EPlayerAttributeType.Gold,
        EPlayerAttributeType.Level,
        EPlayerAttributeType.Experience,
        EPlayerAttributeType.Shield,
        EPlayerAttributeType.Poison,
        EPlayerAttributeType.Burn,
        EPlayerAttributeType.HealthRegen,
        EPlayerAttributeType.CritChance,
        EPlayerAttributeType.Income
    };

    public NavigationSection CurrentSection => _currentSection;
    public bool IsInHeroSection => _currentSection == NavigationSection.Hero;

    public ERunState GetCurrentState()
    {
        return StateChangePatch.GetCurrentRunState();
    }

    public string GetStateDescription()
    {
        return StateChangePatch.GetStateDescription(GetCurrentState());
    }

    /// <summary>
    /// Actualiza todas las listas de cartas.
    /// </summary>
    public void Refresh()
    {
        RefreshSelection();
        RefreshBoard();
        RefreshStash();
        RefreshSkills();

        // Si la sección actual está vacía, cambiar a una con contenido
        if (GetCurrentSectionCount() == 0)
        {
            if (_selectionItems.Count > 0)
            {
                _currentSection = NavigationSection.Selection;
            }
            else if (_boardIndices.Count > 0)
            {
                _currentSection = NavigationSection.Board;
            }
            else if (_skillIndices.Count > 0)
            {
                _currentSection = NavigationSection.Skills;
            }
            _currentIndex = 0;
        }
        else if (_currentIndex >= GetCurrentSectionCount())
        {
            _currentIndex = 0;
        }
    }

    private void RefreshSelection()
    {
        _selectionItems.Clear();
        try
        {
            // Agregar cartas del SelectionSet
            var selectionSet = Data.CurrentState?.SelectionSet;
            if (selectionSet != null)
            {
                foreach (var id in selectionSet)
                {
                    var card = Data.GetCard(id);
                    if (card != null) _selectionItems.Add(NavItem.FromCard(card));
                }
            }

            // Agregar acciones disponibles al final
            if (CanReroll())
            {
                _selectionItems.Add(NavItem.CreateReroll(GetRerollCost()));
            }

            if (CanExit())
            {
                _selectionItems.Add(NavItem.CreateExit());
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"RefreshSelection error: {ex.Message}");
        }
    }

    private void RefreshBoard()
    {
        _boardIndices.Clear();
        var bm = GetBoardManager();
        if (bm?.playerItemSockets == null) return;

        for (int i = 0; i < bm.playerItemSockets.Length; i++)
        {
            if (bm.playerItemSockets[i]?.CardController?.CardData != null)
                _boardIndices.Add(i);
        }
    }

    private void RefreshStash()
    {
        _stashIndices.Clear();
        var bm = GetBoardManager();
        if (bm?.playerStorageSockets == null) return;

        for (int i = 0; i < bm.playerStorageSockets.Length; i++)
        {
            if (bm.playerStorageSockets[i]?.CardController?.CardData != null)
                _stashIndices.Add(i);
        }
    }

    private void RefreshSkills()
    {
        _skillIndices.Clear();
        var bm = GetBoardManager();
        if (bm?.playerSkillSockets == null) return;

        for (int i = 0; i < bm.playerSkillSockets.Length; i++)
        {
            if (bm.playerSkillSockets[i]?.CardController?.CardData != null)
                _skillIndices.Add(i);
        }
    }

    // --- Navegación ---

    /// <summary>
    /// Cambia a la siguiente sección con contenido.
    /// </summary>
    public void NextSection()
    {
        var sections = GetAvailableSections();
        if (sections.Count <= 1) return;

        int idx = sections.IndexOf(_currentSection);
        int nextIdx = (idx + 1) % sections.Count;
        _currentSection = sections[nextIdx];
        _currentIndex = 0;
        _heroStatIndex = 0;
        AnnounceSection();
    }

    /// <summary>
    /// Va directamente a una sección específica.
    /// </summary>
    public void GoToSection(NavigationSection section)
    {
        _currentSection = section;
        _currentIndex = 0;
        _heroStatIndex = 0;
        AnnounceSection();
    }

    /// <summary>
    /// Va a la sección de choices/selection.
    /// </summary>
    public void GoToChoices()
    {
        if (_selectionItems.Count > 0)
        {
            GoToSection(NavigationSection.Selection);
        }
        else
        {
            TolkWrapper.Speak("No choices available");
        }
    }

    /// <summary>
    /// Va a la sección del board.
    /// </summary>
    public void GoToBoard()
    {
        if (_boardIndices.Count > 0)
        {
            GoToSection(NavigationSection.Board);
        }
        else if (_stashIndices.Count > 0)
        {
            GoToSection(NavigationSection.Stash);
            TolkWrapper.Speak("Board empty, showing stash");
        }
        else
        {
            TolkWrapper.Speak("No items on board");
        }
    }

    /// <summary>
    /// Va a la sección del héroe.
    /// </summary>
    public void GoToHero()
    {
        GoToSection(NavigationSection.Hero);
    }

    private List<NavigationSection> GetAvailableSections()
    {
        var list = new List<NavigationSection>();
        if (_selectionItems.Count > 0) list.Add(NavigationSection.Selection);
        if (_boardIndices.Count > 0) list.Add(NavigationSection.Board);
        if (_stashIndices.Count > 0) list.Add(NavigationSection.Stash);
        if (_skillIndices.Count > 0) list.Add(NavigationSection.Skills);
        list.Add(NavigationSection.Hero); // Hero siempre disponible
        return list;
    }

    public void Next()
    {
        if (_currentSection == NavigationSection.Hero)
        {
            _heroStatIndex = (_heroStatIndex + 1) % HeroStats.Length;
            AnnounceHeroStat();
            return;
        }

        int count = GetCurrentSectionCount();
        if (count == 0) return;
        _currentIndex = (_currentIndex + 1) % count;
        AnnounceCurrentItem();
    }

    public void Previous()
    {
        if (_currentSection == NavigationSection.Hero)
        {
            _heroStatIndex = (_heroStatIndex - 1 + HeroStats.Length) % HeroStats.Length;
            AnnounceHeroStat();
            return;
        }

        int count = GetCurrentSectionCount();
        if (count == 0) return;
        _currentIndex = (_currentIndex - 1 + count) % count;
        AnnounceCurrentItem();
    }

    // --- Anuncios ---

    public void AnnounceState()
    {
        Refresh();

        string state = GetStateDescription();
        int selCount = _selectionItems.Count;
        int boardCount = _boardIndices.Count;

        var parts = new List<string> { state };

        if (selCount > 0)
        {
            string type = GetSelectionTypeName();
            parts.Add($"{selCount} {type}");
        }
        if (boardCount > 0)
        {
            parts.Add($"{boardCount} items on board");
        }

        TolkWrapper.Speak(string.Join(", ", parts));

        if (GetCurrentSectionCount() > 0)
        {
            AnnounceCurrentItem();
        }
    }

    private string GetSelectionTypeName()
    {
        // Contar solo las cartas (no Exit/Reroll)
        var cards = _selectionItems.Where(i => i.Type == NavItemType.Card).ToList();
        if (cards.Count == 0) return "options";

        var firstCard = cards[0].Card;
        if (IsEncounterCard(firstCard)) return "encounters";
        if (firstCard.Type == ECardType.Skill) return "skills";
        return "items";
    }

    public void AnnounceSection()
    {
        if (_currentSection == NavigationSection.Hero)
        {
            TolkWrapper.Speak("Hero stats");
            AnnounceHeroStat();
            return;
        }

        string name = _currentSection switch
        {
            NavigationSection.Selection => GetSelectionTypeName(),
            NavigationSection.Board => "Board",
            NavigationSection.Stash => "Stash",
            NavigationSection.Skills => "Skills",
            _ => "Unknown"
        };

        int count = GetCurrentSectionCount();
        TolkWrapper.Speak($"{name}, {count} items");

        if (count > 0) AnnounceCurrentItem();
    }

    public void AnnounceCurrentItem()
    {
        int pos = _currentIndex + 1;
        int total = GetCurrentSectionCount();

        // Si estamos en Hero, anunciar stat
        if (_currentSection == NavigationSection.Hero)
        {
            AnnounceHeroStat();
            return;
        }

        // Si estamos en Selection, puede ser carta o acción
        if (_currentSection == NavigationSection.Selection)
        {
            var navItem = GetCurrentNavItem();
            if (navItem == null)
            {
                TolkWrapper.Speak("Empty");
                return;
            }

            string desc;
            switch (navItem.Type)
            {
                case NavItemType.Exit:
                    desc = "Exit";
                    break;
                case NavItemType.Reroll:
                    desc = $"Refresh, {navItem.RerollCost} gold";
                    break;
                case NavItemType.Card:
                    desc = GetCardDescription(navItem.Card);
                    break;
                default:
                    desc = "Unknown";
                    break;
            }

            TolkWrapper.Speak($"{desc}, {pos} of {total}");
            return;
        }

        // Para otras secciones (Board, Stash, Skills)
        var card = GetCurrentCard();
        if (card == null)
        {
            TolkWrapper.Speak("Empty");
            return;
        }

        string cardDesc = GetCardDescription(card);
        TolkWrapper.Speak($"{cardDesc}, {pos} of {total}");
    }

    public void ReadDetailedInfo()
    {
        if (_currentSection == NavigationSection.Hero)
        {
            ReadAllHeroStats();
            return;
        }

        // Si estamos en Selection, puede ser NavItem
        if (_currentSection == NavigationSection.Selection)
        {
            var navItem = GetCurrentNavItem();
            if (navItem == null)
            {
                TolkWrapper.Speak("Nothing selected");
                return;
            }

            switch (navItem.Type)
            {
                case NavItemType.Exit:
                    TolkWrapper.Speak("Exit. Leave the current state and continue.");
                    return;
                case NavItemType.Reroll:
                    int gold = Data.Run?.Player?.GetAttributeValue(EPlayerAttributeType.Gold) ?? 0;
                    TolkWrapper.Speak($"Refresh. Get new items for {navItem.RerollCost} gold. You have {gold} gold.");
                    return;
                case NavItemType.Card:
                    var card = navItem.Card;
                    if (IsEncounterCard(card))
                        TolkWrapper.Speak(ItemReader.GetEncounterDetailedInfo(card));
                    else
                        TolkWrapper.Speak(ItemReader.GetDetailedDescription(card));
                    return;
            }
        }

        // Para otras secciones
        var currentCard = GetCurrentCard();
        if (currentCard == null)
        {
            TolkWrapper.Speak("Nothing selected");
            return;
        }

        if (IsEncounterCard(currentCard))
            TolkWrapper.Speak(ItemReader.GetEncounterDetailedInfo(currentCard));
        else
            TolkWrapper.Speak(ItemReader.GetDetailedDescription(currentCard));
    }

    // --- Acciones del juego ---

    /// <summary>
    /// Verifica si se puede salir del estado actual.
    /// </summary>
    public bool CanExit()
    {
        try
        {
            var state = AppState.CurrentState;
            if (state == null)
            {
                Plugin.Logger.LogInfo("CanExit: AppState.CurrentState is null");
                return false;
            }

            bool canHandle = state.CanHandleOperation(StateOps.ExitState);
            bool rulesAllow = Data.CurrentState?.SelectionContextRules?.CanExit ?? true;

            Plugin.Logger.LogInfo($"CanExit: canHandle={canHandle}, rulesAllow={rulesAllow}");

            if (!canHandle) return false;
            return rulesAllow;
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"CanExit error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Ejecuta el comando de salir del estado actual.
    /// </summary>
    public bool TryExit()
    {
        if (!CanExit())
        {
            TolkWrapper.Speak("Cannot exit now");
            return false;
        }

        try
        {
            AppState.CurrentState.ExitStateCommand();
            TolkWrapper.Speak("Exiting");
            return true;
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"TryExit error: {ex.Message}");
            TolkWrapper.Speak("Exit failed");
            return false;
        }
    }

    /// <summary>
    /// Verifica si se puede hacer reroll.
    /// </summary>
    public bool CanReroll()
    {
        try
        {
            var state = AppState.CurrentState;
            if (state == null) return false;
            if (!state.CanHandleOperation(StateOps.Reroll)) return false;

            var rerollCost = Data.CurrentState?.RerollCost;
            var rerollsRemaining = Data.CurrentState?.RerollsRemaining;

            if (!rerollCost.HasValue || rerollCost.Value < 0) return false;
            if (rerollsRemaining.HasValue && rerollsRemaining.Value == 0) return false;

            return true;
        }
        catch { return false; }
    }

    /// <summary>
    /// Obtiene el costo de reroll.
    /// </summary>
    public int GetRerollCost()
    {
        return (int)(Data.CurrentState?.RerollCost ?? 0);
    }

    /// <summary>
    /// Ejecuta el comando de reroll.
    /// </summary>
    public bool TryReroll()
    {
        if (!CanReroll())
        {
            TolkWrapper.Speak("Cannot refresh now");
            return false;
        }

        int cost = GetRerollCost();
        int gold = Data.Run?.Player?.GetAttributeValue(EPlayerAttributeType.Gold) ?? 0;

        if (gold < cost)
        {
            TolkWrapper.Speak($"Not enough gold. Need {cost}, have {gold}");
            return false;
        }

        try
        {
            if (AppState.CurrentState.RerollCommand())
            {
                TolkWrapper.Speak($"Refreshed for {cost} gold");
                // Refrescar después del reroll
                Plugin.Instance.StartCoroutine(DelayedRefresh());
                return true;
            }
            else
            {
                TolkWrapper.Speak("Refresh failed");
                return false;
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"TryReroll error: {ex.Message}");
            TolkWrapper.Speak("Refresh failed");
            return false;
        }
    }

    private System.Collections.IEnumerator DelayedRefresh()
    {
        yield return new UnityEngine.WaitForSeconds(0.5f);
        Refresh();
        AnnounceState();
    }

    /// <summary>
    /// Anuncia las acciones disponibles (Exit, Reroll).
    /// </summary>
    public void AnnounceAvailableActions()
    {
        var actions = new List<string>();

        if (CanExit())
            actions.Add("E to exit");

        if (CanReroll())
        {
            int cost = GetRerollCost();
            actions.Add($"R to refresh ({cost} gold)");
        }

        if (actions.Count > 0)
            TolkWrapper.Speak(string.Join(", ", actions));
        else
            TolkWrapper.Speak("No actions available");
    }

    private void AnnounceHeroStat()
    {
        var player = Data.Run?.Player;
        if (player == null) { TolkWrapper.Speak("No hero data"); return; }

        var type = HeroStats[_heroStatIndex];
        var value = player.GetAttributeValue(type);
        string name = GetStatName(type);

        TolkWrapper.Speak(value.HasValue ? $"{name}: {value.Value}" : $"{name}: none");
    }

    private void ReadAllHeroStats()
    {
        var player = Data.Run?.Player;
        if (player == null) { TolkWrapper.Speak("No hero data"); return; }

        var parts = new List<string>();

        var health = player.GetAttributeValue(EPlayerAttributeType.Health);
        var maxHealth = player.GetAttributeValue(EPlayerAttributeType.HealthMax);
        if (health.HasValue && maxHealth.HasValue)
            parts.Add($"Health {health.Value} of {maxHealth.Value}");

        var gold = player.GetAttributeValue(EPlayerAttributeType.Gold);
        if (gold.HasValue) parts.Add($"Gold {gold.Value}");

        var level = player.GetAttributeValue(EPlayerAttributeType.Level);
        if (level.HasValue) parts.Add($"Level {level.Value}");

        var shield = player.GetAttributeValue(EPlayerAttributeType.Shield);
        if (shield.HasValue && shield.Value > 0) parts.Add($"Shield {shield.Value}");

        TolkWrapper.Speak(string.Join(", ", parts));
    }

    private string GetStatName(EPlayerAttributeType type) => type switch
    {
        EPlayerAttributeType.Health => "Health",
        EPlayerAttributeType.HealthMax => "Max Health",
        EPlayerAttributeType.Gold => "Gold",
        EPlayerAttributeType.Level => "Level",
        EPlayerAttributeType.Experience => "Experience",
        EPlayerAttributeType.Shield => "Shield",
        EPlayerAttributeType.Poison => "Poison",
        EPlayerAttributeType.Burn => "Burn",
        EPlayerAttributeType.HealthRegen => "Regeneration",
        EPlayerAttributeType.CritChance => "Crit Chance",
        EPlayerAttributeType.Income => "Income",
        _ => type.ToString()
    };

    // --- Acceso a datos ---

    /// <summary>
    /// Obtiene el NavItem actual en la sección Selection.
    /// </summary>
    public NavItem GetCurrentNavItem()
    {
        if (_currentSection != NavigationSection.Selection) return null;
        if (_currentIndex < 0 || _currentIndex >= _selectionItems.Count) return null;
        return _selectionItems[_currentIndex];
    }

    public Card GetCurrentCard()
    {
        var bm = GetBoardManager();

        switch (_currentSection)
        {
            case NavigationSection.Selection:
                if (_currentIndex < _selectionItems.Count)
                {
                    var navItem = _selectionItems[_currentIndex];
                    return navItem.Type == NavItemType.Card ? navItem.Card : null;
                }
                break;

            case NavigationSection.Board:
                if (_currentIndex < _boardIndices.Count && bm != null)
                {
                    int idx = _boardIndices[_currentIndex];
                    return bm.playerItemSockets[idx]?.CardController?.CardData;
                }
                break;

            case NavigationSection.Stash:
                if (_currentIndex < _stashIndices.Count && bm != null)
                {
                    int idx = _stashIndices[_currentIndex];
                    return bm.playerStorageSockets[idx]?.CardController?.CardData;
                }
                break;

            case NavigationSection.Skills:
                if (_currentIndex < _skillIndices.Count && bm != null)
                {
                    int idx = _skillIndices[_currentIndex];
                    return bm.playerSkillSockets[idx]?.CardController?.CardData;
                }
                break;
        }
        return null;
    }

    public bool IsInSelectionSection() => _currentSection == NavigationSection.Selection;
    public bool IsInPlayerSection() => _currentSection == NavigationSection.Board ||
                                        _currentSection == NavigationSection.Stash;

    public bool HasContent() => GetCurrentSectionCount() > 0;

    public bool IsSelectionFree()
    {
        try { return Data.CurrentState?.SelectionContextRules?.SelectionIsFree ?? false; }
        catch { return false; }
    }

    public bool CanSellInCurrentState()
    {
        var state = AppState.CurrentState;
        return state?.CanHandleOperation(StateOps.SellItem) ?? false;
    }

    public bool CanMoveInCurrentState()
    {
        var state = AppState.CurrentState;
        return state?.CanHandleOperation(StateOps.MoveItem) ?? false;
    }

    // --- Helpers ---

    private int GetCurrentSectionCount() => _currentSection switch
    {
        NavigationSection.Selection => _selectionItems.Count,
        NavigationSection.Board => _boardIndices.Count,
        NavigationSection.Stash => _stashIndices.Count,
        NavigationSection.Skills => _skillIndices.Count,
        NavigationSection.Hero => HeroStats.Length,
        _ => 0
    };

    private string GetCardDescription(Card card)
    {
        // En selección
        if (_currentSection == NavigationSection.Selection)
        {
            if (IsEncounterCard(card))
                return ItemReader.GetEncounterInfo(card);

            if (IsSelectionFree())
                return ItemReader.GetCardName(card);

            int price = ItemReader.GetBuyPrice(card);
            return price > 0 ? $"{ItemReader.GetCardName(card)}, {price} gold" : ItemReader.GetCardName(card);
        }

        // En tablero/stash - mostrar precio de venta
        string name = ItemReader.GetCardName(card);
        int sellPrice = ItemReader.GetSellPrice(card);
        return sellPrice > 0 ? $"{name}, sell {sellPrice}" : name;
    }

    private bool IsEncounterCard(Card card) =>
        card.Type == ECardType.CombatEncounter ||
        card.Type == ECardType.EventEncounter ||
        card.Type == ECardType.PedestalEncounter ||
        card.Type == ECardType.EncounterStep ||
        card.Type == ECardType.PvpEncounter;

    private BoardManager GetBoardManager()
    {
        try { return Singleton<BoardManager>.Instance; }
        catch { return null; }
    }
}
