using FluentValidation;
using MiniCRM.Domain;

namespace MiniCRM.API.Validation;

public sealed class CustomerWriteRequestValidator:AbstractValidator<Controllers.CustomerWriteRequest>
{
    public CustomerWriteRequestValidator(){RuleFor(x=>x.FirstName).NotEmpty().MaximumLength(80);RuleFor(x=>x.LastName).NotEmpty().MaximumLength(80);RuleFor(x=>x.Email).NotEmpty().EmailAddress().MaximumLength(320);RuleFor(x=>x.Status).IsInEnum();}
}
public sealed class LeadWriteRequestValidator:AbstractValidator<Controllers.LeadWriteRequest>
{
    public LeadWriteRequestValidator(){RuleFor(x=>x.Title).NotEmpty().MaximumLength(180);RuleFor(x=>x.Email).EmailAddress().When(x=>!string.IsNullOrWhiteSpace(x.Email));RuleFor(x=>x.EstimatedValue).GreaterThanOrEqualTo(0);RuleFor(x=>x.Status).IsInEnum().Must(x=>x!=LeadStatus.Converted).WithMessage("Use the lead conversion endpoint to mark a lead converted.");}
}
public sealed class OpportunityWriteRequestValidator:AbstractValidator<Controllers.OpportunityWriteRequest>
{
    public OpportunityWriteRequestValidator(){RuleFor(x=>x.Title).NotEmpty().MaximumLength(180);RuleFor(x=>x.CustomerId).GreaterThan(0);RuleFor(x=>x.Amount).GreaterThanOrEqualTo(0);RuleFor(x=>x.Stage).IsInEnum();}
}
public sealed class TaskWriteRequestValidator:AbstractValidator<Controllers.TaskWriteRequest>
{
    public TaskWriteRequestValidator(){RuleFor(x=>x.Title).NotEmpty().MaximumLength(180);RuleFor(x=>x.Priority).IsInEnum();RuleFor(x=>x.Status).IsInEnum();RuleFor(x=>x.DueDate).Must(x=>x is null||x.Value.Kind==DateTimeKind.Utc).WithMessage("Due dates must include a UTC offset.");}
}
public sealed class ContactWriteRequestValidator:AbstractValidator<Controllers.ContactWriteRequest>
{
    public ContactWriteRequestValidator(){RuleFor(x=>x.CustomerId).GreaterThan(0);RuleFor(x=>x.FullName).NotEmpty().MaximumLength(160);RuleFor(x=>x.Email).EmailAddress().When(x=>!string.IsNullOrWhiteSpace(x.Email));}
}
public sealed class NoteWriteRequestValidator:AbstractValidator<Controllers.NoteWriteRequest>
{
    public NoteWriteRequestValidator(){RuleFor(x=>x.Content).NotEmpty().MaximumLength(8000);RuleFor(x=>x).Must(x=>x.CustomerId.HasValue||x.LeadId.HasValue||x.OpportunityId.HasValue).WithMessage("Associate the note with a customer, lead, or opportunity.");}
}
public sealed class ActivityWriteRequestValidator:AbstractValidator<Controllers.ActivityWriteRequest>
{
    public ActivityWriteRequestValidator(){RuleFor(x=>x.Description).NotEmpty().MaximumLength(4000);RuleFor(x=>x.ActivityType).IsInEnum();RuleFor(x=>x).Must(x=>x.CustomerId.HasValue||x.LeadId.HasValue||x.OpportunityId.HasValue).WithMessage("Associate the activity with a customer, lead, or opportunity.");}
}
