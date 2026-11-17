using System.ComponentModel.DataAnnotations;
using MiniCRM.Domain;

namespace MiniCRM.MVC.Models;

public sealed class RecordRowViewModel
{
    public int Id { get; init; }
    public string Primary { get; init; } = "";
    public string? Secondary { get; init; }
    public string? Status { get; init; }
    public string? Related { get; init; }
    public DateTime? Date { get; init; }
}

public sealed class RecordListViewModel
{
    public string Kind { get; init; } = "";
    public string Title { get; init; } = "";
    public IReadOnlyList<RecordRowViewModel> Items { get; init; } = [];
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int TotalCount { get; init; }
    public bool CanCreate { get; init; } = true;
}

public sealed class RecordFormViewModel : IValidatableObject
{
    public string Kind { get; set; } = "";
    public int Id { get; set; }
    [StringLength(8000)] public string? Content { get; set; }
    [StringLength(180)] public string? Title { get; set; }
    [StringLength(160)] public string? FullName { get; set; }
    [StringLength(160)] public string? ContactName { get; set; }
    [EmailAddress, StringLength(320)] public string? Email { get; set; }
    [Phone, StringLength(32)] public string? PhoneNumber { get; set; }
    [StringLength(80)] public string? Source { get; set; }
    [StringLength(2000)] public string? Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    public int? OpportunityId { get; set; }
    public string Status { get; set; } = "New";
    public string Stage { get; set; } = "Qualification";
    public string Priority { get; set; } = "Medium";
    public string ActivityType { get; set; } = "Call";
    public bool IsPrimary { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        bool Required(string? value, string field) => !string.IsNullOrWhiteSpace(value);
        string? missing = Kind switch
        {
            "leads" when !Required(Title, nameof(Title)) => "Title is required.",
            "opportunities" when !Required(Title, nameof(Title)) => "Title is required.",
            "tasks" when !Required(Title, nameof(Title)) => "Title is required.",
            "contacts" when !Required(FullName, nameof(FullName)) => "Full name is required.",
            "notes" when !Required(Content, nameof(Content)) => "Content is required.",
            "activities" when !Required(Description, nameof(Description)) => "Description is required.",
            "opportunities" when CustomerId is null or <= 0 => "A valid customer ID is required.",
            "contacts" when CustomerId is null or <= 0 => "A valid customer ID is required.",
            _ => null
        };
        if (missing is not null) yield return new ValidationResult(missing, [nameof(Title)]);
        if (Amount < 0) yield return new ValidationResult("Value must be zero or greater.", [nameof(Amount)]);
        if (Kind == "leads" && !Enum.TryParse<LeadStatus>(Status, out _)) yield return new ValidationResult("Choose a valid lead status.", [nameof(Status)]);
        if (Kind == "opportunities" && !Enum.TryParse<OpportunityStage>(Stage, out _)) yield return new ValidationResult("Choose a valid opportunity stage.", [nameof(Stage)]);
        if (Kind == "tasks" && (!Enum.TryParse<TaskPriority>(Priority, out _) || !Enum.TryParse<MiniCRM.Domain.TaskStatus>(Status, out _))) yield return new ValidationResult("Choose a valid task status and priority.", [nameof(Status), nameof(Priority)]);
        if (Kind == "activities" && !Enum.TryParse<ActivityType>(ActivityType, out _)) yield return new ValidationResult("Choose a valid activity type.", [nameof(ActivityType)]);
        if (Kind is "notes" or "activities" && CustomerId is null && LeadId is null && OpportunityId is null)
            yield return new ValidationResult("Choose a customer, lead, or opportunity.", [nameof(CustomerId)]);
        if (Kind is not ("leads" or "opportunities" or "tasks" or "contacts" or "notes" or "activities"))
            yield return new ValidationResult("Choose a valid record type.", [nameof(Kind)]);
    }

    public static RecordFormViewModel From(Lead x) => new() { Kind="leads", Id=x.Id, Title=x.Title, CustomerId=x.CustomerId, ContactName=x.ContactName, Email=x.Email, PhoneNumber=x.PhoneNumber, Source=x.Source, Status=x.Status.ToString(), Amount=x.EstimatedValue };
    public static RecordFormViewModel From(Opportunity x) => new() { Kind="opportunities", Id=x.Id, Title=x.Title, CustomerId=x.CustomerId, LeadId=x.LeadId, Description=x.Description, Amount=x.Amount, Stage=x.Stage.ToString(), ExpectedCloseDate=x.ExpectedCloseDate };
    public static RecordFormViewModel From(CrmTask x) => new() { Kind="tasks", Id=x.Id, Title=x.Title, Description=x.Description, DueDate=x.DueDate, Priority=x.Priority.ToString(), Status=x.Status.ToString(), CustomerId=x.CustomerId, LeadId=x.LeadId, OpportunityId=x.OpportunityId };
    public static RecordFormViewModel From(Contact x) => new() { Kind="contacts", Id=x.Id, CustomerId=x.CustomerId, FullName=x.FullName, Email=x.Email, PhoneNumber=x.PhoneNumber, Title=x.JobTitle, IsPrimary=x.IsPrimary };
    public static RecordFormViewModel From(Note x) => new() { Kind="notes", Id=x.Id, Content=x.Content, CustomerId=x.CustomerId, LeadId=x.LeadId, OpportunityId=x.OpportunityId };
    public static RecordFormViewModel From(Activity x) => new() { Kind="activities", Id=x.Id, ActivityType=x.ActivityType.ToString(), Description=x.Description, CustomerId=x.CustomerId, LeadId=x.LeadId, OpportunityId=x.OpportunityId };
}
