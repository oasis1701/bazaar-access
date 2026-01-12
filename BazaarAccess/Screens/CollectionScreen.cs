using System;
using System.Collections.Generic;
using BazaarAccess.Accessibility;
using BazaarAccess.Core;
using BazaarGameShared;
using TheBazaar.UI;
using UnityEngine;
using TMPro;

namespace BazaarAccess.Screens;

/// <summary>
/// Accessible screen for the collections menu.
/// Allows navigation through collection categories and items using the existing UI.
/// </summary>
public class CollectionScreen : BaseScreen
{
    public override string ScreenName => "Collection";

    private CollectionsScreenController _controller;
    private int _currentCategoryIndex = 0;

    // Collection types available
    private readonly string[] _categoryNames = new[]
    {
        "Hero Skins",
        "Boards",
        "Card Skins",
        "Carpets",
        "Card Backs",
        "Album"
    };

    public CollectionScreen(Transform root, CollectionsScreenController controller) : base(root)
    {
        _controller = controller;
    }

    protected override void BuildMenu()
    {
        // Back button
        Menu.AddOption(
            () => "Back",
            () => GoBack());

        // Category selector (navigate with left/right)
        Menu.AddOption(
            () => GetCurrentCategoryText(),
            () => ReadCurrentCategory(),
            (dir) => NavigateCategory(dir));

        // Navigate items with arrows when on this option
        Menu.AddOption(
            () => "Use arrows to browse items. Enter to select.",
            () => { });
    }

    private string GetCurrentCategoryText()
    {
        if (_currentCategoryIndex >= _categoryNames.Length)
            _currentCategoryIndex = 0;

        return $"Category: {_categoryNames[_currentCategoryIndex]}";
    }

    private void NavigateCategory(int direction)
    {
        _currentCategoryIndex += direction;
        if (_currentCategoryIndex < 0) _currentCategoryIndex = _categoryNames.Length - 1;
        if (_currentCategoryIndex >= _categoryNames.Length) _currentCategoryIndex = 0;

        TolkWrapper.Speak(GetCurrentCategoryText());
    }

    private void ReadCurrentCategory()
    {
        TolkWrapper.Speak(GetCurrentCategoryText());
    }

    private void GoBack()
    {
        // Click the back button in the UI
        ClickButtonByName("BackButton");
    }

    public override void OnFocus()
    {
        Plugin.Logger.LogInfo("CollectionScreen.OnFocus called");
        // Read the screen name and first option
        string message = $"{ScreenName}. {GetCurrentCategoryText()}";
        Plugin.Logger.LogInfo($"CollectionScreen speaking: {message}");
        TolkWrapper.Speak(message);
    }

    public override void HandleInput(AccessibleKey key)
    {
        switch (key)
        {
            case AccessibleKey.Back:
                GoBack();
                return;
        }

        base.HandleInput(key);
    }
}
