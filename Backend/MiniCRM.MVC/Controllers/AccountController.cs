using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiniCRM.Infrastructure;
using MiniCRM.MVC.Models;

namespace MiniCRM.MVC.Controllers;

public sealed class AccountController(UserManager<CrmUser> users, SignInManager<CrmUser> signIn) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.FindByEmailAsync(model.Email.Trim());
        if (user is null || !user.IsActive) { ModelState.AddModelError(string.Empty, "Email or password is incorrect."); return View(model); }
        var result = await signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded) { ModelState.AddModelError(string.Empty, "Email or password is incorrect, or the account is temporarily locked."); return View(model); }
        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)) return Redirect(model.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = new CrmUser { UserName = model.Email.Trim(), Email = model.Email.Trim(), FirstName = model.FirstName.Trim(), LastName = model.LastName.Trim(), CreatedAt = DateTime.UtcNow };
        var result = await users.CreateAsync(user, model.Password);
        if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description); return View(model); }
        var roleResult = await users.AddToRoleAsync(user, "SalesRepresentative");
        if (!roleResult.Succeeded) { await users.DeleteAsync(user); ModelState.AddModelError(string.Empty, "The default sales role is not available."); return View(model); }
        await signIn.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout() { await signIn.SignOutAsync(); return RedirectToAction(nameof(Login)); }

    [AllowAnonymous]
    public IActionResult Denied() => View();
}
