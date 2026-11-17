using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniCRM.Infrastructure;
using MiniCRM.MVC.Models;

namespace MiniCRM.MVC.Controllers;

[Authorize(Roles="Admin")]
public sealed class UsersController(UserManager<CrmUser> users,RoleManager<IdentityRole<int>> roles) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var items=await users.Users.AsNoTracking().OrderBy(x=>x.Email).ToListAsync(ct);var rows=new List<UserRowViewModel>();
        foreach(var user in items){var assigned=await users.GetRolesAsync(user);rows.Add(new(user.Id.ToString(),$"{user.FirstName} {user.LastName}",user.Email??"",assigned.FirstOrDefault()??"—",user.IsActive));}
        return View(rows);
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(UserAdminViewModel model)
    {
        string[] allowed=["Admin","Manager","SalesRepresentative"];
        if(!ModelState.IsValid||!allowed.Contains(model.Role)){TempData["Error"]="Select a valid account role.";return RedirectToAction(nameof(Index));}
        if(!int.TryParse(model.Id,out var id)){return NotFound();}var target=await users.FindByIdAsync(id.ToString());if(target is null)return NotFound();
        if(target.Id.ToString()==users.GetUserId(User) && (!model.IsActive || model.Role!="Admin")){TempData["Error"]="You cannot disable or demote your own admin account.";return RedirectToAction(nameof(Index));}
        if(!await roles.RoleExistsAsync(model.Role)){TempData["Error"]="The selected role is unavailable.";return RedirectToAction(nameof(Index));}
        var old=await users.GetRolesAsync(target);var removed=await users.RemoveFromRolesAsync(target,old);if(!removed.Succeeded){TempData["Error"]=string.Join(" ",removed.Errors.Select(x=>x.Description));return RedirectToAction(nameof(Index));}
        var added=await users.AddToRoleAsync(target,model.Role);if(!added.Succeeded){await users.AddToRolesAsync(target,old);TempData["Error"]=string.Join(" ",added.Errors.Select(x=>x.Description));return RedirectToAction(nameof(Index));}
        target.IsActive=model.IsActive;await users.UpdateSecurityStampAsync(target);var saved=await users.UpdateAsync(target);
        TempData[saved.Succeeded?"Success":"Error"]=saved.Succeeded?"User updated; existing sessions were invalidated.":string.Join(" ",saved.Errors.Select(x=>x.Description));return RedirectToAction(nameof(Index));
    }
}
