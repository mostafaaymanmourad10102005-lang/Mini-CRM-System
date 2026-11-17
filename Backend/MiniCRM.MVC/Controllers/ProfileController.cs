using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiniCRM.Infrastructure;
using MiniCRM.MVC.Models;

namespace MiniCRM.MVC.Controllers;

[Authorize]
public sealed class ProfileController(UserManager<CrmUser> users,SignInManager<CrmUser> signIn) : Controller
{
    [HttpGet] public async Task<IActionResult> Index(){var u=await users.GetUserAsync(User);if(u is null)return Challenge();ViewBag.Email=u.Email;ViewBag.Name=$"{u.FirstName} {u.LastName}";return View(new ChangePasswordViewModel());}
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Index(ChangePasswordViewModel model)
    {
        if(!ModelState.IsValid)return View(model);var user=await users.GetUserAsync(User);if(user is null)return Challenge();
        var result=await users.ChangePasswordAsync(user,model.CurrentPassword,model.NewPassword);
        if(!result.Succeeded){foreach(var e in result.Errors)ModelState.AddModelError(string.Empty,e.Description);ViewBag.Email=user.Email;ViewBag.Name=$"{user.FirstName} {user.LastName}";return View(model);}
        await users.UpdateSecurityStampAsync(user);await signIn.RefreshSignInAsync(user);TempData["Success"]="Password updated. Other sessions must sign in again.";return RedirectToAction(nameof(Index));
    }
}
