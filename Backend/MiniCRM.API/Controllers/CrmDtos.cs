using System.ComponentModel.DataAnnotations;
using MiniCRM.Application;
using MiniCRM.Domain;

namespace MiniCRM.API.Controllers;

public sealed record CustomerDto(int Id,string FirstName,string LastName,string Email,string? PhoneNumber,string? CompanyName,string? JobTitle,string? Address,string? City,string? Country,string? Source,CustomerStatus Status,int? AssignedToUserId,DateTime CreatedAt,DateTime? UpdatedAt,IReadOnlyList<ContactDto> Contacts)
{
    public static CustomerDto From(Customer x)=>new(x.Id,x.FirstName,x.LastName,x.Email,x.PhoneNumber,x.CompanyName,x.JobTitle,x.Address,x.City,x.Country,x.Source,x.Status,x.AssignedToUserId,x.CreatedAt,x.UpdatedAt,x.Contacts.Select(ContactDto.From).ToArray());
}
public sealed class CustomerWriteRequest
{
    [Required,MaxLength(80)] public string FirstName{get;set;}=string.Empty;[Required,MaxLength(80)] public string LastName{get;set;}=string.Empty;
    [Required,EmailAddress,MaxLength(320)] public string Email{get;set;}=string.Empty;[Phone,MaxLength(32)] public string? PhoneNumber{get;set;}
    [MaxLength(160)] public string? CompanyName{get;set;}[MaxLength(120)] public string? JobTitle{get;set;}[MaxLength(300)] public string? Address{get;set;}
    [MaxLength(100)] public string? City{get;set;}[MaxLength(100)] public string? Country{get;set;}[MaxLength(80)] public string? Source{get;set;}
    public CustomerStatus Status{get;set;}=CustomerStatus.Prospect;public int? AssignedToUserId{get;set;}
    public Customer ToEntity(int id=0)=>new(){Id=id,FirstName=FirstName,LastName=LastName,Email=Email,PhoneNumber=PhoneNumber,CompanyName=CompanyName,JobTitle=JobTitle,Address=Address,City=City,Country=Country,Source=Source,Status=Status,AssignedToUserId=AssignedToUserId};
}

public sealed record ContactDto(int Id,int CustomerId,string FullName,string? Email,string? PhoneNumber,string? JobTitle,bool IsPrimary){public static ContactDto From(Contact x)=>new(x.Id,x.CustomerId,x.FullName,x.Email,x.PhoneNumber,x.JobTitle,x.IsPrimary);}
public sealed class ContactWriteRequest{public int CustomerId{get;set;}[Required,MaxLength(160)]public string FullName{get;set;}=string.Empty;[EmailAddress,MaxLength(320)]public string? Email{get;set;}[Phone,MaxLength(32)]public string? PhoneNumber{get;set;}[MaxLength(120)]public string? JobTitle{get;set;}public bool IsPrimary{get;set;}public Contact ToEntity(int id=0)=>new(){Id=id,CustomerId=CustomerId,FullName=FullName,Email=Email,PhoneNumber=PhoneNumber,JobTitle=JobTitle,IsPrimary=IsPrimary};}

public sealed record LeadDto(int Id,string Title,int? CustomerId,string? ContactName,string? Email,string? PhoneNumber,string? Source,LeadStatus Status,decimal EstimatedValue,int? AssignedToUserId,DateTime CreatedAt,DateTime? UpdatedAt){public static LeadDto From(Lead x)=>new(x.Id,x.Title,x.CustomerId,x.ContactName,x.Email,x.PhoneNumber,x.Source,x.Status,x.EstimatedValue,x.AssignedToUserId,x.CreatedAt,x.UpdatedAt);}
public sealed class LeadWriteRequest{[Required,MaxLength(180)]public string Title{get;set;}=string.Empty;public int? CustomerId{get;set;}[MaxLength(160)]public string? ContactName{get;set;}[EmailAddress,MaxLength(320)]public string? Email{get;set;}[Phone,MaxLength(32)]public string? PhoneNumber{get;set;}[MaxLength(80)]public string? Source{get;set;}public LeadStatus Status{get;set;}=LeadStatus.New;[Range(typeof(decimal),"0","9999999999999999")]public decimal EstimatedValue{get;set;}public int? AssignedToUserId{get;set;}public Lead ToEntity(int id=0)=>new(){Id=id,Title=Title,CustomerId=CustomerId,ContactName=ContactName,Email=Email,PhoneNumber=PhoneNumber,Source=Source,Status=Status,EstimatedValue=EstimatedValue,AssignedToUserId=AssignedToUserId};}

