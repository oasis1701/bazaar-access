using UnityEngine;

namespace BazaarAccess.UI.Login;

/// <summary>
/// UI accesible para el paso de usuario y contraseña de creación de cuenta.
/// </summary>
public class CreateAccountUserPasswordUI : LoginBaseUI
{
    private readonly object _view;

    public override string UIName => "Create Account - Username and Password";

    public CreateAccountUserPasswordUI(Transform root, object view) : base(root)
    {
        _view = view;
    }

    protected override void BuildMenu()
    {
        // Username field
        var usernameField = GetInputField(_view, "username");
        AddTextField("Username", usernameField);

        // Password field
        var passwordField = GetInputField(_view, "password");
        AddTextField("Password", passwordField);

        // Confirm Password field
        var confirmPasswordField = GetInputField(_view, "confirmPassword");
        AddTextField("Confirm Password", confirmPasswordField);

        // Continue button
        AddBazaarButton(_view, "continueButton", "Continue");
    }

    protected override void OnBack()
    {
        // Volver a la pantalla anterior
    }
}
