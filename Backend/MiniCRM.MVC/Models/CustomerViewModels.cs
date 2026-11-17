using System.ComponentModel.DataAnnotations;
using MiniCRM.Domain;

namespace MiniCRM.MVC.Models;

public sealed class CustomerFormViewModel
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(80)] public string LastName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(320)] public string Email { get; set; } = string.Empty;
    [Phone, StringLength(32)] public string? PhoneNumber { get; set; }
    [StringLength(160)] public string? CompanyName { get; set; }
    [StringLength(120)] public string? JobTitle { get; set; }
    [StringLength(300)] public string? Address { get; set; }
    [StringLength(100)] public string? City { get; set; }
    [StringLength(100)] public string? Country { get; set; }
    [StringLength(80)] public string? Source { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Prospect;
    public static CustomerFormViewModel From(Customer x) => new() { Id=x.Id, FirstName=x.FirstName, LastName=x.LastName, Email=x.Email, PhoneNumber=x.PhoneNumber, CompanyName=x.CompanyName, JobTitle=x.JobTitle, Address=x.Address, City=x.City, Country=x.Country, Source=x.Source, Status=x.Status };
    public Customer ToEntity() => new() { Id=Id, FirstName=FirstName, LastName=LastName, Email=Email, PhoneNumber=PhoneNumber, CompanyName=CompanyName, JobTitle=JobTitle, Address=Address, City=City, Country=Country, Source=Source, Status=Status };
}