public sealed record OpportunityDto(int Id,string Title,int CustomerId,int? LeadId,string? Description,decimal Amount,OpportunityStage Stage,DateTime? ExpectedCloseDate,int? AssignedToUserId,DateTime CreatedAt,DateTime? UpdatedAt){public static OpportunityDto From(Opportunity x)=>new(x.Id,x.Title,x.CustomerId,x.LeadId,x.Description,x.Amount,x.Stage,x.ExpectedCloseDate,x.AssignedToUserId,x.CreatedAt,x.UpdatedAt);}
public sealed class OpportunityWriteRequest{[Required,MaxLength(180)]public string Title{get;set;}=string.Empty;public int CustomerId{get;set;}public int? LeadId{get;set;}[MaxLength(2000)]public string? Description{get;set;}[Range(typeof(decimal),"0","9999999999999999")]public decimal Amount{get;set;}public OpportunityStage Stage{get;set;}=OpportunityStage.Qualification;public DateTime? ExpectedCloseDate{get;set;}public int? AssignedToUserId{get;set;}public Opportunity ToEntity(int id=0)=>new(){Id=id,Title=Title,CustomerId=CustomerId,LeadId=LeadId,Description=Description,Amount=Amount,Stage=Stage,ExpectedCloseDate=ExpectedCloseDate,AssignedToUserId=AssignedToUserId};}

public sealed record TaskDto(int Id,string Title,string? Description,DateTime? DueDate,TaskPriority Priority,MiniCRM.Domain.TaskStatus Status,int? AssignedToUserId,int? CustomerId,int? LeadId,int? OpportunityId,DateTime CreatedAt,DateTime? CompletedAt){public static TaskDto From(CrmTask x)=>new(x.Id,x.Title,x.Description,x.DueDate,x.Priority,x.Status,x.AssignedToUserId,x.CustomerId,x.LeadId,x.OpportunityId,x.CreatedAt,x.CompletedAt);}
public sealed class TaskWriteRequest{[Required,MaxLength(180)]public string Title{get;set;}=string.Empty;[MaxLength(2000)]public string? Description{get;set;}public DateTime? DueDate{get;set;}public TaskPriority Priority{get;set;}=TaskPriority.Medium;public MiniCRM.Domain.TaskStatus Status{get;set;}=MiniCRM.Domain.TaskStatus.Open;public int? AssignedToUserId{get;set;}public int? CustomerId{get;set;}public int? LeadId{get;set;}public int? OpportunityId{get;set;}public CrmTask ToEntity(int id=0)=>new(){Id=id,Title=Title,Description=Description,DueDate=DueDate,Priority=Priority,Status=Status,AssignedToUserId=AssignedToUserId,CustomerId=CustomerId,LeadId=LeadId,OpportunityId=OpportunityId};}

public sealed record NoteDto(int Id,string Content,int? CustomerId,int? LeadId,int? OpportunityId,DateTime CreatedAt){public static NoteDto From(Note x)=>new(x.Id,x.Content,x.CustomerId,x.LeadId,x.OpportunityId,x.CreatedAt);}
public sealed class NoteWriteRequest{[Required,MaxLength(8000)]public string Content{get;set;}=string.Empty;public int? CustomerId{get;set;}public int? LeadId{get;set;}public int? OpportunityId{get;set;}public Note ToEntity()=>new(){Content=Content,CustomerId=CustomerId,LeadId=LeadId,OpportunityId=OpportunityId};}
public sealed record ActivityDto(int Id,ActivityType ActivityType,string Description,int? CustomerId,int? LeadId,int? OpportunityId,DateTime CreatedAt){public static ActivityDto From(Activity x)=>new(x.Id,x.ActivityType,x.Description,x.CustomerId,x.LeadId,x.OpportunityId,x.CreatedAt);}
public sealed class ActivityWriteRequest{public ActivityType ActivityType{get;set;}[Required,MaxLength(4000)]public string Description{get;set;}=string.Empty;public int? CustomerId{get;set;}public int? LeadId{get;set;}public int? OpportunityId{get;set;}public Activity ToEntity()=>new(){ActivityType=ActivityType,Description=Description,CustomerId=CustomerId,LeadId=LeadId,OpportunityId=OpportunityId};}

public static class PageMap{public static Page<TDto> Map<TSource,TDto>(Page<TSource> source,Func<TSource,TDto> map)=>new(source.Items.Select(map).ToArray(),source.PageNumber,source.PageSize,source.TotalCount);}
