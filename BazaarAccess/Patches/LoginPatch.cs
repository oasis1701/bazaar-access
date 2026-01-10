using System;
using BazaarAccess.Accessibility;
using BazaarAccess.UI.Login;
using HarmonyLib;
using Managers.Login;

namespace BazaarAccess.Patches;

/// <summary>
/// Patches de Harmony para las vistas de login/cuenta.
/// </summary>

// === Landing Screen ===
[HarmonyPatch(typeof(LandingStateView), "OnEnable")]
public static class LandingShowPatch
{
    [HarmonyPostfix]
    public static void Postfix(LandingStateView __instance)
    {
        try
        {
            var ui = new LandingUI(__instance.transform, __instance);
            AccessibilityMgr.ShowUI(ui);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"LandingShowPatch error: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(LandingStateView), "OnDisable")]
public static class LandingHidePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        AccessibilityMgr.PopUI();
    }
}

// === Login Screen ===
[HarmonyPatch(typeof(LoginStateView), "OnEnable")]
public static class LoginShowPatch
{
    [HarmonyPostfix]
    public static void Postfix(LoginStateView __instance)
    {
        try
        {
            var ui = new LoginUI(__instance.transform, __instance);
            AccessibilityMgr.ShowUI(ui);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"LoginShowPatch error: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(LoginStateView), "OnDisable")]
public static class LoginHidePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        AccessibilityMgr.PopUI();
    }
}

// === Create Account Email ===
[HarmonyPatch(typeof(CreateAccountEmailStateView), "OnEnable")]
public static class CreateAccountEmailShowPatch
{
    [HarmonyPostfix]
    public static void Postfix(CreateAccountEmailStateView __instance)
    {
        try
        {
            var ui = new CreateAccountEmailUI(__instance.transform, __instance);
            AccessibilityMgr.ShowUI(ui);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"CreateAccountEmailShowPatch error: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(CreateAccountEmailStateView), "OnDisable")]
public static class CreateAccountEmailHidePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        AccessibilityMgr.PopUI();
    }
}

// === Create Account Username/Password ===
[HarmonyPatch(typeof(CreateAccountUserNamePasswordStateView), "OnEnable")]
public static class CreateAccountUserPasswordShowPatch
{
    [HarmonyPostfix]
    public static void Postfix(CreateAccountUserNamePasswordStateView __instance)
    {
        try
        {
            var ui = new CreateAccountUserPasswordUI(__instance.transform, __instance);
            AccessibilityMgr.ShowUI(ui);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"CreateAccountUserPasswordShowPatch error: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(CreateAccountUserNamePasswordStateView), "OnDisable")]
public static class CreateAccountUserPasswordHidePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        AccessibilityMgr.PopUI();
    }
}

// === Create Account Terms ===
[HarmonyPatch(typeof(CreateAccountTermsStateView), "OnEnable")]
public static class CreateAccountTermsShowPatch
{
    [HarmonyPostfix]
    public static void Postfix(CreateAccountTermsStateView __instance)
    {
        try
        {
            var ui = new CreateAccountTermsUI(__instance.transform, __instance);
            AccessibilityMgr.ShowUI(ui);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"CreateAccountTermsShowPatch error: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(CreateAccountTermsStateView), "OnDisable")]
public static class CreateAccountTermsHidePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        AccessibilityMgr.PopUI();
    }
}

// === Forgot Password ===
[HarmonyPatch(typeof(ForgotPasswordStateView), "OnEnable")]
public static class ForgotPasswordShowPatch
{
    [HarmonyPostfix]
    public static void Postfix(ForgotPasswordStateView __instance)
    {
        try
        {
            var ui = new ForgotPasswordUI(__instance.transform, __instance);
            AccessibilityMgr.ShowUI(ui);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"ForgotPasswordShowPatch error: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(ForgotPasswordStateView), "OnDisable")]
public static class ForgotPasswordHidePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        AccessibilityMgr.PopUI();
    }
}

// === Reset Email ===
[HarmonyPatch(typeof(ResetEmailStateView), "OnEnable")]
public static class ResetEmailShowPatch
{
    [HarmonyPostfix]
    public static void Postfix(ResetEmailStateView __instance)
    {
        try
        {
            var ui = new ResetEmailUI(__instance.transform, __instance);
            AccessibilityMgr.ShowUI(ui);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"ResetEmailShowPatch error: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(ResetEmailStateView), "OnDisable")]
public static class ResetEmailHidePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        AccessibilityMgr.PopUI();
    }
}

// === Forgot Password Confirm ===
[HarmonyPatch(typeof(ForgotPasswordConfirmView), "OnEnable")]
public static class ForgotPasswordConfirmShowPatch
{
    [HarmonyPostfix]
    public static void Postfix(ForgotPasswordConfirmView __instance)
    {
        try
        {
            var ui = new ForgotPasswordConfirmUI(__instance.transform, __instance);
            AccessibilityMgr.ShowUI(ui);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"ForgotPasswordConfirmShowPatch error: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(ForgotPasswordConfirmView), "OnDisable")]
public static class ForgotPasswordConfirmHidePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        AccessibilityMgr.PopUI();
    }
}

// === Account Verified ===
[HarmonyPatch(typeof(AccountVerifiedStateView), "OnEnable")]
public static class AccountVerifiedShowPatch
{
    [HarmonyPostfix]
    public static void Postfix(AccountVerifiedStateView __instance)
    {
        try
        {
            var ui = new AccountVerifiedUI(__instance.transform, __instance);
            AccessibilityMgr.ShowUI(ui);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"AccountVerifiedShowPatch error: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(AccountVerifiedStateView), "OnDisable")]
public static class AccountVerifiedHidePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        AccessibilityMgr.PopUI();
    }
}

// === Registration Failed ===
[HarmonyPatch(typeof(RegistrationFailedStateView), "OnEnable")]
public static class RegistrationFailedShowPatch
{
    [HarmonyPostfix]
    public static void Postfix(RegistrationFailedStateView __instance)
    {
        try
        {
            var ui = new RegistrationFailedUI(__instance.transform, __instance);
            AccessibilityMgr.ShowUI(ui);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"RegistrationFailedShowPatch error: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(RegistrationFailedStateView), "OnDisable")]
public static class RegistrationFailedHidePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        AccessibilityMgr.PopUI();
    }
}

// === Access Denied ===
[HarmonyPatch(typeof(AccessDeniedStateView), "OnEnable")]
public static class AccessDeniedShowPatch
{
    [HarmonyPostfix]
    public static void Postfix(AccessDeniedStateView __instance)
    {
        try
        {
            var ui = new AccessDeniedUI(__instance.transform, __instance);
            AccessibilityMgr.ShowUI(ui);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"AccessDeniedShowPatch error: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(AccessDeniedStateView), "OnDisable")]
public static class AccessDeniedHidePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        AccessibilityMgr.PopUI();
    }
}
