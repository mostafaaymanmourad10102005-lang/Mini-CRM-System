using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiniCRM.Infrastructure;

namespace MiniCRM.API.Controllers;

public sealed record UpdateProfileRequest([Required, MaxLength(80)] string FirstName, [Required, MaxLength(80)] string LastName);
public sealed record ProfileDto(int Id, string Email, string FirstName, string LastName, IReadOnlyList<string> Roles);

[ApiController, Authorize, Route("api/profile")]
public sealed class ProfileController(UserManager<CrmUser> users) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var user = await CurrentUserAsync(); return user is null ? Unauthorized() : Ok(await Map(user));
    }
    [HttpPut]
    public async Task<IActionResult> Update(UpdateProfileRequest request)
    {
        var user = await CurrentUserAsync(); if (user is null) return Unauthorized();
        user.FirstName = request.FirstName.Trim(); user.LastName = request.LastName.Trim();
        var result = await users.UpdateAsync(user); return result.Succeeded ? Ok(await Map(user)) : BadRequest(new { errors = result.Errors.Select(x => x.Description) });
    }
    private Task<CrmUser?> CurrentUserAsync() => users.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");
    private async Task<ProfileDto> Map(CrmUser user) => new(user.Id, user.Email ?? "", user.FirstName, user.LastName, (await users.GetRolesAsync(user)).ToArray());
}
