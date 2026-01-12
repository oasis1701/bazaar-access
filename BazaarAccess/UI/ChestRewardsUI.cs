using System.Collections.Generic;
using BazaarAccess.Accessibility;
using BazaarAccess.Core;
using BazaarAccess.Screens;
using BazaarGameShared.TempoNet.Models;
using TheBazaar.Feature.Chest.Scene;
using UnityEngine;

namespace BazaarAccess.UI;

/// <summary>
/// Accessible UI for displaying chest rewards after opening.
/// Only Enter closes this popup - user must explicitly confirm.
/// </summary>
public class ChestRewardsUI : BaseUI
{
    public override string UIName => "Chest Rewards";

    private List<PlayerChestInventory.ChestRewardResponse> _rewards;
    private int _currentRewardIndex = 0;

    public ChestRewardsUI(Transform root, List<PlayerChestInventory.ChestRewardResponse> rewards) : base(root)
    {
        _rewards = rewards ?? new List<PlayerChestInventory.ChestRewardResponse>();
    }

    protected override void BuildMenu()
    {
        // Just a single option to continue
        Menu.AddOption(
            () => "Press Enter to continue",
            () => Close());
    }

    public override void OnFocus()
    {
        // Announce all rewards
        if (_rewards.Count == 0)
        {
            TolkWrapper.Speak("No rewards. Press Enter to continue.");
            return;
        }

        var rewardTexts = new List<string>();

        foreach (var reward in _rewards)
        {
            var parts = new List<string>();

            // Collection item
            if (reward.collectibleItem != null && !string.IsNullOrEmpty(reward.collectibleItem.itemId))
            {
                string itemName = GetCollectibleName(reward.collectibleItem);
                if (reward.IsDuplicate)
                {
                    parts.Add($"{itemName} (duplicate)");
                }
                else
                {
                    parts.Add(itemName);
                }
            }

            // Gems
            if (reward.gems > 0)
            {
                parts.Add($"{reward.gems} gems");
            }

            // Duplicate gems (extra gems for duplicate)
            if (reward.DuplicateGems > 0)
            {
                parts.Add($"{reward.DuplicateGems} bonus gems for duplicate");
            }

            // Ranked vouchers
            if (reward.rankedVouchers > 0)
            {
                parts.Add($"{reward.rankedVouchers} ranked vouchers");
            }

            // Bonus chests
            if (reward.bonusChest != null && reward.bonusChest.Length > 0)
            {
                parts.Add($"{reward.bonusChest.Length} bonus chest{(reward.bonusChest.Length > 1 ? "s" : "")}");
            }

            // Rarity
            string rarity = reward.itemRarity.ToString();

            if (parts.Count > 0)
            {
                rewardTexts.Add($"{rarity}: {string.Join(", ", parts)}");
            }
        }

        if (rewardTexts.Count == 0)
        {
            TolkWrapper.Speak("Chest opened. Press Enter to continue.");
        }
        else if (rewardTexts.Count == 1)
        {
            TolkWrapper.Speak($"You received: {rewardTexts[0]}. Press Enter to continue.");
        }
        else
        {
            TolkWrapper.Speak($"You received {_rewards.Count} rewards: {string.Join(". ", rewardTexts)}. Press Enter to continue.");
        }
    }

    private string GetCollectibleName(BazaarCollectionItem item)
    {
        if (item == null) return "Unknown item";

        // The collectible item has itemId which is a GUID/asset reference
        if (!string.IsNullOrEmpty(item.itemId))
        {
            return "Collection item";
        }

        return "Item";
    }

    private void Close()
    {
        AccessibilityMgr.PopUI();

        // Return to chest selection state
        var screen = AccessibilityMgr.GetCurrentScreen() as ChestSceneScreen;
        screen?.ReturnToSelection();
    }

    protected override void OnBack()
    {
        // Only Enter closes, not Escape - do nothing
    }

    public override void HandleInput(AccessibleKey key)
    {
        // Only Enter closes the rewards popup
        if (key == AccessibleKey.Confirm)
        {
            Close();
            return;
        }

        // Navigate rewards with up/down if there are multiple
        if (_rewards.Count > 1)
        {
            switch (key)
            {
                case AccessibleKey.Up:
                    if (_currentRewardIndex > 0)
                    {
                        _currentRewardIndex--;
                        ReadCurrentReward();
                    }
                    return;

                case AccessibleKey.Down:
                    if (_currentRewardIndex < _rewards.Count - 1)
                    {
                        _currentRewardIndex++;
                        ReadCurrentReward();
                    }
                    return;
            }
        }

        // Ignore all other keys - don't pass to base
    }

    private void ReadCurrentReward()
    {
        if (_currentRewardIndex < 0 || _currentRewardIndex >= _rewards.Count) return;

        var reward = _rewards[_currentRewardIndex];
        var parts = new List<string>();

        parts.Add($"Reward {_currentRewardIndex + 1} of {_rewards.Count}");

        if (reward.collectibleItem != null && !string.IsNullOrEmpty(reward.collectibleItem.itemId))
        {
            string itemName = GetCollectibleName(reward.collectibleItem);
            if (reward.IsDuplicate)
            {
                parts.Add($"{itemName} (duplicate)");
            }
            else
            {
                parts.Add(itemName);
            }
        }

        if (reward.gems > 0)
        {
            parts.Add($"{reward.gems} gems");
        }

        if (reward.DuplicateGems > 0)
        {
            parts.Add($"{reward.DuplicateGems} bonus gems");
        }

        TolkWrapper.Speak(string.Join(". ", parts));
    }
}
