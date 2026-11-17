using MiniCRM.Domain;
using CrmTaskStatus = MiniCRM.Domain.TaskStatus;

namespace MiniCRM.Application;

public static class CrmBusinessRules
{
    public static bool CanMoveLead(LeadStatus from, LeadStatus to) => from == to || (from, to) switch
    {
        (LeadStatus.New, LeadStatus.Contacted or LeadStatus.Lost) => true,
        (LeadStatus.Contacted, LeadStatus.Qualified or LeadStatus.Lost) => true,
        (LeadStatus.Qualified, LeadStatus.Lost) => true,
        _ => false
    };

    public static bool CanConvertLead(LeadStatus status) => status is LeadStatus.New or LeadStatus.Contacted or LeadStatus.Qualified;

    public static bool CanMoveOpportunity(OpportunityStage from, OpportunityStage to) => from == to || (from, to) switch
    {
        (OpportunityStage.Qualification, OpportunityStage.NeedsAnalysis or OpportunityStage.Lost) => true,
        (OpportunityStage.NeedsAnalysis, OpportunityStage.Proposal or OpportunityStage.Lost) => true,
        (OpportunityStage.Proposal, OpportunityStage.Negotiation or OpportunityStage.Lost) => true,
        (OpportunityStage.Negotiation, OpportunityStage.Won or OpportunityStage.Lost) => true,
        _ => false
    };

    public static void ApplyTaskCompletion(CrmTask task, DateTime utcNow)
    {
        if (task.Status == CrmTaskStatus.Completed) task.CompletedAt ??= DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        else task.CompletedAt = null;
    }
}
