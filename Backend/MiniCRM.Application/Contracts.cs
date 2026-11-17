using MiniCRM.Domain;

namespace MiniCRM.Application;

public sealed record Page<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount);
public sealed record DashboardSummary(int Customers, int Leads, int ConvertedLeads, int OpenOpportunities,
    int WonOpportunities, int LostOpportunities, decimal PipelineValue, decimal WonRevenue,
    int PendingTasks, int OverdueTasks);
public interface ICrmService
{
    Task<Page<Customer>> CustomersAsync(string? search, int page, int pageSize, CancellationToken ct);
    Task<Customer?> CustomerAsync(int id, CancellationToken ct); Task<Customer> SaveCustomerAsync(Customer model, CancellationToken ct);
    Task<bool> DeleteCustomerAsync(int id, CancellationToken ct);
    Task<Page<Lead>> LeadsAsync(string? search, int page, int pageSize, CancellationToken ct);
    Task<Lead?> LeadAsync(int id, CancellationToken ct); Task<Lead> SaveLeadAsync(Lead model, CancellationToken ct);
    Task<bool> DeleteLeadAsync(int id, CancellationToken ct); Task<bool> ConvertLeadAsync(int id, CancellationToken ct);
    Task<Page<Opportunity>> OpportunitiesAsync(int page, int pageSize, CancellationToken ct);
    Task<Opportunity?> OpportunityAsync(int id, CancellationToken ct);
    Task<Opportunity> SaveOpportunityAsync(Opportunity model, CancellationToken ct);
    Task<bool> DeleteOpportunityAsync(int id, CancellationToken ct);
    Task<Page<CrmTask>> TasksAsync(int page, int pageSize, CancellationToken ct);
    Task<CrmTask?> TaskAsync(int id, CancellationToken ct);
    Task<CrmTask> SaveTaskAsync(CrmTask model, CancellationToken ct);
    Task<bool> DeleteTaskAsync(int id, CancellationToken ct);
    Task<Page<Contact>> ContactsAsync(int page, int pageSize, CancellationToken ct, int? customerId = null);
    Task<Contact?> ContactAsync(int id, CancellationToken ct);
    Task<Contact> SaveContactAsync(Contact model, CancellationToken ct);
    Task<bool> DeleteContactAsync(int id, CancellationToken ct);
    Task<Page<Note>> NotesAsync(int page, int pageSize, CancellationToken ct, int? customerId = null, int? leadId = null, int? opportunityId = null); Task<Note> SaveNoteAsync(Note model, CancellationToken ct);
    Task<Note?> NoteAsync(int id, CancellationToken ct);
    Task<Page<Activity>> ActivitiesAsync(int page, int pageSize, CancellationToken ct, int? customerId = null, int? leadId = null, int? opportunityId = null); Task<Activity> SaveActivityAsync(Activity model, CancellationToken ct);
    Task<Activity?> ActivityAsync(int id, CancellationToken ct);
    Task<DashboardSummary> DashboardAsync(CancellationToken ct);
}
