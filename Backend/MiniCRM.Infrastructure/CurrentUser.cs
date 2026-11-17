using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MiniCRM.Application;

namespace MiniCRM.Infrastructure;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;
    public int? UserId => int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public bool IsAdministratorOrManager => Principal?.IsInRole("Admin") == true || Principal?.IsInRole("Manager") == true;
}
