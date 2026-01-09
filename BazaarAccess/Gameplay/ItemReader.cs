using System.Text;
using BazaarAccess.Core;
using BazaarGameClient.Domain.Models.Cards;
using BazaarGameShared.Domain.Core;
using BazaarGameShared.Domain.Core.Types;
using TheBazaar.AppFramework;
using TheBazaar.Localization;

namespace BazaarAccess.Gameplay;

/// <summary>
/// Lee información de items/cartas para accesibilidad.
/// </summary>
public static class ItemReader
{
    /// <summary>
    /// Obtiene el texto localizado de un TLocalizableText.
    /// </summary>
    public static string GetLocalizedText(TLocalizableText text)
    {
        if (text == null) return string.Empty;

        try
        {
            var locService = Services.Get<LocalizationService>();
            if (locService != null && locService.TryGetText(text, out var translation))
            {
                return translation;
            }
        }
        catch
        {
            // Fallback al texto por defecto
        }

        return text.Text ?? string.Empty;
    }

    /// <summary>
    /// Obtiene el nombre localizado de una carta.
    /// </summary>
    public static string GetCardName(Card card)
    {
        if (card == null) return "Empty";

        var template = card.Template;
        if (template?.Localization?.Title != null)
        {
            string name = GetLocalizedText(template.Localization.Title);
            if (!string.IsNullOrEmpty(name))
                return name;
        }

        // Fallback al nombre interno
        return template?.InternalName ?? "Unknown";
    }

    /// <summary>
    /// Obtiene el tier de una carta como string.
    /// </summary>
    public static string GetTierName(Card card)
    {
        if (card == null) return string.Empty;

        return card.Tier switch
        {
            ETier.Bronze => "Bronze",
            ETier.Silver => "Silver",
            ETier.Gold => "Gold",
            ETier.Diamond => "Diamond",
            ETier.Legendary => "Legendary",
            _ => card.Tier.ToString()
        };
    }

    /// <summary>
    /// Obtiene el precio de compra de un item.
    /// </summary>
    public static int GetBuyPrice(Card card)
    {
        if (card == null) return 0;
        return card.GetAttributeValue(ECardAttributeType.BuyPrice) ?? 0;
    }

    /// <summary>
    /// Obtiene el precio de venta de un item.
    /// </summary>
    public static int GetSellPrice(Card card)
    {
        if (card == null) return 0;
        return card.GetAttributeValue(ECardAttributeType.SellPrice) ?? 0;
    }

    /// <summary>
    /// Obtiene un resumen corto del item para navegación rápida.
    /// Formato: "Nombre, Tier"
    /// </summary>
    public static string GetShortDescription(Card card)
    {
        if (card == null) return "Empty slot";

        string name = GetCardName(card);
        string tier = GetTierName(card);

        return $"{name}, {tier}";
    }

