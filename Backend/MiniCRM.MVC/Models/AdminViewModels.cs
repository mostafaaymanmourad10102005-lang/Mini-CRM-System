using System.ComponentModel.DataAnnotations;

namespace MiniCRM.MVC.Models;

public sealed record UserRowViewModel(string Id,string Name,string Email,string Role,bool IsActive);
public sealed class UserAdminViewModel
{
    [Required] public string Id { get; set; } = "";
    [Required] public string Role { get; set; } = "SalesRepresentative";
    public bool IsActive { get; set; }
}
