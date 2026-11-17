namespace MiniCRM.Domain;

public enum CustomerStatus { Active, Inactive, Prospect }
public enum LeadStatus { New, Contacted, Qualified, Converted, Lost }
public enum OpportunityStage { Qualification, NeedsAnalysis, Proposal, Negotiation, Won, Lost }
public enum TaskPriority { Low, Medium, High, Urgent }
public enum TaskStatus { Open, InProgress, Completed, Cancelled }
public enum ActivityType { Call, Email, Meeting, Note }

public abstract class Entity { public int Id { get; set; } }
public sealed class Customer : Entity
{
    public string FirstName { get; set; } = ""; public string LastName { get; set; } = "";
    public string Email { get; set; } = ""; public string? PhoneNumber { get; set; }
    public string? CompanyName { get; set; } public string? JobTitle { get; set; }
    public string? Address { get; set; } public string? City { get; set; } public string? Country { get; set; }
    public string? Source { get; set; } public CustomerStatus Status { get; set; } = CustomerStatus.Prospect;
    public int? AssignedToUserId { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } public List<Contact> Contacts { get; set; } = [];
}
public sealed class Contact : Entity
{
    public int CustomerId { get; set; } public Customer? Customer { get; set; }
    public string FullName { get; set; } = ""; public string? Email { get; set; }
    public string? PhoneNumber { get; set; } public string? JobTitle { get; set; } public bool IsPrimary { get; set; }
}
public sealed class Lead : Entity
{
    public string Title { get; set; } = ""; public int? CustomerId { get; set; }
    public string? ContactName { get; set; } public string? Email { get; set; } public string? PhoneNumber { get; set; }
    public string? Source { get; set; } public LeadStatus Status { get; set; } = LeadStatus.New;
    public decimal EstimatedValue { get; set; } public int? AssignedToUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public DateTime? UpdatedAt { get; set; }
}
public sealed class Opportunity : Entity
{
    public string Title { get; set; } = ""; public int CustomerId { get; set; } public int? LeadId { get; set; }
    public string? Description { get; set; } public decimal Amount { get; set; }
    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;
    public DateTime? ExpectedCloseDate { get; set; } public int? AssignedToUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public DateTime? UpdatedAt { get; set; }
}
public sealed class CrmTask : Entity
{
    public string Title { get; set; } = ""; public string? Description { get; set; } public DateTime? DueDate { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium; public TaskStatus Status { get; set; } = TaskStatus.Open;
    public int? AssignedToUserId { get; set; } public int? CustomerId { get; set; } public int? LeadId { get; set; }
    public int? OpportunityId { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public DateTime? CompletedAt { get; set; }
}
public sealed class Note : Entity
{
    public string Content { get; set; } = ""; public int? CustomerId { get; set; } public int? LeadId { get; set; }
    public int? OpportunityId { get; set; } public int CreatedByUserId { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public sealed class Activity : Entity
{
    public ActivityType ActivityType { get; set; } public string Description { get; set; } = "";
    public int? CustomerId { get; set; } public int? LeadId { get; set; } public int? OpportunityId { get; set; }
    public int CreatedByUserId { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
