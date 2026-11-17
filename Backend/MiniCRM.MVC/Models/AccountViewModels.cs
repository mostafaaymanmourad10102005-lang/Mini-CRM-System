using System.ComponentModel.DataAnnotations;

namespace MiniCRM.MVC.Models;

public sealed class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    [Display(Name = "Keep me signed in")] public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public sealed class RegisterViewModel
{
    [Required, StringLength(80)] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(80)] public string LastName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(320)] public string Email { get; set; } = string.Empty;
    [Required, MinLength(12), DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    [Required, Compare(nameof(Password)), DataType(DataType.Password), Display(Name = "Confirm password")] public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password), Display(Name = "Current password")] public string CurrentPassword { get; set; } = string.Empty;
    [Required, MinLength(12), DataType(DataType.Password), Display(Name = "New password")] public string NewPassword { get; set; } = string.Empty;
    [Required, Compare(nameof(NewPassword)), DataType(DataType.Password), Display(Name = "Confirm new password")] public string ConfirmPassword { get; set; } = string.Empty;
}
