using MiniCRM.Application;
using MiniCRM.Domain;
using CrmTaskStatus = MiniCRM.Domain.TaskStatus;

namespace MiniCRM.UnitTests;

public sealed class CrmBusinessRulesTests
{
    [Theory]
    [InlineData(LeadStatus.New, LeadStatus.Contacted, true)]
    [InlineData(LeadStatus.Contacted, LeadStatus.Qualified, true)]
    [InlineData(LeadStatus.Qualified, LeadStatus.Converted, false)]
    [InlineData(LeadStatus.Lost, LeadStatus.Contacted, false)]
    public void Lead_status_must_follow_the_sales_workflow(LeadStatus from, LeadStatus to, bool allowed) => Assert.Equal(allowed, CrmBusinessRules.CanMoveLead(from, to));

    [Theory]
    [InlineData(OpportunityStage.Qualification, OpportunityStage.NeedsAnalysis, true)]
    [InlineData(OpportunityStage.Proposal, OpportunityStage.Won, false)]
    [InlineData(OpportunityStage.Negotiation, OpportunityStage.Won, true)]
    [InlineData(OpportunityStage.Won, OpportunityStage.Proposal, false)]
    public void Opportunity_stage_cannot_skip_or_reverse_steps(OpportunityStage from, OpportunityStage to, bool allowed) => Assert.Equal(allowed, CrmBusinessRules.CanMoveOpportunity(from, to));

    [Fact]
    public void Completion_timestamp_is_set_once_and_cleared_when_reopened()
    {
        var completedAt = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);
        var task = new CrmTask { Status = CrmTaskStatus.Completed };
        CrmBusinessRules.ApplyTaskCompletion(task, completedAt);
        CrmBusinessRules.ApplyTaskCompletion(task, completedAt.AddMinutes(5));
        Assert.Equal(completedAt, task.CompletedAt);
        task.Status = CrmTaskStatus.InProgress;
        CrmBusinessRules.ApplyTaskCompletion(task, completedAt.AddMinutes(10));
        Assert.Null(task.CompletedAt);
    }
}
