using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniCRM.Infrastructure;

namespace MiniCRM.API.Controllers;

public sealed record UserListDto(int Id, string Email, string FirstName, string LastName, bool IsActive, IReadOnlyList<string> Roles);
public sealed record SetUserRoleRequest([Required] string Role);

[ApiController, Authorize(Roles = "Admin"), Route("api/users")]
public sealed class UsersController(UserManager<CrmUser> users, RoleManager<IdentityRole<int>> roles, CrmDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery]string? search, [FromQuery]int page=1, [FromQuery]int pageSize=20, CancellationToken ct=default)
    {
        page=Math.Max(1,page);pageSize=Math.Clamp(pageSize,1,100);var query=users.Users.AsNoTracking();
        if(!string.IsNullOrWhiteSpace(search))query=query.Where(x=>(x.Email??"").Contains(search)||(x.FirstName+" "+x.LastName).Contains(search));
        var total=await query.CountAsync(ct);var entities=await query.OrderBy(x=>x.Email).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct);
        var items=new List<UserListDto>();foreach(var user in entities)items.Add(new(user.Id,user.Email??"",user.FirstName,user.LastName,user.IsActive,(await users.GetRolesAsync(user)).ToArray()));
        return Ok(new { items, pageNumber=page, pageSize, totalCount=total });
    }
    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> SetRole(int id, SetUserRoleRequest request)
    {
        if(!await roles.RoleExistsAsync(request.Role))return BadRequest(new{error="Unknown role."});
        var user=await users.FindByIdAsync(id.ToString());if(user is null)return NotFound();
        if(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value==id.ToString()&&request.Role!="Admin")return Conflict(new{error="You cannot remove your own administrator role."});
        var previous=await users.GetRolesAsync(user);var removed=await users.RemoveFromRolesAsync(user,previous);if(!removed.Succeeded)return BadRequest(new{errors=removed.Errors.Select(x=>x.Description)});
        var added=await users.AddToRoleAsync(user,request.Role);if(!added.Succeeded)return BadRequest(new{errors=added.Errors.Select(x=>x.Description)});
        await users.UpdateSecurityStampAsync(user);
        return NoContent();
    }
    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id,CancellationToken ct)
    {
        if(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value==id.ToString())return Conflict(new{error="You cannot deactivate your own account."});
        var user=await users.FindByIdAsync(id.ToString());if(user is null)return NotFound();user.IsActive=false;await users.UpdateSecurityStampAsync(user);var result=await users.UpdateAsync(user);
        if(!result.Succeeded)return BadRequest(new{errors=result.Errors.Select(x=>x.Description)});
        await db.RefreshTokens.Where(x=>x.UserId==id&&x.RevokedAt==null).ExecuteUpdateAsync(setters=>setters.SetProperty(x=>x.RevokedAt,DateTime.UtcNow),ct);
        return NoContent();
    }
}