    /// <summary>
    /// Obtiene información detallada del item.
    /// Incluye todos los stats y la descripción.
    /// </summary>
    public static string GetDetailedDescription(Card card)
    {
        if (card == null) return "Empty slot";

        var sb = new StringBuilder();

        // Nombre y tier
        sb.Append(GetCardName(card));
        sb.Append(", ");
        sb.Append(GetTierName(card));

        // Tamaño
        var template = card.Template;
        if (template != null)
        {
            sb.Append($", Size {(int)template.Size}");
        }

        // Cooldown (convertir de ms a segundos)
        var cooldown = card.GetAttributeValue(ECardAttributeType.Cooldown);
        if (cooldown.HasValue && cooldown.Value > 0)
        {
            float seconds = cooldown.Value / 1000f;
            sb.Append($", Cooldown {seconds:F1}s");
        }

        // Stats de combate
        AppendStatIfPresent(sb, card, ECardAttributeType.Ammo, "Ammo");
        AppendStatIfPresent(sb, card, ECardAttributeType.AmmoMax, "Max Ammo");
        AppendStatIfPresent(sb, card, ECardAttributeType.DamageAmount, "Damage");
        AppendStatIfPresent(sb, card, ECardAttributeType.HealAmount, "Heal");
        AppendStatIfPresent(sb, card, ECardAttributeType.ShieldApplyAmount, "Shield");
        AppendStatIfPresent(sb, card, ECardAttributeType.PoisonApplyAmount, "Poison");
        AppendStatIfPresent(sb, card, ECardAttributeType.BurnApplyAmount, "Burn");
        AppendStatIfPresent(sb, card, ECardAttributeType.RegenApplyAmount, "Regen");

        // Stats de velocidad
        AppendStatIfPresent(sb, card, ECardAttributeType.HasteAmount, "Haste");
        AppendStatIfPresent(sb, card, ECardAttributeType.SlowAmount, "Slow");
        AppendStatIfPresent(sb, card, ECardAttributeType.FreezeAmount, "Freeze");
        AppendStatIfPresent(sb, card, ECardAttributeType.ChargeAmount, "Charge");

        // Otros stats
        AppendStatIfPresent(sb, card, ECardAttributeType.CritChance, "Crit%");
        AppendStatIfPresent(sb, card, ECardAttributeType.Lifesteal, "Lifesteal");
        AppendStatIfPresent(sb, card, ECardAttributeType.Multicast, "Multicast");

        // Descripción del item
        string desc = GetDescription(card);
        if (!string.IsNullOrEmpty(desc))
        {
            sb.Append(". ");
            sb.Append(desc);
        }

        // Flavor text
        string flavor = GetFlavorText(card);
        if (!string.IsNullOrEmpty(flavor))
        {
            sb.Append(". ");
            sb.Append(flavor);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Obtiene información de precio para compra.
    /// </summary>
    public static string GetBuyInfo(Card card)
    {
        if (card == null) return string.Empty;

        string name = GetCardName(card);
        int price = GetBuyPrice(card);

        return $"{name}, {price} gold";
    }

    /// <summary>
    /// Obtiene información de precio para venta.
    /// </summary>
    public static string GetSellInfo(Card card)
    {
        if (card == null) return string.Empty;

        string name = GetCardName(card);
        int price = GetSellPrice(card);

        return $"{name}, sells for {price} gold";
    }

    private static void AppendStatIfPresent(StringBuilder sb, Card card, ECardAttributeType type, string label)
    {
        var value = card.GetAttributeValue(type);
        if (value.HasValue && value.Value > 0)
        {
            sb.Append($", {label} {value.Value}");
        }
    }

    /// <summary>
    /// Obtiene información básica de un encuentro.
    /// </summary>
    public static string GetEncounterInfo(Card card)
    {
        if (card == null) return "Empty";

        string name = GetCardName(card);
        string type = GetEncounterTypeName(card.Type);

        return $"{name}, {type}";
    }

    /// <summary>
    /// Obtiene información detallada de un encuentro.
    /// Incluye descripción y FlavorText.
    /// </summary>
    public static string GetEncounterDetailedInfo(Card card)
    {
        if (card == null) return "Empty";

        var sb = new StringBuilder();
        sb.Append(GetCardName(card));
        sb.Append(", ");
        sb.Append(GetEncounterTypeName(card.Type));

        // Descripción del encuentro
        string desc = GetDescription(card);
        if (!string.IsNullOrEmpty(desc))
        {
            sb.Append(". ");
            sb.Append(desc);
        }

        // Flavor text (historia/narrativa)
        string flavor = GetFlavorText(card);
        if (!string.IsNullOrEmpty(flavor))
        {
            sb.Append(". ");
            sb.Append(flavor);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Obtiene el nombre del tipo de encuentro.
    /// </summary>
    private static string GetEncounterTypeName(ECardType type)
    {
        return type switch
        {
            ECardType.CombatEncounter => "Combat",
            ECardType.EventEncounter => "Event",
            ECardType.PedestalEncounter => "Upgrade",
            ECardType.EncounterStep => "Path",
            ECardType.PvpEncounter => "PvP",
            _ => "Encounter"
        };
    }

    /// <summary>
    /// Obtiene el texto de sabor (FlavorText) de una carta.
    /// </summary>
    public static string GetFlavorText(Card card)
    {
        if (card == null) return string.Empty;

        var template = card.Template;
        if (template?.Localization?.FlavorText != null)
        {
            return GetLocalizedText(template.Localization.FlavorText);
        }

        return string.Empty;
    }

    /// <summary>
    /// Obtiene la descripción de una carta.
    /// </summary>
    public static string GetDescription(Card card)
    {
        if (card == null) return string.Empty;

        var template = card.Template;
        if (template?.Localization?.Description != null)
        {
            return GetLocalizedText(template.Localization.Description);
        }

        return string.Empty;
    }
}
